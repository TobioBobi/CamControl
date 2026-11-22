using System.Net;

namespace CamControl
{
    internal class CamControlService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly bool ownsClient;
        private readonly SemaphoreSlim directionGate = new(1, 1);
        private readonly SemaphoreSlim zoomGate = new(1, 1);
        private readonly Func<CancellationToken, Task<IReadOnlyList<DiscoveredCamera>>> discover;
        private readonly Action<CameraTarget>? rememberAddress;
        private readonly TimeSpan retryDelay;
        private Uri? cameraAddress;
        private CancellationTokenSource? discoveryCancellation;
        private Task<bool>? pendingDiscovery;
        private bool disposed;
        public CameraTarget Target { get; private set; } = new(CameraSettings.DefaultCameraIp);
        public string CameraIp => Target.Ip;
        public bool IsCameraAvailable => cameraAddress != null;
        public bool CameraUnreachable { get; private set; }
        public event Action? ConnectionChanged;

        public CamControlService() : this(new HttpClient { Timeout = TimeSpan.FromSeconds(10) },
            CameraSettings.LoadTarget(), OnvifDiscovery.FindAsync,
            target => CameraSettings.RememberAddress(target), TimeSpan.FromSeconds(5))
        {
            ownsClient = true;
        }

        internal CamControlService(HttpClient httpClient, string cameraIp)
            : this(httpClient, new CameraTarget(cameraIp), OnvifDiscovery.FindAsync, null, TimeSpan.FromSeconds(5))
        {
        }

        internal CamControlService(HttpClient httpClient, CameraTarget target,
            Func<CancellationToken, Task<IReadOnlyList<DiscoveredCamera>>> discover,
            Action<CameraTarget>? rememberAddress, TimeSpan retryDelay)
        {
            _httpClient = httpClient;
            this.discover = discover;
            this.rememberAddress = rememberAddress;
            this.retryDelay = retryDelay;
            SetTarget(target);
        }

        public void SetCameraIp(string ip) => SetTarget(new(ip));

        public void SetTarget(CameraTarget target)
        {
            if (!CameraSettings.TryNormalizeIp(target.Ip, out var normalizedIp))
                throw new ArgumentException(Texts.Camera_InvalidIp, nameof(target));

            CancelDiscovery();
            Target = target with { Ip = normalizedIp };
            cameraAddress = Target.DeviceId == null ? CommandAddress(Target.Ip) : null;
            CameraUnreachable = false;
            ConnectionChanged?.Invoke();
        }

        private static Uri CommandAddress(string ip)
        {
#if DEBUG
            return new Uri("http://localhost:8080/");
#else
            return new Uri($"http://{ip}/");
#endif
        }

        public Task<bool> ResolveCameraAsync()
        {
            if (disposed) return Task.FromResult(false);
            if (IsCameraAvailable) return Task.FromResult(true);
            if (pendingDiscovery != null) return pendingDiscovery;

            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var cancellation = new CancellationTokenSource();
            pendingDiscovery = completion.Task;
            discoveryCancellation = cancellation;
            _ = DiscoverUntilAvailableAsync(completion, cancellation);
            return completion.Task;
        }

        private async Task DiscoverUntilAvailableAsync(TaskCompletionSource<bool> completion, CancellationTokenSource cancellation)
        {
            try
            {
                while (!cancellation.IsCancellationRequested)
                {
                    var replies = await discover(cancellation.Token);
                    if (cancellation.IsCancellationRequested) break;
                    var matches = replies.Where(camera => camera.DeviceId == Target.DeviceId).ToArray();
                    var camera = matches.FirstOrDefault(match => match.Ip == Target.Ip) ?? matches.FirstOrDefault();
                    if (camera != null)
                    {
                        if (camera.Ip != Target.Ip)
                        {
                            Target = Target with { Ip = camera.Ip };
                            try { rememberAddress?.Invoke(Target); }
                            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                            {
                                // The confirmed address stays usable in this session.
                                System.Diagnostics.Debug.WriteLine(e);
                            }
                        }
                        cameraAddress = CommandAddress(Target.Ip);
                        CameraUnreachable = false;
                        discoveryCancellation = null;
                        pendingDiscovery = null;
                        ConnectionChanged?.Invoke();
                        completion.TrySetResult(true);
                        return;
                    }
                    if (!CameraUnreachable)
                    {
                        CameraUnreachable = true;
                        ConnectionChanged?.Invoke();
                    }
                    await Task.Delay(retryDelay, cancellation.Token);
                }
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            finally
            {
                if (ReferenceEquals(discoveryCancellation, cancellation))
                {
                    discoveryCancellation = null;
                    pendingDiscovery = null;
                }
                completion.TrySetResult(false);
                cancellation.Dispose();
            }
        }

        private void CancelDiscovery()
        {
            var cancellation = discoveryCancellation;
            discoveryCancellation = null;
            pendingDiscovery = null;
            cancellation?.Cancel();
        }

        public Task MoveToPosition(Position position, CancellationToken cancellationToken = default)
            => MoveToPreset((int)position, cancellationToken);

        public Task MoveToPreset(int number, CancellationToken cancellationToken = default)
        {
            if (!PresetSettings.IsValidNumber(number)) throw new ArgumentOutOfRangeException(nameof(number));
            return ExecuteSerializedAsync(async targetAddress =>
            {
                await SendCommandAsync(targetAddress, $"poscall&{number}", cancellationToken);
            }, cancellationToken, directionGate, zoomGate);
        }

        public Task SavePosition(Position position, CancellationToken cancellationToken = default)
        {
            if (!Enum.IsDefined(position))
                throw new ArgumentOutOfRangeException(nameof(position));
            return SavePreset((int)position, cancellationToken);
        }

        public Task SavePreset(int number, CancellationToken cancellationToken = default)
        {
            if (!PresetSettings.IsValidNumber(number)) throw new ArgumentOutOfRangeException(nameof(number));
            return ExecuteSerializedAsync(target => SendCommandAsync(target,
                $"posset&{number}", cancellationToken), cancellationToken, directionGate, zoomGate);
        }

        public Task MoveInDirection(Direction direction, CancellationToken cancellationToken = default)
        {
            return ExecuteSerializedAsync(target => SendCommandAsync(target,
                $"{direction.ToString().ToLowerInvariant()}&10&10", cancellationToken), cancellationToken, directionGate);
        }

        public Task StopDirectionalMovement(CancellationToken cancellationToken = default)
        {
            return ExecuteSerializedAsync(target => SendCommandAsync(target, "ptzstop&10&10", cancellationToken), cancellationToken, directionGate);
        }

        public Task Zoom(Zoom zoom, CancellationToken cancellationToken = default)
        {
            return ExecuteSerializedAsync(target => SendCommandAsync(target,
                $"{zoom.ToString().ToLowerInvariant()}&5", cancellationToken), cancellationToken, zoomGate);

        }
        public Task StopZooming(CancellationToken cancellationToken = default)
        {
            return ExecuteSerializedAsync(target => SendCommandAsync(target, "zoomstop&5", cancellationToken), cancellationToken, zoomGate);
        }

        public Task StopAll()
        {
            // Attempt every stop even if one fails. Hold both gates until all
            // responses complete so a new start cannot overtake these stops.
            return ExecuteSerializedAsync(target => Task.WhenAll(
                SendCommandAsync(target, "ptzstop&10&10", CancellationToken.None),
                SendCommandAsync(target, "zoomstop&5", CancellationToken.None),
                SendCommandAsync(target, "focusstop&5", CancellationToken.None)), CancellationToken.None, directionGate, zoomGate);
        }

        private async Task ExecuteSerializedAsync(Func<Uri, Task> command, CancellationToken cancellationToken,
            params SemaphoreSlim[] gates)
        {
            // Queued commands belong to the camera selected when requested.
            var target = cameraAddress ?? throw new HttpRequestException(Texts.Camera_Unreachable);
            bool automatic = Target.DeviceId != null;
            // Separate channels keep a slow zoom response from delaying a PTZ
            // stop (and vice versa). Presets and StopAll acquire both in this order.
            int acquired = 0;
            try
            {
                foreach (var gate in gates)
                {
                    await gate.WaitAsync(cancellationToken);
                    acquired++;
                }
                cancellationToken.ThrowIfCancellationRequested();
                if ((automatic || Target.DeviceId != null) && !ReferenceEquals(target, cameraAddress))
                    throw new HttpRequestException(Texts.Camera_Unreachable);
                await command(target);
            }
            finally
            {
                while (acquired > 0)
                    gates[--acquired].Release();
            }
        }

        private async Task SendCommandAsync(Uri targetAddress, string command, CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _httpClient.GetAsync(
                    new Uri(targetAddress, $"cgi-bin/ptzctrl.cgi?ptzcmd&{command}"), cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e) when (!cancellationToken.IsCancellationRequested &&
                (e is HttpRequestException { StatusCode: null or HttpStatusCode.NotFound or HttpStatusCode.Gone } or OperationCanceledException))
            {
                if (!disposed && Target.DeviceId != null && ReferenceEquals(targetAddress, cameraAddress))
                {
                    cameraAddress = null;
                    CameraUnreachable = true;
                    ConnectionChanged?.Invoke();
                    _ = ResolveCameraAsync();
                }
                throw;
            }
        }

        public void Dispose()
        {
            disposed = true;
            CancelDiscovery();
            if (ownsClient)
                _httpClient.Dispose();
        }
    }
}

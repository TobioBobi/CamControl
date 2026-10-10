using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamControl
{
    internal class CamControlService
    {
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim commandGate = new(1, 1);
        private Uri cameraAddress = null!;
        public string CameraIp { get; private set; } = CameraSettings.DefaultCameraIp;

        public CamControlService() : this(new HttpClient { Timeout = TimeSpan.FromSeconds(10) }, CameraSettings.Load())
        {
        }

        internal CamControlService(HttpClient httpClient, string cameraIp)
        {
            _httpClient = httpClient;
            SetCameraIp(cameraIp);
        }

        public void SetCameraIp(string ip)
        {
            if (!CameraSettings.TryNormalizeIp(ip, out var normalizedIp))
                throw new ArgumentException("Ungültige IPv4-Adresse.", nameof(ip));

            CameraIp = normalizedIp;
            cameraAddress = new Uri($"http://{CameraIp}/");
#if DEBUG
            cameraAddress = new Uri("http://localhost:8080/");
#endif
        }

        public Task MoveToPosition(Position position, CancellationToken cancellationToken = default)
        {
            return ExecuteSerializedAsync(async targetAddress =>
            {
                await SendCommandAsync(targetAddress, $"poscall&{(int)position}", cancellationToken);
                if (position == Position.StandBy)
                {
                    await Task.Delay(TimeSpan.FromSeconds(4), cancellationToken);
                    await SendCommandAsync(targetAddress, "focusin&5", cancellationToken);
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    await SendCommandAsync(targetAddress, "focusstop&5", cancellationToken);
                }
            }, cancellationToken);
        }

        public Task MoveInDirection(Direction direction, CancellationToken cancellationToken = default)
        {
            return ExecuteSerializedAsync(target => SendCommandAsync(target,
                $"{direction.ToString().ToLowerInvariant()}&10&10", cancellationToken), cancellationToken);
        }

        public Task StopDirectionalMovement(CancellationToken cancellationToken = default)
        {
            return ExecuteSerializedAsync(target => SendCommandAsync(target, "ptzstop&10&10", cancellationToken), cancellationToken);
        }

        public Task Zoom(Zoom zoom, CancellationToken cancellationToken = default)
        {
            return ExecuteSerializedAsync(target => SendCommandAsync(target,
                $"{zoom.ToString().ToLowerInvariant()}&5", cancellationToken), cancellationToken);

        }
        public Task StopZooming(CancellationToken cancellationToken = default)
        {
            return ExecuteSerializedAsync(target => SendCommandAsync(target, "zoomstop&5", cancellationToken), cancellationToken);
        }

        public Task StopAll()
        {
            // Attempt every stop even if one fails. Hold the gate until all
            // responses complete so a new start cannot overtake these stops.
            return ExecuteSerializedAsync(target => Task.WhenAll(
                SendCommandAsync(target, "ptzstop&10&10", CancellationToken.None),
                SendCommandAsync(target, "zoomstop&5", CancellationToken.None),
                SendCommandAsync(target, "focusstop&5", CancellationToken.None)), CancellationToken.None);
        }

        private async Task ExecuteSerializedAsync(Func<Uri, Task> command, CancellationToken cancellationToken)
        {
            // Queued commands belong to the camera selected when requested.
            var target = cameraAddress;
            await commandGate.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await command(target);
            }
            finally
            {
                commandGate.Release();
            }
        }

        private async Task SendCommandAsync(Uri targetAddress, string command, CancellationToken cancellationToken)
        {
            using var response = await _httpClient.GetAsync(
                new Uri(targetAddress, $"cgi-bin/ptzctrl.cgi?ptzcmd&{command}"), cancellationToken);
            response.EnsureSuccessStatusCode();
        }
    }
}

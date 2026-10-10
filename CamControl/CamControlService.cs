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

        public async Task MoveToPosition(Position position)
        {
            var targetAddress = cameraAddress;
            await SendCommandAsync(targetAddress, $"poscall&{(int)position}");

            if (position == Position.StandBy)
            {
                await Task.Delay(1000 * 4);
                await UnfocusAsync(targetAddress);
            }
        }

        public Task MoveInDirection(Direction direction)
        {
            return SendCommandAsync(cameraAddress, $"{direction.ToString().ToLowerInvariant()}&10&10");
        }

        public Task StopDirectionalMovement()
        {
            return SendCommandAsync(cameraAddress, "ptzstop&10&10");
        }

        public Task Zoom(Zoom zoom)
        {
            return SendCommandAsync(cameraAddress, $"{zoom.ToString().ToLowerInvariant()}&5");

        }
        public Task StopZooming()
        {
            return SendCommandAsync(cameraAddress, "zoomstop&5");
        }

        private async Task UnfocusAsync(Uri targetAddress)
        {
            await SendCommandAsync(targetAddress, "focusin&5");
            await Task.Delay(1000 * 5);
            await SendCommandAsync(targetAddress, "focusstop&5");
        }

        private async Task SendCommandAsync(Uri targetAddress, string command)
        {
            using var response = await _httpClient.GetAsync(
                new Uri(targetAddress, $"cgi-bin/ptzctrl.cgi?ptzcmd&{command}"));
            response.EnsureSuccessStatusCode();
        }
    }
}

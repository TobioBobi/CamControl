using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamControl
{
    internal class CamControlService
    {
        HttpClient _httpClient;
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
            await _httpClient.GetAsync(new Uri(targetAddress, $"cgi-bin/ptzctrl.cgi?ptzcmd&poscall&{(int)position}"));

            if (position == Position.StandBy)
            {
                await Task.Delay(1000 * 4);
                await UnfocusAsync(targetAddress);
            }
        }

        public void MoveInDirection(Direction direction)
        {
            _httpClient.GetAsync(new Uri(cameraAddress, $"cgi-bin/ptzctrl.cgi?ptzcmd&{direction.ToString().ToLower()}&10&10"));
        }

        public void StopDirectionalMovement()
        {
            _httpClient.GetAsync(new Uri(cameraAddress, "cgi-bin/ptzctrl.cgi?ptzcmd&ptzstop&10&10"));
        }

        public void Zoom(Zoom zoom)
        {
            _httpClient.GetAsync(new Uri(cameraAddress, $"cgi-bin/ptzctrl.cgi?ptzcmd&{zoom.ToString().ToLower()}&5"));

        }
        public void StopZooming()
        {
            _httpClient.GetAsync(new Uri(cameraAddress, "cgi-bin/ptzctrl.cgi?ptzcmd&zoomstop&5"));
        }

        private async Task UnfocusAsync(Uri targetAddress)
        {
            await _httpClient.GetAsync(new Uri(targetAddress, "cgi-bin/ptzctrl.cgi?ptzcmd&focusin&5"));
            await Task.Delay(1000 * 5);
            await _httpClient.GetAsync(new Uri(targetAddress, "cgi-bin/ptzctrl.cgi?ptzcmd&focusstop&5"));
        }
    }
}

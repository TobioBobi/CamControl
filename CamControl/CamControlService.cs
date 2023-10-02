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

        public CamControlService()
        {
            _httpClient = new HttpClient();

            _httpClient.BaseAddress = new Uri("http://192.168.188.64/");
#if DEBUG
            _httpClient.BaseAddress = new Uri("http://localhost:8080/");
#endif
        }

        public async Task MoveToPosition(Position position)
        {
            await _httpClient.GetAsync($"cgi-bin/ptzctrl.cgi?ptzcmd&poscall&{(int)position}");

            if (position == Position.StandBy)
                await UnfocusAsync();
        }

        public void MoveInDirection(Direction direction)
        {
            _httpClient.GetAsync($"cgi-bin/ptzctrl.cgi?ptzcmd&{direction.ToString().ToLower()}&10&10");
        }

        public void StopDirectionalMovement()
        {
            _httpClient.GetAsync("cgi-bin/ptzctrl.cgi?ptzcmd&ptzstop&10&10");
        }

        public void Zoom(Zoom zoom)
        {
            _httpClient.GetAsync($"cgi-bin/ptzctrl.cgi?ptzcmd&{zoom.ToString().ToLower()}&5");

        }
        public void StopZooming()
        {
            _httpClient.GetAsync("cgi-bin/ptzctrl.cgi?ptzcmd&zoomstop&5");
        }

        internal async Task UnfocusAsync()
        {
            await _httpClient.GetAsync("cgi-bin/ptzctrl.cgi?ptzcmd&focusin&5");
            await Task.Delay(1000 * 5);
            await _httpClient.GetAsync("cgi-bin/ptzctrl.cgi?ptzcmd&focusstop&5");
        }
    }
}

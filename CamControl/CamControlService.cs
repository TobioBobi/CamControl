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
            _httpClient.BaseAddress = new Uri("http://localhost:8080/");
            //_httpClient.BaseAddress = new Uri("http://192.168.188.64/");
        }

        public void MoveToPosition(Position position)
        {
            _httpClient.GetAsync($"cgi-bin/ptzctrl.cgi?ptzcmd&poscall&{(int)position}");
        }

        public void MoveInDirection(Direction direction)
        {
            _httpClient.GetAsync($"cgi-bin/ptzctrl.cgi?ptzcmd&{direction.ToString().ToLower()}&10&10");
        }

        public void StopDirectionalMovement()
        {
            _httpClient.GetAsync("cgi-bin/ptzctrl.cgi?ptzcmd&ptzstop&10&10");
        }
    }
}

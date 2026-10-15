using System.Net;
using System.Drawing;

namespace CamControl
{
    internal sealed class DemoCamera
    {
        private readonly object sync = new();
        private float x = .5f, y = .5f, zoom = 1;
        private int horizontal, vertical, zoomDirection;
        private bool focusing;
        private string lastAction = "Bereit";
        private readonly Dictionary<int, (float X, float Y, float Zoom, string Name)> savedPresets = new();

        public record Snapshot(float X, float Y, float Zoom, bool Moving, bool Zooming, bool Focusing, string Action)
        {
            public RectangleF Frame => new(X - .4f / Zoom, Y - .4f / Zoom, .8f / Zoom, .8f / Zoom);
        }

        public Snapshot State
        {
            get { lock (sync) return new(x, y, zoom, horizontal != 0 || vertical != 0, zoomDirection != 0, focusing, lastAction); }
        }

        public void Advance(TimeSpan elapsed)
        {
            lock (sync)
            {
                float seconds = (float)elapsed.TotalSeconds;
                zoom = Math.Clamp(zoom + zoomDirection * seconds, 1, 4);
                x += horizontal * seconds * .18f / zoom;
                y += vertical * seconds * .18f / zoom;
                ClampFrame();
            }
        }

        public bool Apply(string query)
        {
            var parts = query.TrimStart('?').Split('&');
            if (parts.Length < 2 || parts[0] != "ptzcmd") return false;
            lock (sync)
            {
                switch (parts[1])
                {
                    case "posset":
                        if (parts.Length != 3 || !int.TryParse(parts[2], out int saveNumber) || !PresetSettings.IsValidNumber(saveNumber))
                            return false;
                        savedPresets[saveNumber] = (x, y, zoom, $"Eigenes Preset {saveNumber}");
                        lastAction = $"Preset {saveNumber} gespeichert";
                        break;
                    case "poscall":
                        if (parts.Length != 3 || !int.TryParse(parts[2], out int preset) || !PresetSettings.IsValidNumber(preset)) return false;
                        var target = preset switch
                        {
                            1 => (.25f, .45f, 2.5f, "Preset 1"),
                            2 => (.35f, .45f, 1.8f, "Preset 2"),
                            3 => (.65f, .65f, 2f, "Preset 3"),
                            4 => (.5f, .5f, 1f, "Preset 4"),
                            5 => (.5f, .4f, 1.3f, "Preset 5"),
                            6 => (.72f, .5f, 2.5f, "Preset 6"),
                            9 => (.5f, .85f, 2f, "Preset 9"),
                            _ => (0f, 0f, 0f, "")
                        };
                        if (savedPresets.TryGetValue(preset, out var saved)) target = saved;
                        if (target.Item3 == 0) return false;
                        (x, y, zoom, lastAction) = target;
                        horizontal = vertical = zoomDirection = 0;
                        break;
                    case "up": horizontal = 0; vertical = -1; lastAction = "Nach oben"; break;
                    case "down": horizontal = 0; vertical = 1; lastAction = "Nach unten"; break;
                    case "left": horizontal = -1; vertical = 0; lastAction = "Nach links"; break;
                    case "right": horizontal = 1; vertical = 0; lastAction = "Nach rechts"; break;
                    case "ptzstop": horizontal = vertical = 0; lastAction = "Bewegung gestoppt"; break;
                    case "zoomin": zoomDirection = 1; lastAction = "Zoom In"; break;
                    case "zoomout": zoomDirection = -1; lastAction = "Zoom Out"; break;
                    case "zoomstop": zoomDirection = 0; lastAction = "Zoom gestoppt"; break;
                    case "focusin": focusing = true; lastAction = "Fokusfahrt"; break;
                    case "focusstop": focusing = false; lastAction = "Fokus gestoppt"; break;
                    default: return false;
                }
                ClampFrame();
                return true;
            }
        }

        private void ClampFrame()
        {
            float halfSize = .4f / zoom;
            x = Math.Clamp(x, halfSize, 1 - halfSize);
            y = Math.Clamp(y, halfSize, 1 - halfSize);
        }
    }

    // No network transport exists in this handler, including for malformed URLs.
    internal sealed class DemoCameraHandler(DemoCamera camera) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            bool accepted = request.Method == HttpMethod.Get &&
                request.RequestUri?.AbsolutePath == "/cgi-bin/ptzctrl.cgi" && camera.Apply(request.RequestUri.Query);
            return Task.FromResult(new HttpResponseMessage(accepted ? HttpStatusCode.OK : HttpStatusCode.BadRequest));
        }
    }
}

using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace CamControl
{
    internal sealed class DemoCameraView : Control
    {
        private readonly System.Windows.Forms.Timer timer = new() { Interval = 40 };
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly DemoCamera camera;
        public DemoCameraView(DemoCamera camera)
        {
            this.camera = camera;
            DoubleBuffered = true;
            AccessibleName = "Simulierte Bühne und Kamera-Bildausschnitt";
            timer.Tick += (_, _) => { camera.Advance(clock.Elapsed); clock.Restart(); Invalidate(); };
            VisibleChanged += (_, _) => { clock.Restart(); timer.Enabled = Visible; };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var state = camera.State;
            using var heading = new Font(Font, FontStyle.Bold);
            bool compact = Height < heading.Height + Font.Height * 2 + 92;
            if (!compact) g.DrawString("Simulation: Bühne und Kamerabild", heading, Brushes.DarkBlue, 0, 0);
            float top = compact ? 0 : heading.Height + 10;
            float sceneHeight = Math.Max(1, compact ? Height : Height - top - Font.Height * 2 - 12);
            var overview = new RectangleF(0, top, Width * .48f, sceneHeight);
            var preview = new RectangleF(Width * .52f, top, Width * .48f, sceneHeight);
            DrawScene(g, overview);
            var frame = state.Frame;
            using var outline = new Pen(Color.DodgerBlue, 3);
            g.DrawRectangle(outline, overview.X + frame.X * overview.Width, overview.Y + frame.Y * overview.Height,
                frame.Width * overview.Width, frame.Height * overview.Height);

            var saved = g.Save();
            g.SetClip(preview);
            DrawScene(g, new RectangleF(preview.X - frame.X * preview.Width / frame.Width,
                preview.Y - frame.Y * preview.Height / frame.Height, preview.Width / frame.Width, preview.Height / frame.Height));
            g.Restore(saved);
            g.DrawRectangle(Pens.SlateGray, preview.X, preview.Y, preview.Width, preview.Height);
            if (!compact)
            {
                g.DrawString("Bühne mit Bildausschnitt", Font, Brushes.DimGray, overview.X, overview.Bottom + 4);
                g.DrawString($"Kamerabild · Zoom {state.Zoom:0.0}×", Font, Brushes.DimGray, preview.X, preview.Bottom + 4);
                g.DrawString(state.Focusing ? "Fokusfahrt läuft …" : state.Action, Font, Brushes.DarkBlue, 0, Height - Font.Height);
            }
        }

        private void DrawScene(Graphics g, RectangleF bounds)
        {
            g.FillRectangle(Brushes.AliceBlue, bounds);
            for (int i = 1; i < 10; i++)
            {
                g.DrawLine(Pens.LightGray, bounds.X + bounds.Width * i / 10, bounds.Y, bounds.X + bounds.Width * i / 10, bounds.Bottom);
                g.DrawLine(Pens.LightGray, bounds.X, bounds.Y + bounds.Height * i / 10, bounds.Right, bounds.Y + bounds.Height * i / 10);
            }
            DrawObject(g, bounds, new(.16f, .32f, .13f, .24f), Color.SteelBlue, "Redner");
            DrawObject(g, bounds, new(.34f, .35f, .12f, .20f), Color.SeaGreen, "Leser");
            DrawObject(g, bounds, new(.52f, .16f, .35f, .15f), Color.SlateGray, "Bildschirm");
            DrawObject(g, bounds, new(.50f, .62f, .30f, .17f), Color.Sienna, "Tisch");
            DrawObject(g, bounds, new(.70f, .36f, .12f, .23f), Color.MediumPurple, "Demo");
        }

        private void DrawObject(Graphics g, RectangleF scene, RectangleF normalized, Color color, string label)
        {
            var rectangle = new RectangleF(scene.X + normalized.X * scene.Width, scene.Y + normalized.Y * scene.Height,
                normalized.Width * scene.Width, normalized.Height * scene.Height);
            using var fill = new SolidBrush(color);
            g.FillRectangle(fill, rectangle);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoWrap
            };
            using var labelFont = new Font(Font.FontFamily,
                Math.Max(4, Math.Min(rectangle.Height * 72f / g.DpiY * .8f,
                    Math.Min(Font.SizeInPoints, rectangle.Width / label.Length * .9f))));
            g.DrawString(label, labelFont, Brushes.White, rectangle, format);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) timer.Dispose();
            base.Dispose(disposing);
        }
    }
}

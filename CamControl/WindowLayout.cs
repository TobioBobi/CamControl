namespace CamControl;

internal static class WindowLayout
{
    internal static void FitToScreen(Form form)
    {
        bool fitting = false;
        void Fit()
        {
            if (fitting || !form.Visible || form.IsDisposed) return;
            fitting = true;
            try
            {
                var area = Screen.FromControl(form).WorkingArea;
                form.Size = new Size(Math.Min(form.Width, area.Width), Math.Min(form.Height, area.Height));
                // Offscreen windows are also used by the regression checks.
                if (form.Bounds.IntersectsWith(area))
                    form.Location = new Point(Math.Clamp(form.Left, area.Left, area.Right - form.Width),
                        Math.Clamp(form.Top, area.Top, area.Bottom - form.Height));
            }
            finally { fitting = false; }
        }
        form.Shown += (_, _) => Fit();
        form.SizeChanged += (_, _) => Fit();
        form.DpiChanged += (_, _) => form.BeginInvoke(new Action(Fit));
    }
}

namespace CamControl
{
    internal sealed class HiddenSettingsGesture
    {
        private int clicks;
        private long firstClick;
        private long lastClick;

        public bool RegisterClick(long timestamp)
        {
            if (clicks == 0 || timestamp - lastClick > 800 || timestamp - firstClick > 3000)
            {
                clicks = 0;
                firstClick = timestamp;
            }
            lastClick = timestamp;
            if (++clicks < 5)
                return false;

            Reset();
            return true;
        }

        public void Reset() => clicks = 0;
    }
}

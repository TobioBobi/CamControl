using System.Windows;

namespace CamControl;

internal enum CameraView { Full, Compact, Minimal }
internal enum PresetArrangement { Tiles, Strip, Column }
internal readonly record struct CameraLayout(CameraView View, PresetArrangement Arrangement);

// Client-area DIPs, using actual WPF font measurements rather than scaling UI.
// The same minima drive selection and the native interactive resize constraint.
internal sealed record CameraLayoutMetrics(double PresetWidth, double Row, double Key, double FullWidth, double FullHeight)
{
    internal double TileWidth => 2 * PresetWidth + 16;
    internal double TileHeight => 4 * Row + Key + 16;
    internal double ColumnWidth => Math.Max(PresetWidth, 4 * Key) + 12;
    internal double ColumnHeight => 7 * Row + Key + 16;
    internal double StripWidth => 7 * PresetWidth + 2 * Key + 24;
    internal double StripHeight => Math.Max(Row, Key) + 12;
    internal double MinimumHeight(double width) => width >= StripWidth ? StripHeight : width >= TileWidth ? TileHeight : ColumnHeight;
    internal double MinimumWidth(double height) => height >= ColumnHeight ? ColumnWidth : height >= TileHeight ? TileWidth : StripWidth;
    internal CameraLayout Select(Size size, CameraLayout current)
    {
        const double gap = 8;
        double fullGap = current.View == CameraView.Full ? 0 : gap;
        if (size.Width >= FullWidth + fullGap && size.Height >= FullHeight + fullGap)
            return new(CameraView.Full, PresetArrangement.Tiles);

        double stripGap = current.Arrangement == PresetArrangement.Strip ? 0 : gap;
        double columnGap = current.Arrangement == PresetArrangement.Column ? gap : 0;
        var arrangement = size.Width >= StripWidth && (size.Height < TileHeight || size.Width >= StripWidth + stripGap && size.Height < TileHeight + 2 * Row + (current.Arrangement == PresetArrangement.Strip ? gap : 0))
            ? PresetArrangement.Strip
            : size.Width < TileWidth + columnGap && size.Height >= ColumnHeight
                ? PresetArrangement.Column : PresetArrangement.Tiles;
        var compactSize = arrangement switch
        {
            PresetArrangement.Strip => new Size(StripWidth, StripHeight + Key + 8),
            PresetArrangement.Column => new Size(ColumnWidth, ColumnHeight + 2 * Key + 12),
            _ => new Size(TileWidth + 3 * Key + 16, Math.Max(TileHeight, 4 * Key + 28))
        };
        double compactGap = current.View == CameraView.Compact && current.Arrangement == arrangement ? 0 : gap;
        return new(size.Width >= compactSize.Width + compactGap && size.Height >= compactSize.Height + compactGap
            ? CameraView.Compact : CameraView.Minimal, arrangement);
    }
}

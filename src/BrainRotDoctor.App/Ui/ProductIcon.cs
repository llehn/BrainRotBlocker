using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace BrainRotDoctor.App.Ui;

/// <summary>
/// Draws the product mark at runtime so we ship no binary asset. The mark is a
/// melting brain (brain-rot) on the brand gradient — legible down to tray size.
/// </summary>
internal static class ProductIcon
{
    public static WindowIcon Create() => new(RenderBitmap(64));

    public static RenderTargetBitmap RenderBitmap(int size)
    {
        var bitmap = new RenderTargetBitmap(new PixelSize(size, size), new Vector(96, 96));
        using DrawingContext ctx = bitmap.CreateDrawingContext();

        var bg = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.Parse("#7C5CFF"), 0),
                new GradientStop(Color.Parse("#B14BE0"), 1),
            },
        };
        ctx.DrawRectangle(bg, null, new RoundedRect(new Rect(0, 0, size, size), size * 0.23));

        DrawMeltingBrain(ctx, size);
        return bitmap;
    }

    private static void DrawMeltingBrain(DrawingContext ctx, int size)
    {
        double u = size / 64.0; // design on a 64-grid, scaled
        var brain = new SolidColorBrush(Color.Parse("#F8CBE0"));        // soft pink
        var foldPen = new Pen(new SolidColorBrush(Color.Parse("#DC86BA")), 2.6 * u, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        var seamPen = new Pen(new SolidColorBrush(Color.Parse("#C76FAA")), 3.0 * u, lineCap: PenLineCap.Round);

        Point P(double x, double y) => new(x * u, y * u);

        // Brain body: bumpy top, with a melting, drippy bottom edge.
        var body = new StreamGeometry();
        using (StreamGeometryContext g = body.Open())
        {
            g.BeginFigure(P(13, 33), isFilled: true);
            // bumps across the top, left -> right
            g.CubicBezierTo(P(11, 22), P(21, 17), P(27, 22));
            g.CubicBezierTo(P(30, 14), P(40, 15), P(41, 23));
            g.CubicBezierTo(P(50, 19), P(57, 28), P(50, 34));
            g.CubicBezierTo(P(56, 39), P(50, 47), P(45, 43));
            // drippy bottom edge: dip -> drop -> dip -> big drop -> dip
            g.CubicBezierTo(P(45, 50), P(40, 51), P(40, 44));
            g.CubicBezierTo(P(37, 49), P(33, 60), P(30, 60));
            g.CubicBezierTo(P(27, 60), P(25, 50), P(24, 45));
            g.CubicBezierTo(P(22, 51), P(17, 50), P(18, 43));
            g.CubicBezierTo(P(12, 46), P(8, 39), P(14, 36));
            g.CubicBezierTo(P(9, 33), P(10, 31), P(13, 33));
            g.EndFigure(true);
        }

        ctx.DrawGeometry(brain, null, body);

        // Detached droplets falling off (the "rot").
        ctx.DrawGeometry(brain, null, Circle(P(31, 56), 3.1 * u));
        ctx.DrawGeometry(brain, null, Circle(P(20, 53), 2.2 * u));

        // Centre seam (two hemispheres).
        var seam = new StreamGeometry();
        using (StreamGeometryContext g = seam.Open())
        {
            g.BeginFigure(P(31, 19), false);
            g.CubicBezierTo(P(28, 26), P(34, 30), P(31, 40));
            g.EndFigure(false);
        }

        ctx.DrawGeometry(null, seamPen, seam);

        // Folds (wrinkles) on each hemisphere.
        ctx.DrawGeometry(null, foldPen, Squiggle(P(20, 26), P(16, 30), P(22, 32), P(18, 37)));
        ctx.DrawGeometry(null, foldPen, Squiggle(P(24, 23), P(21, 26), P(26, 28), P(23, 31)));
        ctx.DrawGeometry(null, foldPen, Squiggle(P(38, 25), P(43, 28), P(37, 31), P(42, 35)));
        ctx.DrawGeometry(null, foldPen, Squiggle(P(45, 30), P(49, 33), P(44, 35), P(48, 38)));
    }

    private static StreamGeometry Circle(Point center, double r)
    {
        var geo = new StreamGeometry();
        using StreamGeometryContext g = geo.Open();
        g.BeginFigure(new Point(center.X - r, center.Y), true);
        g.ArcTo(new Point(center.X + r, center.Y), new Size(r, r), 0, false, SweepDirection.Clockwise);
        g.ArcTo(new Point(center.X - r, center.Y), new Size(r, r), 0, false, SweepDirection.Clockwise);
        g.EndFigure(true);
        return geo;
    }

    private static StreamGeometry Squiggle(Point a, Point b, Point c, Point d)
    {
        var geo = new StreamGeometry();
        using StreamGeometryContext g = geo.Open();
        g.BeginFigure(a, false);
        g.CubicBezierTo(b, c, d);
        g.EndFigure(false);
        return geo;
    }
}

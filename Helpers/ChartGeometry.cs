using System.Windows;
using System.Windows.Media;

namespace SmartSpice.Helpers;

/// Builds WPF <see cref="Geometry"/> for the dashboard donut chart and the
/// circular health gauge. Angles are in degrees, 0° = top (12 o'clock), clockwise.
public static class ChartGeometry
{
    private static Point P(Point c, double r, double deg)
    {
        double a = deg * Math.PI / 180.0;
        return new Point(c.X + r * Math.Sin(a), c.Y - r * Math.Cos(a));
    }

    ///A filled donut slice between innerR and outerR.
    public static Geometry RingSegment(Point c, double outerR, double innerR, double startDeg, double sweepDeg)
    {
        bool large = sweepDeg > 180;
        double endDeg = startDeg + sweepDeg;

        Point o1 = P(c, outerR, startDeg);
        Point o2 = P(c, outerR, endDeg);
        Point i2 = P(c, innerR, endDeg);
        Point i1 = P(c, innerR, startDeg);

        var fig = new PathFigure { StartPoint = o1, IsClosed = true, IsFilled = true };
        fig.Segments.Add(new ArcSegment(o2, new Size(outerR, outerR), 0, large, SweepDirection.Clockwise, true));
        fig.Segments.Add(new LineSegment(i2, true));
        fig.Segments.Add(new ArcSegment(i1, new Size(innerR, innerR), 0, large, SweepDirection.Counterclockwise, true));

        var geo = new PathGeometry();
        geo.Figures.Add(fig);
        geo.Freeze();
        return geo;
    }

    ///A centre-line arc, meant to be stroked with a rounded-cap pen (the gauge).
    public static Geometry ArcStroke(Point c, double radius, double startDeg, double sweepDeg)
    {
        bool large = sweepDeg > 180;
        Point start = P(c, radius, startDeg);
        Point end = P(c, radius, startDeg + sweepDeg);

        var fig = new PathFigure { StartPoint = start, IsClosed = false, IsFilled = false };
        fig.Segments.Add(new ArcSegment(end, new Size(radius, radius), 0, large, SweepDirection.Clockwise, true));

        var geo = new PathGeometry();
        geo.Figures.Add(fig);
        geo.Freeze();
        return geo;
    }
}

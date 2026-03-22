using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using VolleyStatsPro.Helpers;

namespace VolleyStatsPro.Controls
{
    /// <summary>Simple bar chart control for player/team statistics.</summary>
    public class StatsBarChart : FrameworkElement
    {
        public class Bar { public string Label = ""; public double Value; public Color Color; public string Suffix = ""; }

        private List<Bar> _bars = new();
        private string _title = "";
        private double _maxValue = 0;

        public string Title { get => _title; set { _title = value; InvalidateVisual(); } }

        public void SetBars(List<Bar> bars, double? max = null)
        {
            _bars = bars;
            _maxValue = max ?? (bars.Count > 0 ? bars.Max(b => b.Value) : 1);
            if (_maxValue == 0) _maxValue = 1;
            InvalidateVisual();
        }

        private StreamGeometry MakePolygon(Point[] pts)
        {
            var geo = new StreamGeometry();
            using var ctx = geo.Open();
            ctx.BeginFigure(pts[0], isFilled: true, isClosed: true);
            ctx.PolyLineTo(pts[1..], isStroked: true, isSmoothJoin: false);
            geo.Freeze();
            return geo;
        }

        protected override void OnRender(DrawingContext dc)
        {
            double w = ActualWidth, h = ActualHeight;

            // Background
            dc.DrawRectangle(Theme.BrushBgCard, null, new Rect(0, 0, w, h));

            double pad = 12, titleH = 24;

            if (!string.IsNullOrEmpty(_title))
            {
                var ft = Theme.FT(_title, Theme.SizeH3, Theme.BrushTextPrimary, true, 1.25);
                double tx = (w - ft.Width) / 2;
                dc.DrawText(ft, new Point(tx, 4));
            }

            if (_bars.Count == 0) return;

            double chartTop = pad + titleH;
            double chartBot = h - 20;
            double chartLeft = 50;
            double chartRight = w - pad;
            double chartH = chartBot - chartTop;
            int barCount = _bars.Count;
            double barWidth = (chartRight - chartLeft) / barCount;
            double innerW = barWidth * 0.6;

            // Grid lines
            var dashPen = new Pen(Theme.Brush(Theme.BorderColor), 1.0);
            dashPen.DashStyle = DashStyles.Dash;
            dashPen.Freeze();

            var mutedBrush = Theme.Brush(Theme.TextMuted);

            for (int i = 0; i <= 4; i++)
            {
                double y = chartTop + chartH * (1.0 - i / 4.0);
                dc.DrawLine(dashPen, new Point(chartLeft, y), new Point(chartRight, y));

                double gridVal = _maxValue * i / 4;
                string lbl = gridVal >= 1 ? ((int)gridVal).ToString() : gridVal.ToString("F2");
                var ft = Theme.FT(lbl, Theme.SizeSmall, mutedBrush, false, 1.25);
                dc.DrawText(ft, new Point(0, y - ft.Height / 2));
            }

            var textPrimaryBrush = Theme.BrushTextPrimary;
            var textSecondBrush = Theme.Brush(Theme.TextSecond);

            for (int i = 0; i < barCount; i++)
            {
                var bar = _bars[i];
                double x = chartLeft + i * barWidth + (barWidth - innerW) / 2;
                double barH = bar.Value / _maxValue * chartH;
                double y = chartBot - barH;

                // Gradient bar
                if (barH > 1)
                {
                    var topColor = Color.FromArgb(220, bar.Color.R, bar.Color.G, bar.Color.B);
                    var botColor = Color.FromArgb(80, bar.Color.R, bar.Color.G, bar.Color.B);
                    var grad = new LinearGradientBrush(topColor, botColor, new Point(0, 0), new Point(0, 1));
                    grad.Freeze();
                    dc.DrawRectangle(grad, null, new Rect(x, y, innerW, barH));

                    var borderColor = Color.FromArgb(180, bar.Color.R, bar.Color.G, bar.Color.B);
                    var barBorderPen = new Pen(new SolidColorBrush(borderColor), 1.0);
                    barBorderPen.Freeze();
                    dc.DrawRectangle(null, barBorderPen, new Rect(x, y, innerW, barH));
                }

                // Value label above bar
                string valStr = bar.Value >= 1
                    ? ((int)bar.Value).ToString() + bar.Suffix
                    : bar.Value.ToString("F2") + bar.Suffix;
                var valFt = Theme.FT(valStr, Theme.SizeSmall, textPrimaryBrush, false, 1.25);
                double valX = x + (innerW - valFt.Width) / 2;
                dc.DrawText(valFt, new Point(valX, y - 16));

                // Category label below
                var lblFt = Theme.FT(bar.Label, Theme.SizeSmall, textSecondBrush, false, 1.25);
                double lblX = x + (innerW - lblFt.Width) / 2;
                dc.DrawText(lblFt, new Point(lblX, chartBot + 2));
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            InvalidateVisual();
        }
    }

    /// <summary>Radar/spider chart for skills vs league average.</summary>
    public class RadarChart : FrameworkElement
    {
        public class Axis { public string Label = ""; public double Value; public double MaxValue = 100; }

        private List<Axis> _axes = new();
        private List<Axis> _comparison = new();
        private Color _teamColor = Theme.AccentBlue;
        private Color _compColor = Color.FromArgb(255, 100, 120, 150);

        public void SetAxes(List<Axis> team, List<Axis>? comparison = null, Color? teamColor = null)
        {
            _axes = team;
            _comparison = comparison ?? new();
            if (teamColor.HasValue) _teamColor = teamColor.Value;
            InvalidateVisual();
        }

        private StreamGeometry MakePolygon(Point[] pts)
        {
            var geo = new StreamGeometry();
            using var ctx = geo.Open();
            ctx.BeginFigure(pts[0], isFilled: true, isClosed: true);
            ctx.PolyLineTo(pts[1..], isStroked: true, isSmoothJoin: false);
            geo.Freeze();
            return geo;
        }

        protected override void OnRender(DrawingContext dc)
        {
            double w = ActualWidth, h = ActualHeight;

            dc.DrawRectangle(Theme.BrushBgCard, null, new Rect(0, 0, w, h));

            if (_axes.Count < 3) return;

            double cx = w / 2.0, cy = h / 2.0;
            double r = Math.Min(w, h) / 2.0 - 40;
            int n = _axes.Count;

            var borderPen = new Pen(Theme.Brush(Theme.BorderColor), 1.0);
            borderPen.Freeze();

            // Web grid rings
            for (int ring = 1; ring <= 5; ring++)
            {
                double rr = r * ring / 5.0;
                var pts = GetPolygonPoints(cx, cy, rr, n, -Math.PI / 2);
                var geo = MakePolygon(pts);
                dc.DrawGeometry(null, borderPen, geo);
            }

            // Spokes
            for (int i = 0; i < n; i++)
            {
                double angle = -Math.PI / 2 + 2 * Math.PI * i / n;
                double px = cx + r * Math.Cos(angle);
                double py = cy + r * Math.Sin(angle);
                dc.DrawLine(borderPen, new Point(cx, cy), new Point(px, py));
            }

            // Comparison polygon
            if (_comparison.Count == n)
            {
                var pts = GetDataPoints(cx, cy, r, _comparison);
                var geo = MakePolygon(pts);
                var compFill = new SolidColorBrush(Color.FromArgb(50, _compColor.R, _compColor.G, _compColor.B));
                compFill.Freeze();
                var compPen = new Pen(new SolidColorBrush(_compColor), 1.5);
                compPen.Freeze();
                dc.DrawGeometry(compFill, compPen, geo);
            }

            // Team polygon
            {
                var pts = GetDataPoints(cx, cy, r, _axes);
                var geo = MakePolygon(pts);
                var teamFill = new SolidColorBrush(Color.FromArgb(70, _teamColor.R, _teamColor.G, _teamColor.B));
                teamFill.Freeze();
                var teamPen = new Pen(new SolidColorBrush(_teamColor), 2.0);
                teamPen.Freeze();
                dc.DrawGeometry(teamFill, teamPen, geo);

                var dotBrush = new SolidColorBrush(_teamColor);
                dotBrush.Freeze();
                foreach (var pt in pts)
                    dc.DrawEllipse(dotBrush, null, pt, 4, 4);
            }

            // Labels
            var textSecondBrush = Theme.Brush(Theme.TextSecond);
            for (int i = 0; i < n; i++)
            {
                double angle = -Math.PI / 2 + 2 * Math.PI * i / n;
                double px = cx + (r + 18) * Math.Cos(angle);
                double py = cy + (r + 18) * Math.Sin(angle);
                var ft = Theme.FT(_axes[i].Label, Theme.SizeSmall, textSecondBrush, false, 1.25);
                dc.DrawText(ft, new Point(px - ft.Width / 2, py - ft.Height / 2));
            }
        }

        private Point[] GetPolygonPoints(double cx, double cy, double r, int n, double startAngle)
        {
            var pts = new Point[n];
            for (int i = 0; i < n; i++)
            {
                double a = startAngle + 2 * Math.PI * i / n;
                pts[i] = new Point(cx + r * Math.Cos(a), cy + r * Math.Sin(a));
            }
            return pts;
        }

        private Point[] GetDataPoints(double cx, double cy, double r, List<Axis> axes)
        {
            int n = axes.Count;
            var pts = new Point[n];
            for (int i = 0; i < n; i++)
            {
                double a = -Math.PI / 2 + 2 * Math.PI * i / n;
                double fr = r * (axes[i].Value / axes[i].MaxValue);
                pts[i] = new Point(cx + fr * Math.Cos(a), cy + fr * Math.Sin(a));
            }
            return pts;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            InvalidateVisual();
        }
    }
}

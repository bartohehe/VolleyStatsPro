using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using VolleyStatsPro.Helpers;
using VolleyStatsPro.Models;

namespace VolleyStatsPro.Controls
{
    /// <summary>
    /// Draws a volleyball court (top view) with 9 attack zones and
    /// a colour-intensity heatmap overlay from ZoneData.
    /// Zones are numbered 1-9 per FIVB/DataVolley convention:
    ///   Back row:   1  6  5
    ///   Front row:  2  3  4
    /// (viewed from server's side)
    /// </summary>
    public class CourtHeatmapControl : FrameworkElement
    {
        private List<ZoneData> _zones = new();
        private List<ZoneData> _awayZones = new();
        private List<Point> _dots = new();  // individual hit dots (normalized 0-1)
        private string _title = "";
        private bool _showDots = true;
        private string _homeLabel = "";
        private string _awayLabel = "";

        public string Title     { get => _title;     set { _title     = value; InvalidateVisual(); } }
        public bool   ShowDots  { get => _showDots;  set { _showDots  = value; InvalidateVisual(); } }
        public string HomeLabel { get => _homeLabel; set { _homeLabel = value; InvalidateVisual(); } }
        public string AwayLabel { get => _awayLabel; set { _awayLabel = value; InvalidateVisual(); } }

        public void SetData(List<ZoneData> zones, List<Point>? dots = null, List<ZoneData>? awayZones = null)
        {
            _zones = zones;
            _awayZones = awayZones ?? new();
            _dots = dots ?? new();
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            double w = ActualWidth, h = ActualHeight;

            dc.DrawRectangle(Theme.BrushBgCard, null, new Rect(0, 0, w, h));

            double pad = 32;
            double titleH = string.IsNullOrEmpty(_title) ? 0 : 28;
            var courtRect = new Rect(pad, pad + titleH, w - 2 * pad, h - 2 * pad - titleH);

            // Title
            if (!string.IsNullOrEmpty(_title))
            {
                var ft = Theme.FT(_title, Theme.SizeH3, Theme.BrushTextPrimary, true, 1.25);
                double tx = (w - ft.Width) / 2;
                dc.DrawText(ft, new Point(tx, 8));
            }

            var homeRects = GetZoneRects(courtRect);
            var awayRects = GetTopZoneRects(courtRect);

            DrawCourt(dc, courtRect);
            DrawZoneMarkings(dc, homeRects);
            DrawZoneMarkings(dc, awayRects);
            DrawZones(dc, homeRects, _zones);
            DrawZones(dc, awayRects, _awayZones);
            if (_showDots) DrawDots(dc, courtRect);
            DrawSideLabels(dc, courtRect);
            DrawLegend(dc, courtRect);
        }

        private void DrawCourt(DrawingContext dc, Rect r)
        {
            // Court background
            var courtBrush = new SolidColorBrush(Color.FromArgb(255, 22, 35, 55));
            courtBrush.Freeze();
            dc.DrawRectangle(courtBrush, null, r);

            // Net line in middle
            double midY = r.Top + r.Height / 2;
            var netPen = new Pen(new SolidColorBrush(Color.FromArgb(255, 200, 200, 200)), 3.0);
            netPen.Freeze();
            dc.DrawLine(netPen, new Point(r.Left, midY), new Point(r.Right, midY));

            // Court border
            var borderPen = new Pen(Theme.Brush(Theme.BorderColor), 1.5);
            borderPen.Freeze();
            dc.DrawRectangle(null, borderPen, r);

            // Attack line (3m line) — 1/3 from net on each side
            double attackLineHome = midY - r.Height / 6;
            double attackLineAway = midY + r.Height / 6;
            var attackPen = new Pen(new SolidColorBrush(Color.FromArgb(255, 80, 130, 190)), 1.0);
            attackPen.DashStyle = DashStyles.Dash;
            attackPen.Freeze();
            dc.DrawLine(attackPen, new Point(r.Left, attackLineHome), new Point(r.Right, attackLineHome));
            dc.DrawLine(attackPen, new Point(r.Left, attackLineAway), new Point(r.Right, attackLineAway));

            // Zone grid lines
            DrawZoneGrid(dc, r);
        }

        private void DrawZoneGrid(DrawingContext dc, Rect r)
        {
            var zonePen = new Pen(new SolidColorBrush(Color.FromArgb(255, 50, 70, 100)), 1.0);
            zonePen.Freeze();

            // Vertical dividers (2 lines → 3 columns)
            double col1 = r.Left + r.Width / 3;
            double col2 = r.Left + 2 * r.Width / 3;
            dc.DrawLine(zonePen, new Point(col1, r.Top), new Point(col1, r.Bottom));
            dc.DrawLine(zonePen, new Point(col2, r.Top), new Point(col2, r.Bottom));

            // Horizontal dividers — each half divided into 3 rows
            double row1 = r.Top + r.Height / 6;
            double row2 = r.Top + 2 * r.Height / 6;
            double row3 = r.Top + 4 * r.Height / 6;
            double row4 = r.Top + 5 * r.Height / 6;
            dc.DrawLine(zonePen, new Point(r.Left, row1), new Point(r.Right, row1));
            dc.DrawLine(zonePen, new Point(r.Left, row2), new Point(r.Right, row2));
            dc.DrawLine(zonePen, new Point(r.Left, row3), new Point(r.Right, row3));
            dc.DrawLine(zonePen, new Point(r.Left, row4), new Point(r.Right, row4));
        }

        // Always-visible zone numbers drawn before the heatmap overlay.
        private void DrawZoneMarkings(DrawingContext dc, Dictionary<int, Rect> zoneRects)
        {
            var markBrush = new SolidColorBrush(Color.FromArgb(100, 180, 200, 230));
            markBrush.Freeze();

            foreach (var (zone, zRect) in zoneRects)
            {
                var ft = Theme.FT($"Z{zone}", Theme.SizeH3, markBrush, true, 1.25);
                double cx = zRect.Left + (zRect.Width - ft.Width) / 2;
                double cy = zRect.Top + (zRect.Height - ft.Height) / 2;
                dc.DrawText(ft, new Point(cx, cy));
            }
        }

        private void DrawZones(DrawingContext dc, Dictionary<int, Rect> zoneRects, List<ZoneData> zones)
        {
            if (zones.Count == 0) return;
            int maxCount = zones.Max(z => z.Count);
            if (maxCount == 0) return;

            var textColor = Theme.TextPrimary;

            foreach (var zd in zones)
            {
                if (zd.Zone < 1 || zd.Zone > 9) continue;
                if (!zoneRects.ContainsKey(zd.Zone)) continue;
                float intensity = zd.Count / (float)maxCount;
                var zRect = zoneRects[zd.Zone];

                int alpha = (int)(40 + 180 * intensity);
                Color baseColor = zd.Error > zd.Success ? Theme.Danger : Theme.Accent;
                var heatColor = Color.FromArgb((byte)alpha, baseColor.R, baseColor.G, baseColor.B);
                var heatBrush = new SolidColorBrush(heatColor);
                heatBrush.Freeze();
                dc.DrawRectangle(heatBrush, null, zRect);

                // Count label at bottom of zone
                var labelBrush = new SolidColorBrush(Color.FromArgb(230, textColor.R, textColor.G, textColor.B));
                labelBrush.Freeze();
                var ftCount = Theme.FT(zd.Count.ToString(), Theme.SizeSmall, labelBrush, true, 1.25);
                double cx = zRect.Left + (zRect.Width - ftCount.Width) / 2;
                double cy = zRect.Bottom - ftCount.Height - 3;
                dc.DrawText(ftCount, new Point(cx, cy));
            }
        }

        private Dictionary<int, Rect> GetZoneRects(Rect r)
        {
            double mid = r.Top + r.Height / 2;
            double cw = r.Width / 3;
            double rh = r.Height / 6;

            double l0 = r.Left, l1 = r.Left + cw, l2 = r.Left + 2 * cw;
            double rFront = mid, rMid = mid + rh, rBack = mid + 2 * rh;

            return new Dictionary<int, Rect>
            {
                [4] = new Rect(l0, rFront, cw, rh),
                [3] = new Rect(l1, rFront, cw, rh),
                [2] = new Rect(l2, rFront, cw, rh),
                [5] = new Rect(l0, rMid,   cw, rh),
                [6] = new Rect(l1, rMid,   cw, rh),
                [1] = new Rect(l2, rMid,   cw, rh),
                [9] = new Rect(l0, rBack,  cw, rh),
                [8] = new Rect(l1, rBack,  cw, rh),
                [7] = new Rect(l2, rBack,  cw, rh),
            };
        }

        // Away team zones — top half of court, mirrored vertically relative to home.
        // Zone numbering is from the away team's own perspective (same as home convention),
        // so front-row zones (2,3,4) sit closest to the net and back-row (7,8,9) at the top baseline.
        private Dictionary<int, Rect> GetTopZoneRects(Rect r)
        {
            double mid = r.Top + r.Height / 2;
            double cw = r.Width / 3;
            double rh = r.Height / 6;

            double l0 = r.Left, l1 = r.Left + cw, l2 = r.Left + 2 * cw;
            double rFront = mid - rh;       // row nearest net (top half)
            double rMid   = mid - 2 * rh;  // middle row
            double rBack  = mid - 3 * rh;  // back row (= r.Top)

            return new Dictionary<int, Rect>
            {
                [4] = new Rect(l0, rFront, cw, rh),
                [3] = new Rect(l1, rFront, cw, rh),
                [2] = new Rect(l2, rFront, cw, rh),
                [5] = new Rect(l0, rMid,   cw, rh),
                [6] = new Rect(l1, rMid,   cw, rh),
                [1] = new Rect(l2, rMid,   cw, rh),
                [9] = new Rect(l0, rBack,  cw, rh),
                [8] = new Rect(l1, rBack,  cw, rh),
                [7] = new Rect(l2, rBack,  cw, rh),
            };
        }

        private void DrawSideLabels(DrawingContext dc, Rect r)
        {
            double midY = r.Top + r.Height / 2;
            var labelBrush = new SolidColorBrush(Color.FromArgb(120, Theme.TextSecond.R, Theme.TextSecond.G, Theme.TextSecond.B));
            labelBrush.Freeze();

            if (!string.IsNullOrEmpty(_awayLabel))
            {
                var ft = Theme.FT(_awayLabel, Theme.SizeSmall, labelBrush, false, 1.25);
                double x = r.Left + 4;
                double y = r.Top + (midY - r.Top - ft.Height) / 2;
                dc.DrawText(ft, new Point(x, y));
            }
            if (!string.IsNullOrEmpty(_homeLabel))
            {
                var ft = Theme.FT(_homeLabel, Theme.SizeSmall, labelBrush, false, 1.25);
                double x = r.Left + 4;
                double y = midY + (r.Bottom - midY - ft.Height) / 2;
                dc.DrawText(ft, new Point(x, y));
            }
        }

        private void DrawDots(DrawingContext dc, Rect r)
        {
            var dotColor = Color.FromArgb(200, Theme.Warning.R, Theme.Warning.G, Theme.Warning.B);
            var dotBrush = new SolidColorBrush(dotColor);
            dotBrush.Freeze();

            foreach (var dot in _dots)
            {
                double px = r.Left + dot.X * r.Width;
                double py = r.Top + dot.Y * r.Height;
                dc.DrawEllipse(dotBrush, null, new Point(px, py), 4, 4);
            }
        }

        private void DrawLegend(DrawingContext dc, Rect r)
        {
            double y = r.Bottom + 6;
            if (y + 14 > ActualHeight) return;
            double x = r.Left;
            DrawLegendItem(dc, ref x, y, Theme.Accent, "Good");
            DrawLegendItem(dc, ref x, y, Theme.Danger, "Error");
        }

        private void DrawLegendItem(DrawingContext dc, ref double x, double y, Color c, string label)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            dc.DrawRectangle(b, null, new Rect(x, y + 2, 10, 10));
            x += 13;
            var tb = Theme.Brush(Theme.TextSecond);
            var ft = Theme.FT(label, Theme.SizeSmall, tb, false, 1.25);
            dc.DrawText(ft, new Point(x, y));
            x += 50;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            InvalidateVisual();
        }
    }
}

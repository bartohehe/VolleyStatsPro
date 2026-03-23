using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;

namespace VolleyStatsPro.Helpers
{
    public static class Theme
    {
        // Colors
        public static readonly Color BgDark       = Color.FromRgb(13,  17,  27);
        public static readonly Color BgPanel      = Color.FromRgb(20,  27,  42);
        public static readonly Color BgCard       = Color.FromRgb(26,  35,  53);
        public static readonly Color BgHover      = Color.FromRgb(33,  44,  66);
        public static readonly Color BorderColor  = Color.FromRgb(40,  55,  80);
        public static readonly Color Accent       = Color.FromRgb(0,  188, 140);
        public static readonly Color AccentBlue   = Color.FromRgb(59,  130, 246);
        public static readonly Color AccentPurple = Color.FromRgb(139,  92, 246);
        public static readonly Color TextPrimary  = Color.FromRgb(240, 245, 255);
        public static readonly Color TextSecond   = Color.FromRgb(130, 150, 185);
        public static readonly Color TextMuted    = Color.FromRgb(80,  100, 135);
        public static readonly Color Success      = Color.FromRgb(34,  197,  94);
        public static readonly Color Warning      = Color.FromRgb(251, 191,  36);
        public static readonly Color Danger       = Color.FromRgb(239,  68,  68);
        public static readonly Color NavBg        = Color.FromRgb(10,   14,  22);
        public static readonly Color NavActive    = Color.FromRgb(0,  188, 140);

        // Frozen Brushes
        public static readonly SolidColorBrush BrushBgDark       = Freeze(new SolidColorBrush(BgDark));
        public static readonly SolidColorBrush BrushBgPanel      = Freeze(new SolidColorBrush(BgPanel));
        public static readonly SolidColorBrush BrushBgCard       = Freeze(new SolidColorBrush(BgCard));
        public static readonly SolidColorBrush BrushBgHover      = Freeze(new SolidColorBrush(BgHover));
        public static readonly SolidColorBrush BrushBorder       = Freeze(new SolidColorBrush(BorderColor));
        public static readonly SolidColorBrush BrushAccent       = Freeze(new SolidColorBrush(Accent));
        public static readonly SolidColorBrush BrushAccentBlue   = Freeze(new SolidColorBrush(AccentBlue));
        public static readonly SolidColorBrush BrushAccentPurple = Freeze(new SolidColorBrush(AccentPurple));
        public static readonly SolidColorBrush BrushTextPrimary  = Freeze(new SolidColorBrush(TextPrimary));
        public static readonly SolidColorBrush BrushTextSecond   = Freeze(new SolidColorBrush(TextSecond));
        public static readonly SolidColorBrush BrushTextMuted    = Freeze(new SolidColorBrush(TextMuted));
        public static readonly SolidColorBrush BrushSuccess      = Freeze(new SolidColorBrush(Success));
        public static readonly SolidColorBrush BrushWarning      = Freeze(new SolidColorBrush(Warning));
        public static readonly SolidColorBrush BrushDanger       = Freeze(new SolidColorBrush(Danger));
        public static readonly SolidColorBrush BrushNavBg        = Freeze(new SolidColorBrush(NavBg));
        public static readonly SolidColorBrush BrushNavActive    = Freeze(new SolidColorBrush(NavActive));

        // Font
        public static readonly FontFamily FontFamily = new FontFamily("Segoe UI");
        public const double SizeTitle = 22;
        public const double SizeH2    = 14;
        public const double SizeH3    = 11;
        public const double SizeBody  = 9.75;
        public const double SizeSmall = 8.75;

        public static SolidColorBrush Brush(Color c) => new SolidColorBrush(c);
        private static SolidColorBrush Freeze(SolidColorBrush b) { b.Freeze(); return b; }

        public static Color ResultColor(string result) => result switch
        {
            "#" or "Kill" or "Ace" or "Perfect" or "Block" => Success,
            "+" or "!" => Accent,
            "-" or "/" => Warning,
            "=" or "Error" => Danger,
            _ => TextSecond
        };

        public static Color ActionColor(string action) => action switch
        {
            "Serve"     => AccentBlue,
            "Attack"    => Danger,
            "Block"     => AccentPurple,
            "Reception" => Success,
            "Dig"       => Warning,
            "Set"       => Color.FromRgb(251, 146, 60),
            _ => TextSecond
        };

        // ── DWM rounded corners ───────────────────────────────────────────────────

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUNDSMALL              = 3;

        /// <summary>
        /// Applies small native rounded corners (Windows 11 DWM).
        /// Safe to call in any constructor — hooks SourceInitialized internally.
        /// </summary>
        public static void SetRoundedCorners(Window w)
        {
            w.SourceInitialized += (_, _) =>
            {
                var hwnd = new WindowInteropHelper(w).Handle;
                int pref = DWMWCP_ROUNDSMALL;
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
            };
        }

        // ── Custom dialog chrome ──────────────────────────────────────────────────
        private static readonly Color DialogBarBg = Color.FromRgb(10, 14, 22);

        /// <summary>
        /// Replaces the default OS title bar with a dark custom one matching the main window.
        /// Call at the end of any dialog constructor, after setting Content.
        /// </summary>
        public static void ApplyDialogChrome(Window w, string title)
        {
            // Wrap existing content
            var original = (UIElement)w.Content;
            w.Content    = null;

            // Title bar
            var titleGrid = new Grid
            {
                Height     = 36,
                Background = new SolidColorBrush(DialogBarBg)
            };
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleText = new TextBlock
            {
                Text              = title,
                Foreground        = BrushTextPrimary,
                FontFamily        = FontFamily,
                FontSize          = SizeBody,
                FontWeight        = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(14, 0, 0, 0)
            };
            Grid.SetColumn(titleText, 0);
            titleGrid.Children.Add(titleText);

            var closeBtn = new Button
            {
                Content         = "\uE8BB",
                Width           = 46,
                Height          = 36,
                FontFamily      = new FontFamily("Segoe MDL2 Assets"),
                FontSize        = 10,
                Foreground      = new SolidColorBrush(Color.FromRgb(192, 206, 221)),
                Background      = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding         = new Thickness(0),
                Style           = (Style)Application.Current.Resources["WinChromeButtonClose"]
            };
            closeBtn.Click += (_, _) => w.Close();
            WindowChrome.SetIsHitTestVisibleInChrome(closeBtn, true);
            Grid.SetColumn(closeBtn, 1);
            titleGrid.Children.Add(closeBtn);

            // Separator line below title bar
            var sep = new System.Windows.Shapes.Rectangle
            {
                Height = 1,
                Fill   = new SolidColorBrush(Color.FromRgb(28, 37, 55))
            };

            var dock = new DockPanel { Background = new SolidColorBrush(BgPanel) };
            DockPanel.SetDock(titleGrid, Dock.Top);
            DockPanel.SetDock(sep,       Dock.Top);
            dock.Children.Add(titleGrid);
            dock.Children.Add(sep);
            dock.Children.Add(original);

            w.Content     = dock;
            w.WindowStyle = WindowStyle.None;
            w.Height     += 6;   // compensate: 36px custom bar − ~30px removed system chrome

            WindowChrome.SetWindowChrome(w, new WindowChrome
            {
                CaptionHeight         = 36,
                ResizeBorderThickness  = new Thickness(0),
                GlassFrameThickness   = new Thickness(0),
                UseAeroCaptionButtons = false,
                CornerRadius          = new CornerRadius(0)
            });

            SetRoundedCorners(w);
        }

        // Helper to create a FormattedText for OnRender usage
        public static FormattedText FT(string text, double size, SolidColorBrush brush,
            bool bold = false, double dpi = 96)
        {
            return new FormattedText(text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyles.Normal,
                    bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal),
                size, brush,
                dpi / 96.0);
        }
    }
}

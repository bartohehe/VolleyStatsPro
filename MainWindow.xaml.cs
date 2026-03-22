using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using VolleyStatsPro.Data;
using VolleyStatsPro.Helpers;
using VolleyStatsPro.Views;

namespace VolleyStatsPro
{
    public partial class MainWindow : Window
    {
        // Views
        private DashboardView   _dashboard   = null!;
        private PlayersView     _players     = null!;
        private MatchesView     _matches     = null!;
        private TeamStatsView   _teamStats   = null!;
        private ManageTeamsView _manageTeams = null!;
        private LiveMatchView   _liveMatch   = null!;

        // Nav
        private Button? _activeNavBtn;

        public MainWindow()
        {
            InitializeComponent();
            LoadSvgIcon();
            BuildNav();
            BuildViews();
            ShowView(_dashboard, NavBar.Children[0] as Button);
        }

        private void LoadSvgIcon()
        {
            try
            {
                const int    Size  = 64;
                const double Cx    = Size / 2.0;
                const double Cy    = Size / 2.0;
                const double R     = Size / 2.0 - 3;

                var teal      = new SolidColorBrush(Color.FromRgb(0, 188, 140));
                var darkTeal  = new SolidColorBrush(Color.FromRgb(8, 80, 65));
                var outline   = new Pen(teal, 3) { LineJoin = PenLineJoin.Round };
                var seamPen   = new Pen(darkTeal, 2) { LineJoin = PenLineJoin.Round };

                var visual = new DrawingVisual();
                using (var ctx = visual.RenderOpen())
                {
                    // Fill
                    ctx.DrawEllipse(new SolidColorBrush(Color.FromRgb(10, 16, 28)),
                                    outline, new Point(Cx, Cy), R, R);

                    // Horizontal seam (slight curve via two lines)
                    ctx.DrawLine(seamPen, new Point(Cx - R * 0.55, Cy - R * 0.08),
                                          new Point(Cx + R * 0.55, Cy - R * 0.08));
                    ctx.DrawLine(seamPen, new Point(Cx - R * 0.55, Cy + R * 0.12),
                                          new Point(Cx + R * 0.55, Cy + R * 0.12));

                    // Vertical seam
                    ctx.DrawLine(seamPen, new Point(Cx - R * 0.18, Cy - R * 0.75),
                                          new Point(Cx - R * 0.18, Cy + R * 0.75));
                }

                var rtb = new RenderTargetBitmap(Size, Size, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(visual);
                rtb.Freeze();
                Icon = rtb;
            }
            catch { /* icon is optional */ }
        }

        private void BuildNav()
        {
            string[] navItems = { "Dashboard", "Players", "Matches", "Team Stats", "Manage Teams" };
            // Segoe MDL2 Assets glyphs: Home, Contact, Calendar, BarChart, People
            string[] navIcons = { "\uE80F", "\uE77B", "\uE787", "\uE9D2", "\uE716" };

            for (int i = 0; i < navItems.Length; i++)
            {
                var sp = new StackPanel { Orientation = Orientation.Horizontal };
                sp.Children.Add(new TextBlock
                {
                    Text              = navIcons[i],
                    FontFamily        = new FontFamily("Segoe MDL2 Assets"),
                    FontSize          = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin            = new Thickness(0, 0, 8, 0)
                });
                sp.Children.Add(new TextBlock
                {
                    Text              = navItems[i],
                    VerticalAlignment = VerticalAlignment.Center
                });

                var btn = new Button { Content = sp, Style = (Style)FindResource("NavButton") };
                NavBar.Children.Add(btn);
            }
        }

        private void BuildViews()
        {
            _dashboard   = new DashboardView();
            _players     = new PlayersView();
            _matches     = new MatchesView();
            _teamStats   = new TeamStatsView();
            _manageTeams = new ManageTeamsView();
            _liveMatch   = new LiveMatchView();

            // Set stretch so views fill the content grid
            UIElement[] allViews = { _dashboard, _players, _matches, _teamStats, _manageTeams, _liveMatch };
            foreach (UIElement v in allViews)
            {
                if (v is FrameworkElement fe)
                {
                    fe.HorizontalAlignment = HorizontalAlignment.Stretch;
                    fe.VerticalAlignment   = VerticalAlignment.Stretch;
                }
                ContentGrid.Children.Add(v);
                v.Visibility = Visibility.Collapsed;
            }

            // Wire nav buttons
            var navViews = new UIElement[] { _dashboard, _players, _matches, _teamStats, _manageTeams };
            for (int i = 0; i < navViews.Length && i < NavBar.Children.Count; i++)
            {
                int idx = i;
                if (NavBar.Children[i] is Button btn)
                {
                    btn.Click += (_, _) =>
                    {
                        if (navViews[idx] == _matches)     _matches.LoadMatches();
                        if (navViews[idx] == _players)     _players.Reload();
                        if (navViews[idx] == _dashboard)   _dashboard.Reload();
                        ShowView(navViews[idx], NavBar.Children[idx] as Button);
                    };
                }
            }

            // Opening a match from MatchesView -> switch to LiveMatchView
            _matches.OpenLiveMatch += (_, matchId) =>
            {
                _liveMatch.LoadMatch(matchId);
                ShowView(_liveMatch, null);
            };

            // Back button in LiveMatchView -> return to Matches
            _liveMatch.NavigateBack += (_, _) =>
            {
                _matches.LoadMatches();
                ShowView(_matches, NavBar.Children[2] as Button);
            };
        }

        private void ShowView(UIElement view, Button? navBtn)
        {
            foreach (UIElement child in ContentGrid.Children)
                child.Visibility = Visibility.Collapsed;

            view.Opacity    = 0;
            view.Visibility = Visibility.Visible;
            view.BeginAnimation(OpacityProperty,
                new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(160))));

            SetActiveNav(navBtn);
        }

        private void SetActiveNav(Button? btn)
        {
            if (_activeNavBtn != null)
            {
                _activeNavBtn.Foreground  = Theme.BrushTextSecond;
                _activeNavBtn.Background  = Brushes.Transparent;
                _activeNavBtn.BorderBrush = Brushes.Transparent;
            }
            _activeNavBtn = btn;
            if (btn != null)
            {
                btn.Foreground  = Theme.BrushNavActive;
                btn.Background  = new SolidColorBrush(Color.FromArgb(30, 0, 188, 140));
                btn.BorderBrush = Theme.BrushNavActive;
            }
        }

        private void BtnStartMatch_Click(object sender, RoutedEventArgs e)
        {
            var teams = new TeamRepository().GetAll();
            if (teams.Count < 2)
            {
                MessageBox.Show("Add at least 2 teams first.", "No Teams",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new NewMatchDialog(teams) { Owner = this };
            if (dlg.ShowDialog() == true && dlg.Result != null)
            {
                int matchId = new MatchRepository().Insert(dlg.Result);
                _liveMatch.LoadMatch(matchId);
                ShowView(_liveMatch, null);
            }
        }
    }
}

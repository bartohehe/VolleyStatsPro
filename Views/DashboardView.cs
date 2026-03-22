using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using VolleyStatsPro.Controls;
using VolleyStatsPro.Data;
using VolleyStatsPro.Helpers;
using VolleyStatsPro.Models;

namespace VolleyStatsPro.Views
{
    public class DashboardView : System.Windows.Controls.UserControl
    {
        private readonly TeamRepository  _teamRepo    = new();
        private readonly MatchRepository _matchRepo   = new();
        private readonly StatsService    _statsService = new();

        private ComboBox    _cbTeam           = null!;
        private UniformGrid _statsRow         = null!;
        private Border      _chartLeft        = null!;
        private Border      _chartRight       = null!;
        private Border      _recentMatchesPanel   = null!;
        private Border      _topPerformersPanel   = null!;

        public DashboardView()
        {
            Background = Theme.BrushBgDark;
            Content    = BuildUI();
            LoadTeams();
        }

        public void Reload(int? teamId = null)
        {
            LoadTeams();
            if (teamId.HasValue)
            {
                foreach (var item in _cbTeam.Items)
                    if (item is Team t && t.Id == teamId.Value) { _cbTeam.SelectedItem = t; break; }
            }
            LoadData();
        }

        // ── UI construction ────────────────────────────────────────────────────

        private UIElement BuildUI()
        {
            var outer = new DockPanel { Background = Theme.BrushBgDark };

            // Top bar (64px)
            var topBar = BuildTopBar();
            DockPanel.SetDock(topBar, Dock.Top);
            outer.Children.Add(topBar);

            // KPI row (108px)
            _statsRow = new UniformGrid
            {
                Rows       = 1,
                Columns    = 4,
                Height     = 108,
                Background = Theme.BrushBgDark,
                Margin     = new Thickness(12, 8, 12, 8)
            };
            DockPanel.SetDock(_statsRow, Dock.Top);
            outer.Children.Add(_statsRow);

            // Charts row (300px)
            var chartsRow = BuildChartsRow();
            DockPanel.SetDock(chartsRow, Dock.Top);
            outer.Children.Add(chartsRow);

            // Bottom row (fill)
            var bottomRow = BuildBottomRow();
            outer.Children.Add(bottomRow); // fills remaining space
            return outer;
        }

        private UIElement BuildTopBar()
        {
            var grid = new Grid { Height = 64, Background = Theme.BrushBgDark };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Left: title + subtitle
            var left = new StackPanel { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(20, 0, 0, 0) };
            left.Children.Add(new TextBlock
            {
                Text       = "Team Dashboard",
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeH2,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.BrushTextPrimary
            });
            left.Children.Add(new TextBlock
            {
                Text       = "Comprehensive performance analytics and key metrics",
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeBody,
                Foreground = Theme.BrushTextSecond,
                Margin     = new Thickness(0, 4, 0, 0)
            });
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            // Right: Team label + ComboBox
            var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 20, 0) };
            right.Children.Add(new TextBlock
            {
                Text              = "Team:",
                FontFamily        = Theme.FontFamily,
                FontSize          = Theme.SizeBody,
                Foreground        = Theme.BrushTextSecond,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(0, 0, 8, 0)
            });
            _cbTeam = new ComboBox
            {
                Width      = 200,
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeBody,
                Background = Theme.BrushBgCard,
                Foreground = Theme.BrushTextPrimary,
                Style      = (Style)Application.Current.Resources["DarkComboBox"]
            };
            _cbTeam.SelectionChanged += (_, _) => LoadData();
            right.Children.Add(_cbTeam);
            Grid.SetColumn(right, 1);
            grid.Children.Add(right);

            return grid;
        }

        private UIElement BuildChartsRow()
        {
            var grid = new Grid { Height = 300, Background = Theme.BrushBgDark, Margin = new Thickness(16, 0, 16, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(340) });

            _chartLeft = MakeCardBorder();
            Grid.SetColumn(_chartLeft, 0);
            grid.Children.Add(_chartLeft);

            _chartRight = MakeCardBorder();
            Grid.SetColumn(_chartRight, 2);
            grid.Children.Add(_chartRight);

            return grid;
        }

        private UIElement BuildBottomRow()
        {
            var grid = new Grid { Background = Theme.BrushBgDark, Margin = new Thickness(16, 0, 16, 16) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360) });

            _topPerformersPanel = MakeCardBorder();
            Grid.SetColumn(_topPerformersPanel, 0);
            grid.Children.Add(_topPerformersPanel);

            _recentMatchesPanel = MakeCardBorder();
            Grid.SetColumn(_recentMatchesPanel, 2);
            grid.Children.Add(_recentMatchesPanel);

            return grid;
        }

        // ── Data loading ───────────────────────────────────────────────────────

        private void LoadTeams()
        {
            _cbTeam.Items.Clear();
            foreach (var t in _teamRepo.GetAll()) _cbTeam.Items.Add(t);
            if (_cbTeam.Items.Count > 0 && _cbTeam.SelectedIndex < 0) _cbTeam.SelectedIndex = 0;
        }

        private void LoadData()
        {
            if (_cbTeam.SelectedItem is not Team team) return;

            var agg     = _statsService.GetTeamAggregateStats(team.Id);
            var matches = _matchRepo.GetAll().Where(m => m.HomeTeamId == team.Id || m.AwayTeamId == team.Id).ToList();

            int won    = matches.Count(m => (m.HomeTeamId == team.Id && m.HomeScore > m.AwayScore) || (m.AwayTeamId == team.Id && m.AwayScore > m.HomeScore));
            int played = matches.Count(m => m.Status == "Finished");
            double winRate = played == 0 ? 0 : won / (double)played * 100;

            // KPI cards
            _statsRow.Children.Clear();
            var cards = new[]
            {
                ("WIN RATE",   $"{winRate:F0}%",          $"{won}W-{played - won}L",           Theme.Accent),
                ("ATTACK EFF", $"{agg.AttackEff:P1}",     "K-E/Att",                           Theme.AccentBlue),
                ("BLOCK AVG",  agg.BlockKill.ToString(),  "Per Set",                           Theme.AccentPurple),
                ("SERVE EFF",  $"{agg.ServeEff:P1}",      "A/(A+SE)",                          Theme.Warning),
            };
            foreach (var (lbl, val, sub, col) in cards)
                _statsRow.Children.Add(MakeKpiCard(lbl, val, sub, col));

            // Bar chart
            var allStats = _statsService.GetAllPlayerStats(team.Id);
            var barChart = new StatsBarChart { Title = "Player Performance Comparison" };
            barChart.SetBars(allStats.Take(8).Select(s => new StatsBarChart.Bar
            {
                Label  = s.PlayerName.Split(' ').Last(),
                Value  = s.AttackKill + s.ServeAce + s.BlockKill,
                Color  = Theme.AccentBlue
            }).ToList());

            var chartLeftContent = new DockPanel();
            var chartLeftTitle = new TextBlock
            {
                Text       = "Player Performance Comparison",
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeH3,
                FontWeight = FontWeights.SemiBold,
                Foreground = Theme.BrushTextPrimary,
                Height     = 28,
                Padding    = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(chartLeftTitle, Dock.Top);
            chartLeftContent.Children.Add(chartLeftTitle);
            chartLeftContent.Children.Add(barChart);
            _chartLeft.Child = chartLeftContent;

            // Radar chart
            var radar = new RadarChart();
            double scale = Math.Max(agg.AttackTotal, 1);
            radar.SetAxes(
                new List<RadarChart.Axis>
                {
                    new() { Label = "Attack",    Value = Math.Min(agg.AttackTotal    / scale * 100, 100), MaxValue = 100 },
                    new() { Label = "Serve",     Value = Math.Min(agg.ServeTotal     / scale * 100, 100), MaxValue = 100 },
                    new() { Label = "Reception", Value = Math.Min(agg.ReceptionTotal / scale * 100, 100), MaxValue = 100 },
                    new() { Label = "Block",     Value = Math.Min(agg.BlockTotal     / scale * 80,  100), MaxValue = 100 },
                    new() { Label = "Defense",   Value = Math.Min(agg.DigTotal       / scale * 100, 100), MaxValue = 100 },
                    new() { Label = "Setting",   Value = Math.Min(agg.SetTotal       / scale * 100, 100), MaxValue = 100 },
                },
                comparison: new List<RadarChart.Axis>
                {
                    new() { Label = "Attack",    Value = 60, MaxValue = 100 },
                    new() { Label = "Serve",     Value = 55, MaxValue = 100 },
                    new() { Label = "Reception", Value = 65, MaxValue = 100 },
                    new() { Label = "Block",     Value = 50, MaxValue = 100 },
                    new() { Label = "Defense",   Value = 60, MaxValue = 100 },
                    new() { Label = "Setting",   Value = 58, MaxValue = 100 },
                }
            );

            var chartRightContent = new DockPanel();
            var chartRightTitle = new TextBlock
            {
                Text       = "Skills Analysis vs League Average",
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeH3,
                FontWeight = FontWeights.SemiBold,
                Foreground = Theme.BrushTextPrimary,
                Height     = 28,
                Padding    = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(chartRightTitle, Dock.Top);
            chartRightContent.Children.Add(chartRightTitle);
            chartRightContent.Children.Add(radar);
            _chartRight.Child = chartRightContent;

            // Top performers
            BuildTopPerformers(allStats);

            // Recent matches
            BuildRecentMatches(team, matches.Take(8).ToList());
        }

        private void BuildTopPerformers(List<PlayerStats> stats)
        {
            var outer = new DockPanel();

            var title = new TextBlock
            {
                Text              = "Top Performers",
                FontFamily        = Theme.FontFamily,
                FontSize          = Theme.SizeH3,
                FontWeight        = FontWeights.SemiBold,
                Foreground        = Theme.BrushTextPrimary,
                Height            = 30,
                Padding           = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(title, Dock.Top);
            outer.Children.Add(title);

            var sv   = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var list = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(6, 2, 6, 6) };

            var top = stats
                .OrderByDescending(s => s.AttackKill + s.ServeAce + s.BlockKill)
                .Take(6)
                .ToList();

            Color[] rankColors = { Theme.Warning, Theme.TextSecond, Theme.TextSecond,
                                   Theme.TextMuted, Theme.TextMuted, Theme.TextMuted };

            for (int i = 0; i < top.Count; i++)
            {
                var s   = top[i];
                int pts = s.AttackKill + s.ServeAce + s.BlockKill;

                var row = new Border
                {
                    Margin        = new Thickness(0, 2, 0, 2),
                    Padding       = new Thickness(8, 7, 8, 7),
                    Background    = i == 0
                        ? new SolidColorBrush(Color.FromArgb(20, 0, 188, 140))
                        : Theme.BrushBgHover,
                    CornerRadius  = new CornerRadius(4)
                };

                var g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var rankTb = new TextBlock
                {
                    Text              = $"{i + 1}",
                    FontFamily        = Theme.FontFamily,
                    FontSize          = Theme.SizeSmall,
                    FontWeight        = i < 2 ? FontWeights.Bold : FontWeights.Normal,
                    Foreground        = Theme.Brush(rankColors[i]),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(rankTb, 0);
                g.Children.Add(rankTb);

                var nameTb = new TextBlock
                {
                    Text              = s.PlayerName,
                    FontFamily        = Theme.FontFamily,
                    FontSize          = Theme.SizeBody,
                    Foreground        = Theme.BrushTextPrimary,
                    TextTrimming      = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(nameTb, 1);
                g.Children.Add(nameTb);

                var ptsTb = new TextBlock
                {
                    Text              = pts.ToString(),
                    FontFamily        = Theme.FontFamily,
                    FontSize          = Theme.SizeH2,
                    FontWeight        = FontWeights.Bold,
                    Foreground        = i == 0 ? Theme.BrushAccent : Theme.BrushTextSecond,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin            = new Thickness(8, 0, 0, 0)
                };
                Grid.SetColumn(ptsTb, 2);
                g.Children.Add(ptsTb);

                row.Child = g;
                list.Children.Add(row);
            }

            list.Children.Add(new TextBlock
            {
                Text       = "pts = Kills + Aces + Blocks",
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeSmall,
                Foreground = Theme.BrushTextMuted,
                Margin     = new Thickness(2, 6, 0, 0)
            });

            sv.Content = list;
            outer.Children.Add(sv);
            _topPerformersPanel.Child = outer;
        }

        private void BuildRecentMatches(Team team, List<Models.Match> matches)
        {
            var outer = new DockPanel();

            var title = new TextBlock
            {
                Text       = "Last 8 Matches Form",
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeH3,
                FontWeight = FontWeights.SemiBold,
                Foreground = Theme.BrushTextPrimary,
                Height     = 30,
                Padding    = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(title, Dock.Top);
            outer.Children.Add(title);

            var list = new StackPanel { Orientation = Orientation.Vertical };
            foreach (var m in matches)
            {
                bool isHome = m.HomeTeamId == team.Id;
                bool won    = isHome ? m.HomeScore > m.AwayScore : m.AwayScore > m.HomeScore;
                string opp  = isHome ? m.AwayTeamName : m.HomeTeamName;
                string score = $"{m.HomeScore}-{m.AwayScore}";
                var wColor  = won ? Theme.BrushSuccess : Theme.BrushDanger;
                string wLabel = won ? "W" : "L";

                var row = new Border
                {
                    Background      = Theme.BrushBgHover,
                    BorderBrush     = Theme.BrushBorder,
                    BorderThickness = new Thickness(1),
                    Margin          = new Thickness(4, 2, 4, 2),
                    Padding         = new Thickness(8, 8, 8, 8)
                };

                var rowGrid = new Grid();
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // W/L badge
                var badge = new Border
                {
                    Width               = 26,
                    Height              = 26,
                    Background          = wColor,
                    VerticalAlignment   = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin              = new Thickness(0, 0, 8, 0)
                };
                badge.Child = new TextBlock
                {
                    Text                = wLabel,
                    FontFamily          = Theme.FontFamily,
                    FontSize            = Theme.SizeH3,
                    FontWeight          = FontWeights.Bold,
                    Foreground          = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center
                };
                Grid.SetColumn(badge, 0);
                rowGrid.Children.Add(badge);

                // Opponent name + date
                var info = new StackPanel { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Center };
                info.Children.Add(new TextBlock { Text = opp, FontFamily = Theme.FontFamily, FontSize = Theme.SizeH3, Foreground = Theme.BrushTextPrimary });
                info.Children.Add(new TextBlock { Text = m.Date.ToString("MMM dd"), FontFamily = Theme.FontFamily, FontSize = Theme.SizeSmall, Foreground = Theme.BrushTextMuted });
                Grid.SetColumn(info, 1);
                rowGrid.Children.Add(info);

                // Score
                var scoreText = new TextBlock
                {
                    Text              = score,
                    FontFamily        = Theme.FontFamily,
                    FontSize          = Theme.SizeH3,
                    Foreground        = Theme.BrushTextSecond,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(scoreText, 2);
                rowGrid.Children.Add(scoreText);

                row.Child = rowGrid;
                list.Children.Add(row);
            }

            var sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            sv.Content = list;
            outer.Children.Add(sv);

            _recentMatchesPanel.Child = outer;
        }

        // ── Card helpers ───────────────────────────────────────────────────────

        private static Border MakeCardBorder() =>
            new Border
            {
                Background      = Theme.BrushBgCard,
                BorderBrush     = Theme.BrushBorder,
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(6)
            };

        private static UIElement MakeKpiCard(string label, string value, string sub, Color accent)
        {
            var card = new Border
            {
                Background      = Theme.BrushBgCard,
                BorderBrush     = Theme.BrushBorder,
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(6),
                Margin          = new Thickness(4, 0, 4, 0),
                Padding         = new Thickness(14, 10, 14, 0),
                ClipToBounds    = true
            };

            var dock = new DockPanel();

            // Bottom accent line
            var accentLine = new Border
            {
                Height     = 2,
                Background = Theme.Brush(accent),
                Margin     = new Thickness(0, 8, 0, 0)
            };
            DockPanel.SetDock(accentLine, Dock.Bottom);
            dock.Children.Add(accentLine);

            var content = new StackPanel { Orientation = Orientation.Vertical };
            content.Children.Add(new TextBlock
            {
                Text       = label,
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeSmall,
                Foreground = Theme.BrushTextMuted
            });
            content.Children.Add(new TextBlock
            {
                Text       = value,
                FontFamily = Theme.FontFamily,
                FontSize   = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.Brush(accent),
                Margin     = new Thickness(0, 4, 0, 0)
            });
            content.Children.Add(new TextBlock
            {
                Text       = sub,
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeSmall,
                Foreground = Theme.BrushTextSecond,
                Margin     = new Thickness(0, 4, 0, 0)
            });
            dock.Children.Add(content);

            card.Child = dock;
            return card;
        }
    }
}

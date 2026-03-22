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
    public class TeamStatsView : System.Windows.Controls.UserControl
    {
        private readonly TeamRepository  _teamRepo     = new();
        private readonly MatchRepository _matchRepo    = new();
        private readonly StatsService    _statsService = new();

        private ComboBox   _cbTeam = null!;
        private TabControl _tabs   = null!;

        public TeamStatsView()
        {
            Background = Theme.BrushBgDark;
            Content    = BuildUI();
            LoadTeams();
        }

        // ── UI ─────────────────────────────────────────────────────────────────

        private UIElement BuildUI()
        {
            var outer = new DockPanel { Background = Theme.BrushBgDark };

            // Header
            var header = BuildHeader();
            DockPanel.SetDock(header, Dock.Top);
            outer.Children.Add(header);

            // TabControl fills remaining space
            _tabs = new TabControl
            {
                Background = Theme.BrushBgDark,
                Foreground = Theme.BrushTextPrimary,
                Margin     = new Thickness(14, 0, 14, 14),
                Style      = (Style)Application.Current.Resources["DarkTabControl"]
            };

            _tabs.Items.Add(MakeTab("Overview",  "overview"));
            _tabs.Items.Add(MakeTab("Players",   "players"));
            _tabs.Items.Add(MakeTab("Heatmaps",  "heatmaps"));
            _tabs.Items.Add(MakeTab("Radar",     "radar"));

            _tabs.SelectionChanged += (_, _) => LoadStats();

            outer.Children.Add(_tabs);
            return outer;
        }

        private UIElement BuildHeader()
        {
            var grid = new Grid
            {
                Background = Theme.BrushBgDark,
                Margin     = new Thickness(20, 14, 20, 12)
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titles = new StackPanel();
            titles.Children.Add(new TextBlock
            {
                Text       = "Team Statistics",
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeH2,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.BrushTextPrimary
            });
            titles.Children.Add(new TextBlock
            {
                Text       = "Performance metrics and analysis",
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeBody,
                Foreground = Theme.BrushTextSecond,
                Margin     = new Thickness(0, 4, 0, 0)
            });
            Grid.SetColumn(titles, 0);
            grid.Children.Add(titles);

            var teamRow = new StackPanel
            {
                Orientation       = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            teamRow.Children.Add(new TextBlock
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
                Width      = 220,
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeBody,
                Background = Theme.BrushBgCard,
                Foreground = Theme.BrushTextPrimary,
                Style      = (Style)Application.Current.Resources["DarkComboBox"]
            };
            _cbTeam.SelectionChanged += (_, _) => LoadStats();
            teamRow.Children.Add(_cbTeam);
            Grid.SetColumn(teamRow, 1);
            grid.Children.Add(teamRow);

            return grid;
        }

        private static TabItem MakeTab(string title, string tag) => new TabItem
        {
            Header     = title,
            Tag        = tag,
            Style      = (Style)Application.Current.Resources["DarkTabItem"],
            Content    = new Grid()          // placeholder, rebuilt on selection
        };

        // ── Data ───────────────────────────────────────────────────────────────

        private void LoadTeams()
        {
            _cbTeam.Items.Clear();
            foreach (var t in _teamRepo.GetAll()) _cbTeam.Items.Add(t);
            if (_cbTeam.Items.Count > 0) _cbTeam.SelectedIndex = 0;
        }

        private void LoadStats()
        {
            if (_cbTeam.SelectedItem is not Team team) return;
            if (_tabs.SelectedItem is not TabItem tab) return;

            // Replace the tab's content Grid with freshly built content
            var host = new Grid();
            tab.Content = host;

            switch (tab.Tag?.ToString())
            {
                case "overview":  BuildOverview(host, team);  break;
                case "players":   BuildPlayers(host, team);   break;
                case "heatmaps":  BuildHeatmaps(host, team);  break;
                case "radar":     BuildRadar(host, team);     break;
            }
        }

        // ── Tab content builders ───────────────────────────────────────────────

        private void BuildOverview(Grid host, Team team)
        {
            var agg     = _statsService.GetTeamAggregateStats(team.Id);
            var matches = _matchRepo.GetAll()
                .Where(m => m.HomeTeamId == team.Id || m.AwayTeamId == team.Id).ToList();
            int played  = matches.Count(m => m.Status == "Finished");
            int won     = matches.Count(m => m.Status == "Finished" &&
                          ((m.HomeTeamId == team.Id && m.HomeScore > m.AwayScore) ||
                           (m.AwayTeamId == team.Id && m.AwayScore > m.HomeScore)));
            double winRate = played == 0 ? 0 : won / (double)played * 100;

            host.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            host.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // KPI cards row
            var kpiRow = new UniformGrid
            {
                Rows       = 1,
                Columns    = 4,
                Height     = 108,
                Background = Theme.BrushBgDark,
                Margin     = new Thickness(8)
            };
            kpiRow.Children.Add(StatCard("WIN RATE",    $"{winRate:F0}%",        Theme.Accent));
            kpiRow.Children.Add(StatCard("ATTACK EFF",  $"{agg.AttackEff:P1}",   Theme.AccentBlue));
            kpiRow.Children.Add(StatCard("BLOCK KILLS", $"{agg.BlockKill}",      Theme.AccentPurple));
            kpiRow.Children.Add(StatCard("SERVE EFF",   $"{agg.ServeEff:P1}",    Theme.Warning));
            Grid.SetRow(kpiRow, 0);
            host.Children.Add(kpiRow);

            // Bar chart
            var chart = new StatsBarChart { Title = "Action Totals", Margin = new Thickness(8) };
            chart.SetBars(new List<StatsBarChart.Bar>
            {
                new() { Label = "Serve",     Value = agg.ServeTotal,     Color = Theme.ActionColor("Serve") },
                new() { Label = "Attack",    Value = agg.AttackTotal,    Color = Theme.ActionColor("Attack") },
                new() { Label = "Block",     Value = agg.BlockTotal,     Color = Theme.ActionColor("Block") },
                new() { Label = "Reception", Value = agg.ReceptionTotal, Color = Theme.ActionColor("Reception") },
                new() { Label = "Dig",       Value = agg.DigTotal,       Color = Theme.ActionColor("Dig") },
                new() { Label = "Set",       Value = agg.SetTotal,       Color = Theme.ActionColor("Set") },
            });
            Grid.SetRow(chart, 1);
            host.Children.Add(chart);
        }

        private void BuildPlayers(Grid host, Team team)
        {
            var allStats = _statsService.GetAllPlayerStats(team.Id);

            var dg = new DataGrid
            {
                AutoGenerateColumns        = false,
                CanUserAddRows             = false,
                IsReadOnly                 = true,
                Background                 = Theme.BrushBgPanel,
                Foreground                 = Theme.BrushTextPrimary,
                RowBackground              = Theme.BrushBgPanel,
                AlternatingRowBackground   = Theme.BrushBgCard,
                GridLinesVisibility        = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush   = Theme.BrushBorder,
                BorderThickness            = new Thickness(0),
                FontSize                   = 12,
                RowHeight                  = 32,
                ColumnHeaderHeight         = 34,
                Margin                     = new Thickness(8),
                Style                      = (Style)Application.Current.Resources["DarkDataGrid"]
            };

            dg.Columns.Add(Col("#",        nameof(PlayerStats.Number),         40));
            dg.Columns.Add(Col("Name",     nameof(PlayerStats.PlayerName),     160));
            dg.Columns.Add(Col("Pos",      nameof(PlayerStats.Position),        55));
            dg.Columns.Add(Col("Serve",    nameof(PlayerStats.ServeTotal),      60));
            dg.Columns.Add(Col("Ace",      nameof(PlayerStats.ServeAce),        55));
            dg.Columns.Add(Col("SrvErr",   nameof(PlayerStats.ServeError),      60));
            dg.Columns.Add(Col("Attack",   nameof(PlayerStats.AttackTotal),     65));
            dg.Columns.Add(Col("Kill",     nameof(PlayerStats.AttackKill),      55));
            dg.Columns.Add(Col("AttErr",   nameof(PlayerStats.AttackError),     60));
            dg.Columns.Add(Col("Block",    nameof(PlayerStats.BlockKill),       60));
            dg.Columns.Add(Col("Recep",    nameof(PlayerStats.ReceptionTotal),  65));
            dg.Columns.Add(Col("Dig",      nameof(PlayerStats.DigTotal),        55));

            dg.ItemsSource = allStats;
            host.Children.Add(dg);
        }

        private void BuildHeatmaps(Grid host, Team team)
        {
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            string[] actions  = { "Serve", "Attack", "Block" };
            int[]    colIdxs  = { 0, 2, 4 };
            for (int i = 0; i < 3; i++)
            {
                var zones = _statsService.GetZoneData(team.Id, actions[i]);
                var hm    = new CourtHeatmapControl { Title = $"{actions[i]} Heatmap", Margin = new Thickness(8) };
                hm.SetData(zones);
                Grid.SetColumn(hm, colIdxs[i]);
                host.Children.Add(hm);
            }
        }

        private void BuildRadar(Grid host, Team team)
        {
            var agg   = _statsService.GetTeamAggregateStats(team.Id);
            var radar = new RadarChart { Margin = new Thickness(40) };
            radar.SetAxes(new List<RadarChart.Axis>
            {
                new() { Label = "Attack",    Value = Math.Min(agg.AttackTotal,    100), MaxValue = 100 },
                new() { Label = "Serve",     Value = Math.Min(agg.ServeTotal,     100), MaxValue = 100 },
                new() { Label = "Reception", Value = Math.Min(agg.ReceptionTotal, 100), MaxValue = 100 },
                new() { Label = "Block",     Value = Math.Min(agg.BlockTotal,      50), MaxValue = 50  },
                new() { Label = "Dig",       Value = Math.Min(agg.DigTotal,       100), MaxValue = 100 },
                new() { Label = "Setting",   Value = Math.Min(agg.SetTotal,       100), MaxValue = 100 },
            });
            host.Children.Add(radar);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static UIElement StatCard(string label, string value, Color accent)
        {
            var card = new Border
            {
                Background      = Theme.BrushBgCard,
                BorderBrush     = Theme.BrushBorder,
                BorderThickness = new Thickness(1),
                Margin          = new Thickness(4),
                Padding         = new Thickness(12, 8, 12, 8)
            };
            var sp = new StackPanel { Orientation = Orientation.Vertical };
            sp.Children.Add(new TextBlock
            {
                Text       = label,
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeSmall,
                Foreground = Theme.BrushTextMuted
            });
            sp.Children.Add(new TextBlock
            {
                Text       = value,
                FontFamily = Theme.FontFamily,
                FontSize   = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.Brush(accent),
                Margin     = new Thickness(0, 4, 0, 0)
            });
            card.Child = sp;
            return card;
        }

        private static DataGridTextColumn Col(string header, string binding, double width = double.NaN) =>
            new DataGridTextColumn
            {
                Header  = header,
                Binding = new System.Windows.Data.Binding(binding),
                Width   = double.IsNaN(width)
                              ? new DataGridLength(1, DataGridLengthUnitType.Star)
                              : new DataGridLength(width)
            };
    }
}

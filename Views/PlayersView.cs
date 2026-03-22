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
    /// <summary>Browse all players, view individual stat cards and heatmaps.</summary>
    public class PlayersView : System.Windows.Controls.UserControl
    {
        private readonly TeamRepository   _teamRepo    = new();
        private readonly PlayerRepository _playerRepo  = new();
        private readonly StatsService     _statsService = new();

        private ComboBox  _cbTeam     = null!;
        private ListBox   _playerList = null!;
        private ScrollViewer _detailScroll = null!;
        private StackPanel   _detailPanel  = null!;

        public PlayersView()
        {
            Background = Theme.BrushBgDark;
            Content    = BuildUI();
            LoadTeams();
        }

        public void Reload() => LoadTeams();

        // ── UI construction ────────────────────────────────────────────────────

        private UIElement BuildUI()
        {
            var outer = new DockPanel { Background = Theme.BrushBgDark };

            // Header area (~96px)
            var header = BuildHeader();
            DockPanel.SetDock(header, Dock.Top);
            outer.Children.Add(header);

            // Main content: left list + splitter + right detail (fills remaining)
            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Left panel
            var leftBorder = new Border
            {
                Background      = Theme.BrushBgCard,
                BorderBrush     = Theme.BrushBorder,
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(6),
                Margin          = new Thickness(14, 0, 0, 14),
                ClipToBounds    = true
            };
            _playerList = new ListBox
            {
                Background      = Theme.BrushBgPanel,
                Foreground      = Theme.BrushTextPrimary,
                FontFamily      = Theme.FontFamily,
                FontSize        = Theme.SizeBody,
                BorderThickness = new Thickness(0)
            };
            _playerList.ItemContainerStyle = (Style)System.Windows.Application.Current.Resources["DarkListBoxItem"];
            _playerList.SelectionChanged  += (_, _) => LoadPlayerDetail();
            leftBorder.Child = _playerList;
            Grid.SetColumn(leftBorder, 0);
            mainGrid.Children.Add(leftBorder);

            // GridSplitter
            var splitter = new GridSplitter
            {
                Width               = 5,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background          = Theme.BrushBgDark
            };
            Grid.SetColumn(splitter, 1);
            mainGrid.Children.Add(splitter);

            // Right detail panel
            _detailPanel = new StackPanel { Orientation = Orientation.Vertical };
            _detailScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = Theme.BrushBgDark,
                Margin     = new Thickness(0, 0, 14, 14)
            };
            _detailScroll.Content = _detailPanel;
            Grid.SetColumn(_detailScroll, 2);
            mainGrid.Children.Add(_detailScroll);

            outer.Children.Add(mainGrid);
            return outer;
        }

        private UIElement BuildHeader()
        {
            var grid = new Grid { Background = Theme.BrushBgDark, Height = 96 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(20, 0, 0, 0) };
            left.Children.Add(new TextBlock
            {
                Text       = "Players",
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeH2,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.BrushTextPrimary
            });
            left.Children.Add(new TextBlock
            {
                Text       = "Individual player profiles and statistics",
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeBody,
                Foreground = Theme.BrushTextSecond,
                Margin     = new Thickness(0, 4, 0, 0)
            });
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

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
            _cbTeam.SelectionChanged += (_, _) => LoadPlayers();
            right.Children.Add(_cbTeam);
            Grid.SetColumn(right, 1);
            grid.Children.Add(right);

            return grid;
        }

        // ── Data loading ───────────────────────────────────────────────────────

        private void LoadTeams()
        {
            _cbTeam.Items.Clear();
            foreach (var t in _teamRepo.GetAll()) _cbTeam.Items.Add(t);
            if (_cbTeam.Items.Count > 0) _cbTeam.SelectedIndex = 0;
        }

        private void LoadPlayers()
        {
            if (_cbTeam.SelectedItem is not Team team) return;
            _playerList.Items.Clear();
            foreach (var p in _playerRepo.GetByTeam(team.Id)) _playerList.Items.Add(p);
            _detailPanel.Children.Clear();
        }

        private void LoadPlayerDetail()
        {
            if (_playerList.SelectedItem is not Player player) return;
            _detailPanel.Children.Clear();

            var stats = _statsService.GetPlayerStats(player.Id);
            stats.PlayerName = player.Name;
            stats.Position   = player.Position;
            stats.Number     = player.Number;

            BuildPlayerCard(stats, player);
        }

        private void BuildPlayerCard(PlayerStats stats, Player player)
        {
            // ── Name card (90px height) ────────────────────────────────────────
            var nameCard = new Border
            {
                Background      = Theme.BrushBgCard,
                BorderBrush     = Theme.BrushBorder,
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(6),
                Height          = 90,
                Margin          = new Thickness(0, 0, 0, 4),
                ClipToBounds    = true
            };

            var nameGrid = new Grid();
            nameGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            nameGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Number badge
            var badge = new Border
            {
                Width               = 70,
                Height              = 70,
                Background          = Theme.BrushAccentBlue,
                Margin              = new Thickness(10),
                VerticalAlignment   = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            badge.Child = new TextBlock
            {
                Text                = $"#{player.Number}",
                FontFamily          = Theme.FontFamily,
                FontSize            = 20,
                FontWeight          = FontWeights.Bold,
                Foreground          = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center
            };
            Grid.SetColumn(badge, 0);
            nameGrid.Children.Add(badge);

            var nameInfo = new StackPanel { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
            nameInfo.Children.Add(new TextBlock { Text = player.Name, FontFamily = Theme.FontFamily, FontSize = Theme.SizeH2, FontWeight = FontWeights.Bold, Foreground = Theme.BrushTextPrimary });
            nameInfo.Children.Add(new TextBlock { Text = PositionFull(player.Position), FontFamily = Theme.FontFamily, FontSize = Theme.SizeBody, Foreground = Theme.BrushTextSecond, Margin = new Thickness(0, 4, 0, 0) });
            nameInfo.Children.Add(new TextBlock
            {
                Text       = player.IsActive ? "ACTIVE" : "INACTIVE",
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeSmall,
                Foreground = player.IsActive ? Theme.BrushSuccess : Theme.BrushDanger,
                Margin     = new Thickness(0, 4, 0, 0)
            });
            Grid.SetColumn(nameInfo, 1);
            nameGrid.Children.Add(nameInfo);

            nameCard.Child = nameGrid;
            _detailPanel.Children.Add(nameCard);

            // ── Stat grid (2x3, 160px) ─────────────────────────────────────────
            var statGrid = new UniformGrid
            {
                Rows    = 2,
                Columns = 3,
                Height  = 160,
                Margin  = new Thickness(0, 0, 0, 4)
            };
            statGrid.Children.Add(SmallStatCard("ATTACKS",    $"{stats.AttackTotal}",    $"Kill: {stats.AttackKill}  Err: {stats.AttackError}",             Theme.ActionColor("Attack")));
            statGrid.Children.Add(SmallStatCard("EFFICIENCY", $"{stats.AttackEff:P1}",   "Kills-Errors/Att",                                                Theme.Accent));
            statGrid.Children.Add(SmallStatCard("SERVES",     $"{stats.ServeTotal}",     $"Ace: {stats.ServeAce}  Err: {stats.ServeError}",                 Theme.ActionColor("Serve")));
            statGrid.Children.Add(SmallStatCard("BLOCKS",     $"{stats.BlockKill}",      $"Total: {stats.BlockTotal}  Err: {stats.BlockError}",             Theme.ActionColor("Block")));
            statGrid.Children.Add(SmallStatCard("RECEPTION",  $"{stats.ReceptionEff:P1}",$"Total: {stats.ReceptionTotal}  Perf: {stats.ReceptionPerfect}", Theme.ActionColor("Reception")));
            statGrid.Children.Add(SmallStatCard("DIGS",       $"{stats.DigTotal}",       $"Errors: {stats.DigError}",                                       Theme.ActionColor("Dig")));
            _detailPanel.Children.Add(statGrid);

            // ── Heatmaps (3 side by side) ─────────────────────────────────────
            var hmGrid = new Grid { Height = 280, Margin = new Thickness(0, 4, 0, 0) };
            hmGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hmGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            hmGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hmGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            hmGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            string[] hmActions   = { "Attack", "Serve", "Reception" };
            int[]    hmColIdxs   = { 0, 2, 4 };
            for (int i = 0; i < 3; i++)
            {
                string action = hmActions[i];
                var playerZones = new RallyRepository().GetActionsForPlayer(player.Id)
                    .Where(a => a.ActionType == action && a.Zone.HasValue)
                    .GroupBy(a => a.Zone!.Value)
                    .Select(g => new ZoneData
                    {
                        Zone    = g.Key,
                        Count   = g.Count(),
                        Success = g.Count(a => a.Result is "#" or "Kill" or "Ace" or "Perfect"),
                        Error   = g.Count(a => a.Result is "=" or "Error")
                    })
                    .ToList();

                var hm = new CourtHeatmapControl { Title = $"{action} Map" };
                hm.SetData(playerZones);
                Grid.SetColumn(hm, hmColIdxs[i]);
                hmGrid.Children.Add(hm);
            }
            _detailPanel.Children.Add(hmGrid);
        }

        private static UIElement SmallStatCard(string label, string value, string sub, Color accent)
        {
            var card = new Border
            {
                Background      = Theme.BrushBgCard,
                BorderBrush     = Theme.BrushBorder,
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(6),
                Margin          = new Thickness(2),
                Padding         = new Thickness(10, 8, 10, 8)
            };
            var sp = new StackPanel { Orientation = Orientation.Vertical };
            sp.Children.Add(new TextBlock { Text = label, FontFamily = Theme.FontFamily, FontSize = Theme.SizeSmall, Foreground = Theme.BrushTextMuted });
            sp.Children.Add(new TextBlock { Text = value, FontFamily = Theme.FontFamily, FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Theme.Brush(accent), Margin = new Thickness(0, 4, 0, 0) });
            sp.Children.Add(new TextBlock { Text = sub,   FontFamily = Theme.FontFamily, FontSize = Theme.SizeSmall, Foreground = Theme.BrushTextSecond, Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap });
            card.Child = sp;
            return card;
        }

        private static string PositionFull(string pos) => pos switch
        {
            "OH"  => "Outside Hitter",
            "MB"  => "Middle Blocker",
            "S"   => "Setter",
            "L"   => "Libero",
            "OPP" => "Opposite",
            "DS"  => "Defensive Specialist",
            _     => pos
        };
    }
}

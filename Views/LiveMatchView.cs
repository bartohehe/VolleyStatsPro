using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using VolleyStatsPro.Controls;
using VolleyStatsPro.Data;
using VolleyStatsPro.Helpers;
using VolleyStatsPro.Models;

namespace VolleyStatsPro.Views
{
    public class LiveMatchView : System.Windows.Controls.UserControl
    {
        // ── Repos ──────────────────────────────────────────────────────────────
        private readonly MatchRepository  _matchRepo  = new();
        private readonly SetRepository    _setRepo    = new();
        private readonly RallyRepository  _rallyRepo  = new();
        private readonly PlayerRepository _playerRepo = new();

        // ── Match state ────────────────────────────────────────────────────────
        private Models.Match? _match;
        private Set?           _currentSet;
        private Rally?         _currentRally;
        private Team?          _homeTeam, _awayTeam;
        private Dictionary<int, Player> _homePlayers = new(); // jersey# → Player
        private Dictionary<int, Player> _awayPlayers = new();
        private int _selectedZone = 0;

        // ── UI refs ────────────────────────────────────────────────────────────
        private TextBlock           _lblMatchTitle = null!;
        private TextBlock           _lblCurrentSet = null!;
        private TextBlock           _lblHomeScore  = null!;
        private TextBlock           _lblAwayScore  = null!;
        private TextBlock           _lblSetScore   = null!;
        private StackPanel          _consoleLines  = null!;
        private ScrollViewer        _consoleScroll = null!;
        private TextBox             _consoleInput  = null!;

        private CourtHeatmapControl _heatmap       = null!;
        private string              _heatmapAction = "Attack";
        private Button              _btnTabAttack  = null!;
        private Button              _btnTabServe   = null!;

        // ── Rotation state ─────────────────────────────────────────────────────
        // _homeRotation[i] = jersey# of player at position (i+1); i=0 is pos-1 (server)
        private int[]      _homeRotation = new int[6];
        private int[]      _awayRotation = new int[6];
        private bool       _homeServing  = true;
        private TextBlock  _lblServingInd  = null!;
        private TextBlock[] _homeRotNums   = new TextBlock[6];
        private TextBlock[] _awayRotNums   = new TextBlock[6];
        private Border[]    _homeRotCells  = new Border[6];
        private Border[]    _awayRotCells  = new Border[6];

        // ── Player drawer ──────────────────────────────────────────────────────
        private Border             _drawerPanel    = null!;
        private StackPanel         _drawerHome     = null!;
        private StackPanel         _drawerAway     = null!;
        private TextBlock          _drawerHomeHdr  = null!;
        private TextBlock          _drawerAwayHdr  = null!;
        private bool               _drawerOpen     = false;
        private TranslateTransform _drawerSlide    = null!;
        private TextBlock          _drawerArrow    = null!;
        private const double DrawerWidth           = 300;

        // ── Navigation ─────────────────────────────────────────────────────────
        public event EventHandler? NavigateBack;

        // ── Console history ────────────────────────────────────────────────────
        private readonly List<string> _inputHistory = new();
        private int _historyIdx = -1;

        // ── Console palette ────────────────────────────────────────────────────
        private static readonly SolidColorBrush ConsoleBg     = Freeze(new SolidColorBrush(Color.FromRgb( 8, 10, 18)));
        private static readonly SolidColorBrush ConsolePrompt = Freeze(new SolidColorBrush(Color.FromRgb( 0,188,140)));
        private static readonly SolidColorBrush ConsoleInput  = Freeze(new SolidColorBrush(Color.FromRgb(240,245,255)));
        private static readonly SolidColorBrush ConsoleOk     = Freeze(new SolidColorBrush(Color.FromRgb( 34,197, 94)));
        private static readonly SolidColorBrush ConsoleErr    = Freeze(new SolidColorBrush(Color.FromRgb(239, 68, 68)));
        private static readonly SolidColorBrush ConsoleSys    = Freeze(new SolidColorBrush(Color.FromRgb(130,150,185)));
        private static readonly SolidColorBrush ConsoleRally  = Freeze(new SolidColorBrush(Color.FromRgb(251,191, 36)));
        private static readonly FontFamily ConsoleFont = new FontFamily("Consolas, Courier New");
        private const double ConsoleFontSize = 11.5;

        private static SolidColorBrush Freeze(SolidColorBrush b) { b.Freeze(); return b; }

        // ──────────────────────────────────────────────────────────────────────

        public LiveMatchView()
        {
            Background = Theme.BrushBgDark;
            Content    = BuildUI();
        }

        public void LoadMatch(int matchId)
        {
            _match = _matchRepo.GetById(matchId);
            if (_match == null) return;

            _lblMatchTitle.Text = $"{_match.HomeTeamName}  vs  {_match.AwayTeamName}";

            _homeTeam = new TeamRepository().GetById(_match.HomeTeamId);
            _awayTeam = new TeamRepository().GetById(_match.AwayTeamId);
            _heatmap.HomeLabel = _homeTeam?.Name ?? "";
            _heatmap.AwayLabel = _awayTeam?.Name ?? "";

            _homePlayers = _playerRepo.GetByTeam(_match.HomeTeamId)
                                      .ToDictionary(p => p.Number, p => p);
            _awayPlayers = _playerRepo.GetByTeam(_match.AwayTeamId)
                                      .ToDictionary(p => p.Number, p => p);
            RefreshDrawer();

            var sets = _setRepo.GetByMatch(matchId);
            if (sets.Count == 0)
            {
                _currentSet    = new Set { MatchId = matchId, SetNumber = 1 };
                _currentSet.Id = _setRepo.Insert(_currentSet);
            }
            else
            {
                _currentSet = sets.LastOrDefault(s => !s.IsComplete) ?? sets.Last();
            }

            // Replay existing actions
            var existing = _rallyRepo.GetActionsForMatch(matchId);
            if (existing.Count > 0)
            {
                AppendSys($"Loaded {existing.Count} previous actions.");
                foreach (var a in existing.TakeLast(20))
                    AppendSys($"  {FormatLoadedAction(a)}");
            }

            AppendSys("──────────────────────────────────────────────────────");
            AppendSys($"Match: {_match.HomeTeamName} (home)  vs  {_match.AwayTeamName} (away)");
            AppendSys("──────────────────────────────────────────────────────");
            AppendSys("Format:  [a]<num><action>[sub][zone][result]");
            AppendSys("  S=Serve  R=Reception  A=Attack  B=Block  D=Dig  E=Set");
            AppendSys("  Sub:  H=float  M=jump-float  Q=jump  T=underhand");
            AppendSys("  Attack combos: X1 X5 X6 V5 V6 XP VP PP …");
            AppendSys("  Result: #=perfect  +=positive  !=overpass");
            AppendSys("         /=freeball  -=negative  ==error");
            AppendSys("  Compound: 12SM6.17#  (serve.reception)");
            AppendSys("  Commands: /er h|a /es /em /help /clear");
            AppendSys("──────────────────────────────────────────────────────");

            UpdateSetLabel();
            StartNewRally();
            UpdateRotationDisplay();
            // Prompt for starting lineup on new match (no existing actions)
            if (existing.Count == 0)
                Dispatcher.BeginInvoke((System.Action)ShowLineupDialog);
            _consoleInput.Focus();
        }

        // ── UI construction ────────────────────────────────────────────────────

        private UIElement BuildUI()
        {
            // Wrap everything in a Grid so the drawer can float on top as an overlay layer.
            var overlay = new Grid();

            var root = new DockPanel { Background = Theme.BrushBgDark };

            var topBar = BuildTopBar();
            DockPanel.SetDock(topBar, Dock.Top);
            root.Children.Add(topBar);

            var leftPanel = BuildLeftPanel();
            DockPanel.SetDock(leftPanel, Dock.Left);
            root.Children.Add(leftPanel);

            var legendPanel = BuildLegendPanel();
            DockPanel.SetDock(legendPanel, Dock.Right);
            root.Children.Add(legendPanel);

            var center = new DockPanel();
            var rotBar = BuildRotationSection();
            DockPanel.SetDock(rotBar, Dock.Top);
            center.Children.Add(rotBar);
            center.Children.Add(BuildConsole());
            root.Children.Add(center);

            overlay.Children.Add(root);
            overlay.Children.Add(BuildPlayerDrawer());
            return overlay;
        }

        private UIElement BuildTopBar()
        {
            var bar = new Border
            {
                Background      = Theme.BrushBgPanel,
                Height          = 70,
                BorderBrush     = Theme.BrushBorder,
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titles = new StackPanel { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(20, 0, 20, 0) };
            _lblMatchTitle = new TextBlock { Text = Loc.Get("live.nomatch"), Foreground = Theme.BrushTextPrimary, FontFamily = Theme.FontFamily, FontSize = Theme.SizeH2, FontWeight = FontWeights.SemiBold };
            _lblCurrentSet = new TextBlock { Text = "Set 1", Foreground = Theme.BrushTextSecond, FontFamily = Theme.FontFamily, FontSize = Theme.SizeH3 };
            titles.Children.Add(_lblMatchTitle);
            titles.Children.Add(_lblCurrentSet);
            Grid.SetColumn(titles, 0);

            var scores = new StackPanel { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            var scoreRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            _lblHomeScore = new TextBlock { Text = "0", Foreground = Theme.BrushAccent,     FontFamily = Theme.FontFamily, FontSize = 28, FontWeight = FontWeights.Bold, Margin = new Thickness(0,0,12,0) };
            _lblAwayScore = new TextBlock { Text = "0", Foreground = Theme.BrushAccentBlue, FontFamily = Theme.FontFamily, FontSize = 28, FontWeight = FontWeights.Bold };
            scoreRow.Children.Add(_lblHomeScore);
            scoreRow.Children.Add(new TextBlock { Text = ":", Foreground = Theme.BrushTextSecond, FontFamily = Theme.FontFamily, FontSize = 28, FontWeight = FontWeights.Bold, Margin = new Thickness(0,0,12,0) });
            scoreRow.Children.Add(_lblAwayScore);
            _lblSetScore = new TextBlock { Text = "0 : 0", Foreground = Theme.BrushTextSecond, FontFamily = Theme.FontFamily, FontSize = Theme.SizeH3, HorizontalAlignment = HorizontalAlignment.Center };
            scores.Children.Add(scoreRow);
            scores.Children.Add(_lblSetScore);
            Grid.SetColumn(scores, 1);

            // Back button (column 2)
            var backContent = new StackPanel { Orientation = Orientation.Horizontal };
            backContent.Children.Add(new TextBlock
            {
                Text              = "\uE76B",
                FontFamily        = new FontFamily("Segoe MDL2 Assets"),
                FontSize          = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(0, 0, 6, 0)
            });
            backContent.Children.Add(new TextBlock
            {
                Text              = Loc.Get("live.back"),
                FontFamily        = Theme.FontFamily,
                FontSize          = Theme.SizeBody,
                VerticalAlignment = VerticalAlignment.Center
            });
            var backBtn = new Button
            {
                Content    = backContent,
                Height     = 30,
                Padding    = new Thickness(10, 0, 10, 0),
                Background = Theme.BrushBgHover,
                Foreground = Theme.BrushTextSecond,
                Margin     = new Thickness(0, 0, 16, 0),
                Style      = (Style)Application.Current.Resources["FlatButton"]
            };
            backBtn.Click += (_, _) => NavigateBack?.Invoke(this, EventArgs.Empty);
            Grid.SetColumn(backBtn, 2);

            g.Children.Add(titles);
            g.Children.Add(scores);
            g.Children.Add(backBtn);
            bar.Child = g;
            return bar;
        }

        private UIElement BuildLeftPanel()
        {
            var panel = new Border
            {
                Width           = 320,
                Background      = Theme.BrushBgCard,
                BorderBrush     = Theme.BrushBorder,
                BorderThickness = new Thickness(0, 0, 1, 0)
            };
            var sp = new StackPanel { Orientation = Orientation.Vertical };

            // Heatmap tab strip
            var tabStrip = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
            _btnTabAttack = new Button { Content = Loc.Get("live.tab.attack"), Style = (Style)Application.Current.Resources["NavButton"], Height = 32, Padding = new Thickness(14, 0, 14, 0) };
            _btnTabServe  = new Button { Content = Loc.Get("live.tab.serve"),  Style = (Style)Application.Current.Resources["NavButton"], Height = 32, Padding = new Thickness(14, 0, 14, 0) };
            _btnTabAttack.Click += (_, _) => { _heatmapAction = "Attack"; SetActiveHeatmapTab(_btnTabAttack); RefreshHeatmap(); };
            _btnTabServe.Click  += (_, _) => { _heatmapAction = "Serve";  SetActiveHeatmapTab(_btnTabServe);  RefreshHeatmap(); };
            tabStrip.Children.Add(_btnTabAttack);
            tabStrip.Children.Add(_btnTabServe);
            sp.Children.Add(tabStrip);
            SetActiveHeatmapTab(_btnTabAttack);

            // Heatmap at top, bigger
            _heatmap = new CourtHeatmapControl { Title = Loc.Get("live.heatmap"), Height = 320, Margin = new Thickness(8, 4, 8, 4) };
            sp.Children.Add(_heatmap);

            sp.Children.Add(new Border { Height = 1, Background = Theme.BrushBorder, Margin = new Thickness(0, 6, 0, 6) });

            // Control buttons — rally point row
            var rallyRow = new Grid { Margin = new Thickness(8, 3, 8, 3) };
            rallyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rallyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            rallyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var btnHomePoint = MakeCtrlButton(Loc.Get("live.homept"), Theme.AccentBlue);
            btnHomePoint.Click += (_, _) => CmdEndRally(homeWins: true);
            Grid.SetColumn(btnHomePoint, 0);

            var btnAwayPoint = MakeCtrlButton(Loc.Get("live.awaypt"), Color.FromRgb(80, 80, 180));
            btnAwayPoint.Click += (_, _) => CmdEndRally(homeWins: false);
            Grid.SetColumn(btnAwayPoint, 2);

            rallyRow.Children.Add(btnHomePoint);
            rallyRow.Children.Add(btnAwayPoint);
            sp.Children.Add(rallyRow);

            var btnEndSet = MakeCtrlButton(Loc.Get("live.endset"), Theme.Warning);
            btnEndSet.Margin = new Thickness(8, 3, 8, 3);
            var btnEndMatch = MakeCtrlButton(Loc.Get("live.endmatch"), Theme.Danger);
            btnEndMatch.Margin = new Thickness(8, 3, 8, 3);

            btnEndSet.Click   += (_, _) => CmdEndSet();
            btnEndMatch.Click += (_, _) => CmdEndMatch();

            sp.Children.Add(btnEndSet);
            sp.Children.Add(btnEndMatch);

            var scroll = new ScrollViewer
            {
                Content                       = sp,
                VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            panel.Child = scroll;
            return panel;
        }

        private UIElement BuildRotationSection()
        {
            var bar = new Border
            {
                Background      = Theme.BrushBgPanel,
                BorderBrush     = Theme.BrushBorder,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding         = new Thickness(8, 6, 8, 6)
            };

            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // ── Home side (col 0) ──────────────────────────────────────────────
            var homeStack = new DockPanel();
            var homeLbl = new TextBlock
            {
                Text       = Loc.Get("live.home"),
                Foreground = Theme.BrushAccent,
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeSmall,
                FontWeight = FontWeights.Bold,
                Margin     = new Thickness(0, 0, 0, 3)
            };
            DockPanel.SetDock(homeLbl, Dock.Top);
            homeStack.Children.Add(homeLbl);
            homeStack.Children.Add(BuildRotGrid(isHome: true));
            Grid.SetColumn(homeStack, 0);

            // ── Center: serving indicator + Set Lineup button (col 1) ──────────
            var centerStack = new StackPanel
            {
                Orientation         = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
                Margin              = new Thickness(16, 0, 16, 0),
                MinWidth            = 140
            };
            centerStack.Children.Add(new TextBlock
            {
                Text       = Loc.Get("live.rotation"),
                Foreground = Theme.BrushTextSecond,
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeSmall,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin     = new Thickness(0, 0, 0, 4)
            });
            _lblServingInd = new TextBlock
            {
                Foreground          = Theme.BrushAccent,
                FontFamily          = Theme.FontFamily,
                FontSize            = Theme.SizeSmall,
                FontWeight          = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin              = new Thickness(0, 0, 0, 8)
            };
            centerStack.Children.Add(_lblServingInd);
            var btnSetLineup = new Button
            {
                Content    = Loc.Get("live.setlineup"),
                Height     = 26,
                Background = Theme.BrushBgHover,
                Foreground = Theme.BrushTextSecond,
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeSmall,
                Style      = (Style)Application.Current.Resources["FlatButton"]
            };
            btnSetLineup.Click += (_, _) => ShowLineupDialog();
            centerStack.Children.Add(btnSetLineup);
            Grid.SetColumn(centerStack, 1);

            // ── Away side (col 2) ──────────────────────────────────────────────
            var awayStack = new DockPanel();
            var awayLbl = new TextBlock
            {
                Text       = Loc.Get("live.away"),
                Foreground = Theme.BrushAccentBlue,
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeSmall,
                FontWeight = FontWeights.Bold,
                Margin     = new Thickness(0, 0, 0, 3)
            };
            DockPanel.SetDock(awayLbl, Dock.Top);
            awayStack.Children.Add(awayLbl);
            awayStack.Children.Add(BuildRotGrid(isHome: false));
            Grid.SetColumn(awayStack, 2);

            g.Children.Add(homeStack);
            g.Children.Add(centerStack);
            g.Children.Add(awayStack);
            bar.Child = g;
            return bar;
        }

        // Home: row0=[pos1,pos6,pos5]  row1=[pos2,pos3,pos4]
        private static readonly int[,] RotLayout     = { { 4, 3, 2 }, { 5, 6, 1 } };
        // Away: mirrored — pos1 bottom-right, pos2 top-right, pos3 top-center, etc.
        private static readonly int[,] AwayRotLayout = { { 4, 3, 2 }, { 5, 6, 1 } };

        private UIElement BuildRotGrid(bool isHome)
        {
            var layout = isHome ? RotLayout : AwayRotLayout;
            var g = new UniformGrid { Rows = 2, Columns = 3 };
            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    int pos = layout[row, col];
                    int idx = pos - 1; // array index

                    var posLbl = new TextBlock
                    {
                        Text              = pos.ToString(),
                        Foreground        = Theme.BrushTextSecond,
                        FontFamily        = Theme.FontFamily,
                        FontSize          = 8,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin            = new Thickness(3, 2, 0, 0)
                    };
                    var numLbl = new TextBlock
                    {
                        Text              = "—",
                        Foreground        = Theme.BrushTextPrimary,
                        FontFamily        = Theme.FontFamily,
                        FontSize          = 12,
                        FontWeight        = FontWeights.SemiBold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin            = new Thickness(0, 0, 0, 2)
                    };

                    if (isHome) { _homeRotNums[idx] = numLbl; }
                    else        { _awayRotNums[idx] = numLbl; }

                    var inner = new DockPanel();
                    DockPanel.SetDock(posLbl, Dock.Top);
                    inner.Children.Add(posLbl);
                    inner.Children.Add(numLbl);

                    var cell = new Border
                    {
                        BorderBrush     = Theme.BrushBorder,
                        BorderThickness = new Thickness(1),
                        Margin          = new Thickness(1),
                        MinHeight       = 42,
                        Child           = inner
                    };

                    if (isHome) { _homeRotCells[idx] = cell; }
                    else        { _awayRotCells[idx] = cell; }

                    g.Children.Add(cell);
                }
            }
            return g;
        }

        private void UpdateRotationDisplay()
        {
            for (int i = 0; i < 6; i++)
            {
                _homeRotNums[i].Text = _homeRotation[i] > 0 ? $"#{_homeRotation[i]}" : "—";
                _awayRotNums[i].Text = _awayRotation[i] > 0 ? $"#{_awayRotation[i]}" : "—";

                // Highlight server cell (position 1 = index 0)
                _homeRotCells[i].Background = (i == 0 && _homeServing)
                    ? new SolidColorBrush(Color.FromArgb(60, 0, 188, 140))
                    : Brushes.Transparent;
                _awayRotCells[i].Background = (i == 0 && !_homeServing)
                    ? new SolidColorBrush(Color.FromArgb(60, 80, 80, 220))
                    : Brushes.Transparent;
            }

            _lblServingInd.Text = _homeServing
                ? $"▶ {_match?.HomeTeamName ?? "Home"} serving"
                : $"▶ {_match?.AwayTeamName ?? "Away"} serving";
            _lblServingInd.Foreground = _homeServing ? Theme.BrushAccent : Theme.BrushAccentBlue;
        }

        private void ApplyRotation(bool homeWins)
        {
            bool homeWasServing = _homeServing;
            _homeServing = homeWins;

            if (homeWins && !homeWasServing)
                _homeRotation = RotateTeam(_homeRotation);
            else if (!homeWins && homeWasServing)
                _awayRotation = RotateTeam(_awayRotation);

            UpdateRotationDisplay();
        }

        private static int[] RotateTeam(int[] r)
        {
            // Gaining the serve: player in pos2 → pos1 (server), 3→2, … 1→6
            // In array: new[i] = old[(i+1) % 6]
            var n = new int[6];
            for (int i = 0; i < 6; i++) n[i] = r[(i + 1) % 6];
            return n;
        }

        private void ShowLineupDialog()
        {
            if (_match == null) return;

            var homeTb = new TextBox[6];
            var awayTb = new TextBox[6];
            bool dlgHomeServing = _homeServing;

            // Serving toggle buttons (kept in closure)
            Button? btnDlgHome = null, btnDlgAway = null;
            void RefreshServingBtns()
            {
                if (btnDlgHome == null || btnDlgAway == null) return;
                btnDlgHome.Background = dlgHomeServing
                    ? new SolidColorBrush(Theme.Accent)
                    : Theme.BrushBgHover;
                btnDlgAway.Background = !dlgHomeServing
                    ? new SolidColorBrush(Color.FromRgb(80, 80, 220))
                    : Theme.BrushBgHover;
            }

            var dlg = new Window
            {
                Title                   = string.Format(Loc.Get("live.setlineup.dlg"), _currentSet?.SetNumber),
                Width                   = 520,
                Height                  = 370,
                ResizeMode              = ResizeMode.NoResize,
                WindowStartupLocation   = WindowStartupLocation.CenterOwner,
                Owner                   = Application.Current.MainWindow,
                Background              = new SolidColorBrush(Color.FromRgb(10, 14, 24)),
                ShowInTaskbar           = false
            };

            var root = new DockPanel { Margin = new Thickness(16) };

            // Bottom buttons
            var botRow = new StackPanel
            {
                Orientation         = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin              = new Thickness(0, 10, 0, 0)
            };
            var btnCancel = new Button { Content = Loc.Get("common.cancel"), Width = 80, Height = 28, Margin = new Thickness(0,0,8,0),
                Background = Theme.BrushBgHover, Foreground = Theme.BrushTextSecond, FontFamily = Theme.FontFamily,
                Style = (Style)Application.Current.Resources["FlatButton"] };
            btnCancel.Click += (_, _) => dlg.Close();

            var btnSave = new Button { Content = Loc.Get("live.savelineup"), Width = 100, Height = 28,
                Background = new SolidColorBrush(Theme.Accent), Foreground = Brushes.White,
                FontFamily = Theme.FontFamily,
                Style = (Style)Application.Current.Resources["FlatButton"] };
            btnSave.Click += (_, _) =>
            {
                int[] ParseInputs(TextBox[] tbs, int[]? existing)
                {
                    var arr = new int[6];
                    for (int i = 0; i < 6; i++)
                    {
                        if (int.TryParse(tbs[i].Text.Trim(), out int n) && n >= 0)
                            arr[i] = n;
                        else if (existing != null)
                            arr[i] = existing[i];
                    }
                    return arr;
                }
                _homeRotation = ParseInputs(homeTb, _homeRotation);
                _awayRotation = ParseInputs(awayTb, _awayRotation);
                _homeServing  = dlgHomeServing;
                UpdateRotationDisplay();
                dlg.DialogResult = true;
            };
            botRow.Children.Add(btnCancel);
            botRow.Children.Add(btnSave);
            DockPanel.SetDock(botRow, Dock.Bottom);
            root.Children.Add(botRow);

            // Serving row
            var servingRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            servingRow.Children.Add(new TextBlock { Text = "Serving first: ", Foreground = Theme.BrushTextSecond,
                FontFamily = Theme.FontFamily, FontSize = Theme.SizeSmall, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,6,0) });
            btnDlgHome = new Button { Content = _match.HomeTeamName, Height = 24, Padding = new Thickness(8,0,8,0),
                Foreground = Brushes.White, FontFamily = Theme.FontFamily, FontSize = Theme.SizeSmall,
                Style = (Style)Application.Current.Resources["FlatButton"] };
            btnDlgHome.Click += (_, _) => { dlgHomeServing = true;  RefreshServingBtns(); };
            btnDlgAway = new Button { Content = _match.AwayTeamName, Height = 24, Padding = new Thickness(8,0,8,0),
                Foreground = Brushes.White, FontFamily = Theme.FontFamily, FontSize = Theme.SizeSmall,
                Margin = new Thickness(6,0,0,0),
                Style = (Style)Application.Current.Resources["FlatButton"] };
            btnDlgAway.Click += (_, _) => { dlgHomeServing = false; RefreshServingBtns(); };
            servingRow.Children.Add(btnDlgHome);
            servingRow.Children.Add(btnDlgAway);
            DockPanel.SetDock(servingRow, Dock.Top);
            root.Children.Add(servingRow);
            RefreshServingBtns();

            // Two-column lineup grid
            var cols = new Grid();
            cols.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cols.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            cols.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            UIElement MakeLineupGrid(string teamName, TextBox[] tbs, int[] existing, int colIdx)
            {
                var sp2 = new StackPanel();
                sp2.Children.Add(new TextBlock { Text = teamName, Foreground = Theme.BrushTextPrimary,
                    FontFamily = Theme.FontFamily, FontSize = Theme.SizeBody, FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 6) });

                var ug = new UniformGrid { Rows = 2, Columns = 3 };
                for (int row = 0; row < 2; row++)
                {
                    for (int c2 = 0; c2 < 3; c2++)
                    {
                        int pos = RotLayout[row, c2];
                        int idx = pos - 1;
                        var cell = new StackPanel { Margin = new Thickness(2) };
                        cell.Children.Add(new TextBlock
                        {
                            Text      = pos == 1 ? "1 ▶" : pos.ToString(),
                            Foreground = pos == 1 ? Theme.BrushAccent : Theme.BrushTextSecond,
                            FontFamily = Theme.FontFamily,
                            FontSize   = 9,
                            Margin     = new Thickness(0, 0, 0, 2)
                        });
                        var tb = new TextBox
                        {
                            Text            = existing[idx] > 0 ? existing[idx].ToString() : "",
                            Background      = new SolidColorBrush(Color.FromRgb(18, 24, 38)),
                            Foreground      = Theme.BrushTextPrimary,
                            CaretBrush      = Theme.BrushAccent,
                            BorderBrush     = Theme.BrushBorder,
                            BorderThickness = new Thickness(1),
                            FontFamily      = Theme.FontFamily,
                            FontSize        = Theme.SizeBody,
                            Padding         = new Thickness(4, 2, 4, 2),
                            MaxLength       = 3,
                            TextAlignment   = TextAlignment.Center
                        };
                        tbs[idx] = tb;
                        cell.Children.Add(tb);
                        ug.Children.Add(cell);
                    }
                }
                sp2.Children.Add(ug);
                Grid.SetColumn(sp2, colIdx);
                return sp2;
            }

            var homeGrid = MakeLineupGrid(_match.HomeTeamName, homeTb, _homeRotation, 0);
            var awayGrid = MakeLineupGrid(_match.AwayTeamName, awayTb, _awayRotation, 2);
            cols.Children.Add(homeGrid);
            cols.Children.Add(awayGrid);
            root.Children.Add(cols);

            dlg.Content = root;
            Theme.ApplyDialogChrome(dlg, dlg.Title);
            bool saved = dlg.ShowDialog() == true;
            if (saved)
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render,
                    (System.Action)(() => { RefreshDrawer(); OpenDrawer(); }));
        }

        private UIElement BuildPlayerDrawer()
        {
            var host = new Grid { HorizontalAlignment = HorizontalAlignment.Right, IsHitTestVisible = true };
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // tab
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // panel

            _drawerSlide = new TranslateTransform { X = DrawerWidth };
            host.RenderTransform = _drawerSlide;

            // ── Tab button ─────────────────────────────────────────────────────
            _drawerArrow = new TextBlock
            {
                Text                = "❮",
                Foreground          = Theme.BrushAccent,
                FontSize            = 13,
                FontWeight          = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var tabLabel = new TextBlock
            {
                Text            = "P\nL\nA\nY\nE\nR\nS",
                Foreground      = Theme.BrushTextSecond,
                FontFamily      = Theme.FontFamily,
                FontSize        = 9,
                FontWeight      = FontWeights.Bold,
                TextAlignment   = TextAlignment.Center,
                Margin          = new Thickness(0, 6, 0, 0)
            };
            var tabStack = new StackPanel
            {
                Orientation         = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center
            };
            tabStack.Children.Add(_drawerArrow);
            tabStack.Children.Add(tabLabel);

            var tabBtn = new Border
            {
                Width           = 26,
                Height          = 110,
                Background      = new SolidColorBrush(Color.FromRgb(14, 20, 34)),
                BorderBrush     = Theme.BrushBorder,
                BorderThickness = new Thickness(1, 1, 0, 1),
                CornerRadius    = new CornerRadius(8, 0, 0, 8),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor          = Cursors.Hand,
                Child           = tabStack
            };
            Grid.SetColumn(tabBtn, 0);

            // ── Drawer panel ───────────────────────────────────────────────────
            _drawerPanel = new Border
            {
                Width           = DrawerWidth,
                Background      = new SolidColorBrush(Color.FromRgb(10, 14, 26)),
                BorderBrush     = Theme.BrushBorder,
                BorderThickness = new Thickness(1, 0, 0, 0),
                Effect          = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color     = Colors.Black,
                    Direction = 180,
                    ShadowDepth = 0,
                    BlurRadius  = 24,
                    Opacity     = 0.7
                }
            };

            var drawerContent = new DockPanel();

            // Header
            var headerBorder = new Border
            {
                Background      = new SolidColorBrush(Color.FromRgb(14, 20, 36)),
                BorderBrush     = Theme.BrushBorder,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding         = new Thickness(14, 10, 14, 10)
            };
            var headerRow = new Grid();
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var headerTitle = new TextBlock
            {
                Text       = "ROSTER",
                Foreground = Theme.BrushTextPrimary,
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeSmall,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            var closeBtn = new TextBlock
            {
                Text       = "✕",
                Foreground = Theme.BrushTextSecond,
                FontSize   = 11,
                Cursor     = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            closeBtn.MouseLeftButtonUp += (_, _) => ToggleDrawer();
            Grid.SetColumn(headerTitle, 0);
            Grid.SetColumn(closeBtn, 1);
            headerRow.Children.Add(headerTitle);
            headerRow.Children.Add(closeBtn);
            headerBorder.Child = headerRow;
            DockPanel.SetDock(headerBorder, Dock.Top);
            drawerContent.Children.Add(headerBorder);

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            var body = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };

            // ── Home section ───────────────────────────────────────────────────
            var homeSectionHdr = new Border
            {
                Background      = new SolidColorBrush(Color.FromArgb(40, 0, 188, 140)),
                BorderBrush     = new SolidColorBrush(Color.FromArgb(80, 0, 188, 140)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding         = new Thickness(14, 7, 14, 7)
            };
            _drawerHomeHdr = new TextBlock
            {
                Text       = Loc.Get("live.home"),
                Foreground = Theme.BrushAccent,
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeSmall,
                FontWeight = FontWeights.Bold
            };
            homeSectionHdr.Child = _drawerHomeHdr;
            body.Children.Add(homeSectionHdr);
            _drawerHome = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
            body.Children.Add(_drawerHome);

            // ── Away section ───────────────────────────────────────────────────
            var awaySectionHdr = new Border
            {
                Background      = new SolidColorBrush(Color.FromArgb(40, 60, 100, 220)),
                BorderBrush     = new SolidColorBrush(Color.FromArgb(80, 60, 100, 220)),
                BorderThickness = new Thickness(0, 1, 0, 1),
                Padding         = new Thickness(14, 7, 14, 7),
                Margin          = new Thickness(0, 10, 0, 0)
            };
            _drawerAwayHdr = new TextBlock
            {
                Text       = Loc.Get("live.away"),
                Foreground = Theme.BrushAccentBlue,
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeSmall,
                FontWeight = FontWeights.Bold
            };
            awaySectionHdr.Child = _drawerAwayHdr;
            body.Children.Add(awaySectionHdr);
            _drawerAway = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
            body.Children.Add(_drawerAway);

            scroll.Content = body;
            drawerContent.Children.Add(scroll);
            _drawerPanel.Child = drawerContent;
            Grid.SetColumn(_drawerPanel, 1);

            tabBtn.MouseLeftButtonUp += (_, _) => ToggleDrawer();

            host.Children.Add(tabBtn);
            host.Children.Add(_drawerPanel);
            return host;
        }

        private void ToggleDrawer()
        {
            _drawerOpen = !_drawerOpen;
            AnimateDrawer();
        }

        private void OpenDrawer()
        {
            if (_drawerOpen) return;
            _drawerOpen = true;
            AnimateDrawer();
        }

        private void AnimateDrawer()
        {
            var anim = new System.Windows.Media.Animation.DoubleAnimation
            {
                To             = _drawerOpen ? 0 : DrawerWidth,
                Duration       = TimeSpan.FromMilliseconds(220),
                EasingFunction = new System.Windows.Media.Animation.CubicEase
                    { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };
            _drawerSlide.BeginAnimation(TranslateTransform.XProperty, anim);
            _drawerArrow.Text = _drawerOpen ? "❯" : "❮";
        }

        private void RefreshDrawer()
        {
            _drawerHome.Children.Clear();
            _drawerAway.Children.Clear();

            if (_match != null)
            {
                _drawerHomeHdr.Text = _match.HomeTeamName.ToUpperInvariant();
                _drawerAwayHdr.Text = _match.AwayTeamName.ToUpperInvariant();
            }

            static Color PosColor(string pos) => pos.ToUpperInvariant() switch
            {
                "OH"  or "OUTSIDE"  => Color.FromRgb( 56, 139, 220),
                "MB"  or "MIDDLE"   => Color.FromRgb(220, 120,  56),
                "S"   or "SETTER"   => Color.FromRgb( 56, 190, 140),
                "L"   or "LIBERO"   => Color.FromRgb(220, 210,  56),
                "OPP" or "OPPOSITE" => Color.FromRgb(160,  80, 220),
                _                   => Color.FromRgb(120, 130, 150)
            };

            void AddPlayer(StackPanel target, Player p, int[] rotation, Color accentColor)
            {
                int rotIdx = Array.IndexOf(rotation, p.Number); // -1 if not in lineup
                bool inRotation = rotIdx >= 0;

                // Card outer border with left accent strip
                var card = new Grid { Margin = new Thickness(10, 2, 10, 2) };
                card.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3) });
                card.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var strip = new Border
                {
                    Background   = new SolidColorBrush(inRotation
                        ? accentColor
                        : Color.FromArgb(60, accentColor.R, accentColor.G, accentColor.B)),
                    CornerRadius = new CornerRadius(2, 0, 0, 2)
                };
                Grid.SetColumn(strip, 0);

                var cardInner = new Border
                {
                    Background      = new SolidColorBrush(inRotation
                        ? Color.FromArgb(25, accentColor.R, accentColor.G, accentColor.B)
                        : Color.FromRgb(14, 20, 34)),
                    BorderBrush     = new SolidColorBrush(Color.FromArgb(40, accentColor.R, accentColor.G, accentColor.B)),
                    BorderThickness = new Thickness(0, 1, 1, 1),
                    CornerRadius    = new CornerRadius(0, 4, 4, 0),
                    Padding         = new Thickness(8, 7, 8, 7)
                };

                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });  // badge
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // name
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });  // pos / rot

                // Jersey badge
                var badge = new Border
                {
                    Width        = 30,
                    Height       = 30,
                    CornerRadius = new CornerRadius(15),
                    Background   = new SolidColorBrush(Color.FromArgb(inRotation ? (byte)60 : (byte)30,
                        accentColor.R, accentColor.G, accentColor.B)),
                    BorderBrush  = new SolidColorBrush(Color.FromArgb(inRotation ? (byte)180 : (byte)60,
                        accentColor.R, accentColor.G, accentColor.B)),
                    BorderThickness = new Thickness(1.5),
                    Margin       = new Thickness(0, 0, 8, 0),
                    Child        = new TextBlock
                    {
                        Text                = p.Number.ToString(),
                        Foreground          = new SolidColorBrush(inRotation ? accentColor : Color.FromArgb(160, accentColor.R, accentColor.G, accentColor.B)),
                        FontFamily          = Theme.FontFamily,
                        FontSize            = 11,
                        FontWeight          = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment   = VerticalAlignment.Center
                    }
                };
                Grid.SetColumn(badge, 0);

                // Name + position
                var nameStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                nameStack.Children.Add(new TextBlock
                {
                    Text         = p.Name,
                    Foreground   = inRotation ? Theme.BrushTextPrimary : Theme.BrushTextSecond,
                    FontFamily   = Theme.FontFamily,
                    FontSize     = Theme.SizeSmall,
                    FontWeight   = inRotation ? FontWeights.SemiBold : FontWeights.Normal,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                if (!string.IsNullOrWhiteSpace(p.Position))
                {
                    var posColor = PosColor(p.Position);
                    nameStack.Children.Add(new Border
                    {
                        Background      = new SolidColorBrush(Color.FromArgb(40, posColor.R, posColor.G, posColor.B)),
                        BorderBrush     = new SolidColorBrush(Color.FromArgb(100, posColor.R, posColor.G, posColor.B)),
                        BorderThickness = new Thickness(1),
                        CornerRadius    = new CornerRadius(3),
                        Padding         = new Thickness(4, 1, 4, 1),
                        Margin          = new Thickness(0, 2, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Child           = new TextBlock
                        {
                            Text       = p.Position,
                            Foreground = new SolidColorBrush(Color.FromArgb(200, posColor.R, posColor.G, posColor.B)),
                            FontFamily = Theme.FontFamily,
                            FontSize   = 9,
                            FontWeight = FontWeights.SemiBold
                        }
                    });
                }
                Grid.SetColumn(nameStack, 1);

                // Rotation position pill (shown when player is in starting lineup)
                if (inRotation)
                {
                    var rotPill = new Border
                    {
                        Background      = new SolidColorBrush(Color.FromArgb(50, accentColor.R, accentColor.G, accentColor.B)),
                        BorderBrush     = new SolidColorBrush(Color.FromArgb(120, accentColor.R, accentColor.G, accentColor.B)),
                        BorderThickness = new Thickness(1),
                        CornerRadius    = new CornerRadius(4),
                        Padding         = new Thickness(5, 2, 5, 2),
                        Margin          = new Thickness(6, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        Child           = new TextBlock
                        {
                            Text       = $"P{rotIdx + 1}",
                            Foreground = new SolidColorBrush(accentColor),
                            FontFamily = Theme.FontFamily,
                            FontSize   = 9,
                            FontWeight = FontWeights.Bold
                        }
                    };
                    Grid.SetColumn(rotPill, 2);
                    row.Children.Add(rotPill);
                }

                row.Children.Add(badge);
                row.Children.Add(nameStack);
                cardInner.Child = row;
                Grid.SetColumn(cardInner, 1);
                card.Children.Add(strip);
                card.Children.Add(cardInner);
                target.Children.Add(card);
            }

            var homeAccent = Theme.Accent;
            var awayAccent = Color.FromRgb(60, 100, 220);

            foreach (var p in _homePlayers.Values.OrderBy(p => p.Number))
                AddPlayer(_drawerHome, p, _homeRotation, homeAccent);
            foreach (var p in _awayPlayers.Values.OrderBy(p => p.Number))
                AddPlayer(_drawerAway, p, _awayRotation, awayAccent);
        }

        private UIElement BuildLegendPanel()
        {
            var panel = new Border
            {
                Width           = 220,
                Background      = Theme.BrushBgCard,
                BorderBrush     = Theme.BrushBorder,
                BorderThickness = new Thickness(1, 0, 0, 0)
            };

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var sp = new StackPanel { Margin = new Thickness(10, 10, 10, 10) };

            void Section(string title)
            {
                sp.Children.Add(new TextBlock
                {
                    Text       = title,
                    Foreground = Theme.BrushAccent,
                    FontFamily = ConsoleFont,
                    FontSize   = 10,
                    FontWeight = FontWeights.Bold,
                    Margin     = new Thickness(0, 10, 0, 3)
                });
                sp.Children.Add(new Border { Height = 1, Background = Theme.BrushBorder, Margin = new Thickness(0, 0, 0, 4) });
            }

            void Row(string code, string desc, SolidColorBrush? color = null)
            {
                var row = new Grid { Margin = new Thickness(0, 1, 0, 1) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var codeBlock = new TextBlock
                {
                    Text       = code,
                    Foreground = color ?? ConsolePrompt,
                    FontFamily = ConsoleFont,
                    FontSize   = 10,
                    FontWeight = FontWeights.Bold
                };
                Grid.SetColumn(codeBlock, 0);

                var descBlock = new TextBlock
                {
                    Text         = desc,
                    Foreground   = ConsoleSys,
                    FontFamily   = ConsoleFont,
                    FontSize     = 10,
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetColumn(descBlock, 1);

                row.Children.Add(codeBlock);
                row.Children.Add(descBlock);
                sp.Children.Add(row);
            }

            // Header
            sp.Children.Add(new TextBlock
            {
                Text       = "CODE REFERENCE",
                Foreground = Theme.BrushTextPrimary,
                FontFamily = ConsoleFont,
                FontSize   = 10.5,
                FontWeight = FontWeights.Bold,
                Margin     = new Thickness(0, 0, 0, 2)
            });
            sp.Children.Add(new TextBlock
            {
                Text         = "[a]<##><action>[sub][zone][result]",
                Foreground   = ConsoleInput,
                FontFamily   = ConsoleFont,
                FontSize     = 9.5,
                TextWrapping = TextWrapping.Wrap,
                Margin       = new Thickness(0, 0, 0, 0)
            });

            Section("ACTIONS");
            Row("S", "Serve");
            Row("R", "Reception");
            Row("A", "Attack");
            Row("B", "Block");
            Row("D", "Dig");
            Row("E", "Set");
            Row("F", "FreeBall");

            Section("SERVE SUB");
            Row("H", "Float");
            Row("M", "Jump-float");
            Row("Q", "Jump-topspin");
            Row("T", "Underhand");

            Section("ATTACK COMBO");
            Row("X1", "Quick");
            Row("X5", "5 quick");
            Row("X6", "6 quick");
            Row("XP", "Pipe");
            Row("V5", "V5");
            Row("V6", "V6");
            Row("VP", "V-pipe");
            Row("PP", "Setter dump");

            Section("RESULTS");
            Row("#", "Perfect");
            Row("+", "Positive",  ConsoleOk);
            Row("!",  "Overpass",  ConsoleRally);
            Row("/",  "Freeball",  ConsoleSys);
            Row("-",  "Negative",  ConsoleRally);
            Row("=",  "Error",     ConsoleErr);

            Section("TEAM PREFIX");
            Row("a",  "Away team (none = home)");

            Section("COMMANDS");
            Row("/er h", "Home wins rally",  ConsoleRally);
            Row("/er a", "Away wins rally",  ConsoleRally);
            Row("/es", "End set",    ConsoleRally);
            Row("/em", "End match",  ConsoleErr);
            Row("/clear", "Clear log");
            Row("/help",  "Help");

            Section("EXAMPLE");
            sp.Children.Add(new TextBlock
            {
                Text         = "12SM6#",
                Foreground   = ConsoleOk,
                FontFamily   = ConsoleFont,
                FontSize     = 10,
                FontWeight   = FontWeights.Bold
            });
            sp.Children.Add(new TextBlock
            {
                Text         = "Home #12 jump-float serve zone 6 — perfect",
                Foreground   = ConsoleSys,
                FontFamily   = ConsoleFont,
                FontSize     = 9.5,
                TextWrapping = TextWrapping.Wrap,
                Margin       = new Thickness(0, 2, 0, 4)
            });
            sp.Children.Add(new TextBlock
            {
                Text         = "a17R+",
                Foreground   = ConsoleOk,
                FontFamily   = ConsoleFont,
                FontSize     = 10,
                FontWeight   = FontWeights.Bold
            });
            sp.Children.Add(new TextBlock
            {
                Text         = "Away #17 reception — positive",
                Foreground   = ConsoleSys,
                FontFamily   = ConsoleFont,
                FontSize     = 9.5,
                TextWrapping = TextWrapping.Wrap,
                Margin       = new Thickness(0, 2, 0, 0)
            });

            scroll.Content = sp;
            panel.Child    = scroll;
            return panel;
        }

        private UIElement BuildConsole()
        {
            var outer = new DockPanel { Background = ConsoleBg };

            // Input row at bottom
            var inputRow = new Border
            {
                Background      = Color.FromRgb(12, 15, 24) is Color bg ? new SolidColorBrush(bg) : ConsoleBg,
                BorderBrush     = Theme.BrushBorder,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding         = new Thickness(6, 4, 6, 4)
            };
            var inputGrid = new Grid();
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var prompt = new TextBlock
            {
                Text              = "▶  ",
                Foreground        = ConsolePrompt,
                FontFamily        = ConsoleFont,
                FontSize          = ConsoleFontSize,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(4, 0, 0, 0)
            };
            Grid.SetColumn(prompt, 0);

            _consoleInput = new TextBox
            {
                Background      = Brushes.Transparent,
                Foreground      = ConsoleInput,
                CaretBrush      = ConsolePrompt,
                FontFamily      = ConsoleFont,
                FontSize        = ConsoleFontSize,
                BorderThickness = new Thickness(0),
                Padding         = new Thickness(4, 2, 4, 2),
                Margin          = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _consoleInput.KeyDown += OnConsoleKeyDown;
            Grid.SetColumn(_consoleInput, 1);

            inputGrid.Children.Add(prompt);
            inputGrid.Children.Add(_consoleInput);
            inputRow.Child = inputGrid;
            DockPanel.SetDock(inputRow, Dock.Bottom);
            outer.Children.Add(inputRow);

            // Log area
            _consoleLines = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(8, 6, 8, 4) };
            _consoleScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Background = Brushes.Transparent,
                Content    = _consoleLines
            };
            outer.Children.Add(_consoleScroll);

            return outer;
        }

        // ── Console input handler ──────────────────────────────────────────────

        private void OnConsoleKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                string text = _consoleInput.Text.Trim();
                _consoleInput.Clear();
                if (string.IsNullOrEmpty(text)) return;

                _inputHistory.Insert(0, text);
                if (_inputHistory.Count > 100) _inputHistory.RemoveAt(_inputHistory.Count - 1);
                _historyIdx = -1;

                ProcessInput(text);
            }
            else if (e.Key == Key.Up)
            {
                e.Handled = true;
                if (_inputHistory.Count == 0) return;
                _historyIdx = Math.Min(_historyIdx + 1, _inputHistory.Count - 1);
                _consoleInput.Text = _inputHistory[_historyIdx];
                _consoleInput.SelectionStart = _consoleInput.Text.Length;
            }
            else if (e.Key == Key.Down)
            {
                e.Handled = true;
                _historyIdx = Math.Max(_historyIdx - 1, -1);
                _consoleInput.Text = _historyIdx >= 0 ? _inputHistory[_historyIdx] : "";
                _consoleInput.SelectionStart = _consoleInput.Text.Length;
            }
        }

        private void ProcessInput(string raw)
        {
            // Echo input
            AppendLine($"▶  {raw}", ConsoleInput);

            // Commands
            if (raw.StartsWith('/'))
            {
                string cmd = raw.ToLowerInvariant();
                if (cmd is "/er h" or "/er home") { CmdEndRally(homeWins: true);  return; }
                if (cmd is "/er a" or "/er away") { CmdEndRally(homeWins: false); return; }
                if (cmd is "/er" or "/end-rally") { AppendErr("Specify winner: /er h (home) or /er a (away)"); return; }
                if (cmd is "/es" or "/end-set")   { CmdEndSet();   return; }
                if (cmd is "/em" or "/end-match")  { CmdEndMatch(); return; }
                if (cmd is "/clear") { _consoleLines.Children.Clear(); return; }
                if (cmd is "/help")  { ShowHelp(); return; }
                AppendErr($"Unknown command: {raw}. Type /help.");
                return;
            }

            if (_match == null || _currentSet == null || _currentRally == null)
            {
                AppendErr("No match loaded.");
                return;
            }

            // Handle compound codes (split on '.')
            var parts = raw.Split('.');
            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part)) continue;
                ProcessToken(part.Trim());
            }
        }

        private void ProcessToken(string code)
        {
            var tok = TryParseToken(code, out string err);
            if (tok == null) { AppendErr($"  Parse error: {err}"); return; }

            var players = tok.IsAway ? _awayPlayers : _homePlayers;
            var team    = tok.IsAway ? _awayTeam    : _homeTeam;

            if (team == null) { AppendErr("  Team not loaded."); return; }

            if (!players.TryGetValue(tok.PlayerNum, out var player))
            {
                AppendErr($"  Player #{tok.PlayerNum} not found in {(tok.IsAway ? "away" : "home")} team.");
                return;
            }

            int? zone = tok.Zone ?? (_selectedZone > 0 ? _selectedZone : (int?)null);

            var action = new Models.Action
            {
                RallyId     = _currentRally!.Id,
                PlayerId    = player.Id,
                TeamId      = team.Id,
                ActionType  = tok.Action,
                Result      = tok.Result,
                Zone        = zone,
                PlayerName  = player.Name,
                TeamName    = team.Name,
                SetNumber   = _currentSet!.SetNumber,
                RallyNumber = _currentRally.RallyNumber
            };
            action.Id = _rallyRepo.InsertAction(action);
            RefreshHeatmap();

            // Formatted confirmation line
            string teamTag  = tok.IsAway ? "AWAY" : "HOME";
            string subInfo  = string.IsNullOrEmpty(tok.SubType) ? "" : $"  [{tok.SubType}]";
            string zoneInfo = zone.HasValue ? $"  Z{zone}" : "";
            string resInfo  = ResultLabel(tok.Result);
            AppendLine($"   ↳ {teamTag}  #{player.Number} {player.Name}  {tok.Action}{subInfo}{zoneInfo}  {resInfo}", ConsoleOk);
        }

        // ── DataVolley code parser ─────────────────────────────────────────────
        // Format: [a?][NN][T][SUB?][Z?][R?]
        //   team:    blank=home  a=away
        //   NN:      1-2 digit jersey number
        //   T:       S R A B D E F  (or implicit A for X/V/P combo codes)
        //   SUB:     H M Q T (serve subtypes)  |  X1 X5 X6 V5 XP PP … (attack combos)
        //   Z:       1-6  (optional zone digit)
        //   R:       # + ! - / =

        private sealed record DvToken(
            bool IsAway, int PlayerNum, string Action, string SubType, int? Zone, string Result);

        private static DvToken? TryParseToken(string raw, out string error)
        {
            error = "";
            int i = 0;

            // Team prefix: 'a' followed by a digit = away
            bool isAway = false;
            if (i < raw.Length && raw[i] == 'a' && i + 1 < raw.Length && char.IsDigit(raw[i + 1]))
            { isAway = true; i++; }

            // Player number (1-2 digits)
            int numStart = i;
            while (i < raw.Length && char.IsDigit(raw[i]) && i - numStart < 2) i++;
            if (i == numStart) { error = "Missing player number"; return null; }
            int playerNum = int.Parse(raw[numStart..i]);

            if (i >= raw.Length) { error = "Missing action type after player number"; return null; }

            // Action type (single letter, or implicit A for combo prefixes)
            string action;
            char c = char.ToUpper(raw[i]);
            switch (c)
            {
                case 'S': action = "Serve";     i++; break;
                case 'R': action = "Reception"; i++; break;
                case 'A': action = "Attack";    i++; break;
                case 'B': action = "Block";     i++; break;
                case 'D': action = "Dig";       i++; break;
                case 'E': action = "Set";       i++; break;
                case 'F': action = "Free";      i++; break;
                // Attack combo codes — X*, V*, P* — action is implicit Attack, don't advance
                case 'X': case 'V': case 'P': action = "Attack"; break;
                // Result char with no action letter — imply Reception (e.g. "7#" shorthand)
                case '#': case '+': case '!': case '-': case '/': case '=':
                    action = "Reception"; break;   // don't advance i; result parser handles it
                default:
                    error = $"Unknown action '{raw[i]}'  (expected S R A B D E F or combo X/V/P)";
                    return null;
            }

            // Sub-type / combo: 0-2 chars.
            // First char must be a letter (non-digit, non-result).
            // Second char may be a letter OR a digit, but only if another digit follows
            // (so the second digit is the subtype suffix, not the zone).
            // Examples: "X6 5 #" → subType="X6" zone=5; "Q 6 #" → subType="Q" zone=6
            int subStart = i;
            if (i < raw.Length && !char.IsDigit(raw[i]) && !"#+!-/=".Contains(raw[i]))
            {
                i++; // first letter
                if (i < raw.Length && !"#+!-/=".Contains(raw[i]))
                {
                    bool isDigit = char.IsDigit(raw[i]);
                    // Include digit only if another digit comes after it (that digit will be the zone)
                    bool nextIsDigit = isDigit && i + 1 < raw.Length && char.IsDigit(raw[i + 1]);
                    if (!isDigit || nextIsDigit)
                        i++; // second char (letter, or digit with zone following)
                }
            }
            string subType = raw[subStart..i].ToUpper();

            // Zone (single digit 1-9)
            int? zone = null;
            if (i < raw.Length && char.IsDigit(raw[i]) && raw[i] != '0')
            { zone = raw[i] - '0'; i++; }

            // Result
            string result = "";
            if (i < raw.Length && "#+!-/=".Contains(raw[i]))
                result = raw[i].ToString();

            return new DvToken(isAway, playerNum, action, subType, zone, result);
        }

        // ── Match control commands ─────────────────────────────────────────────

        private void CmdEndRally(bool homeWins)
        {
            if (_currentRally == null || _currentSet == null || _match == null) return;

            if (homeWins) _currentSet.HomePoints++;
            else          _currentSet.AwayPoints++;

            _setRepo.UpdatePoints(_currentSet.Id, _currentSet.HomePoints, _currentSet.AwayPoints, false);
            UpdateSetLabel();

            ApplyRotation(homeWins);

            string winner = homeWins ? _match.HomeTeamName : _match.AwayTeamName;
            AppendRally($"── Rally {_currentRally.RallyNumber} ended  →  {winner} point  ({_currentSet.HomePoints}:{_currentSet.AwayPoints}) ──");
            StartNewRally();
            AppendRally($"── Rally {_currentRally?.RallyNumber} started ──");
        }

        private void CmdEndSet()
        {
            if (_match == null || _currentSet == null) return;

            _currentSet.IsComplete = true;
            _setRepo.UpdatePoints(_currentSet.Id, _currentSet.HomePoints, _currentSet.AwayPoints, true);

            bool homeWon = _currentSet.HomePoints > _currentSet.AwayPoints;
            if (homeWon) _match.HomeScore++; else _match.AwayScore++;
            _matchRepo.UpdateScore(_match.Id, _match.HomeScore, _match.AwayScore, "Live");

            int nextSet = _currentSet.SetNumber + 1;
            AppendRally($"══ SET {_currentSet.SetNumber} FINISHED  {_currentSet.HomePoints}:{_currentSet.AwayPoints}  ({(homeWon ? _match.HomeTeamName : _match.AwayTeamName)} wins set) ══");

            _currentSet    = new Set { MatchId = _match.Id, SetNumber = nextSet };
            _currentSet.Id = _setRepo.Insert(_currentSet);
            UpdateSetLabel();
            StartNewRally();
            AppendRally($"══ SET {nextSet} STARTED ══");
            Dispatcher.BeginInvoke((System.Action)ShowLineupDialog);
        }

        private void CmdEndMatch()
        {
            if (_match == null) return;
            _matchRepo.UpdateScore(_match.Id, _match.HomeScore, _match.AwayScore, "Finished");
            AppendRally($"══ MATCH FINISHED  {_match.HomeTeamName} {_match.HomeScore} : {_match.AwayScore} {_match.AwayTeamName} ══");
        }

        // ── Internal helpers ───────────────────────────────────────────────────

        private void StartNewRally()
        {
            if (_match == null || _currentSet == null) return;
            var existing = _rallyRepo.GetActionsForMatch(_match.Id);
            int rallyNum = existing.Count == 0 ? 1 : existing.Max(a => a.RallyNumber) + 1;
            _currentRally    = new Rally { MatchId = _match.Id, SetId = _currentSet.Id, RallyNumber = rallyNum };
            _currentRally.Id = _rallyRepo.InsertRally(_currentRally);
            _selectedZone    = 0;
        }

        private void RefreshHeatmap()
        {
            if (_match == null) return;
            var svc    = new StatsService();
            int homeId = _homeTeam?.Id ?? _match.HomeTeamId;
            int awayId = _awayTeam?.Id ?? _match.AwayTeamId;
            _heatmap.HomeLabel = _homeTeam?.Name ?? "";
            _heatmap.AwayLabel = _awayTeam?.Name ?? "";
            _heatmap.SetData(
                svc.GetZoneData(homeId, _heatmapAction, _match.Id),
                null,
                svc.GetZoneData(awayId, _heatmapAction, _match.Id));
        }

        private void SetActiveHeatmapTab(Button btn)
        {
            foreach (var b in new[] { _btnTabAttack, _btnTabServe })
            {
                b.Foreground  = Theme.BrushTextSecond;
                b.Background  = Brushes.Transparent;
                b.BorderBrush = Brushes.Transparent;
            }
            btn.Foreground  = Theme.BrushNavActive;
            btn.Background  = new SolidColorBrush(Color.FromArgb(30, 0, 188, 140));
            btn.BorderBrush = Theme.BrushNavActive;
        }

        private void UpdateSetLabel()
        {
            if (_currentSet != null)
            {
                _lblHomeScore.Text  = _currentSet.HomePoints.ToString();
                _lblAwayScore.Text  = _currentSet.AwayPoints.ToString();
                _lblCurrentSet.Text = $"Set {_currentSet.SetNumber}";
                _lblSetScore.Text   = _match != null ? $"{_match.HomeScore} : {_match.AwayScore}" : "";
            }
        }

        private void ShowHelp()
        {
            AppendSys("──── Help ────────────────────────────────────────────");
            AppendSys("Code format:  [a]<num><action>[sub][zone][result]");
            AppendSys("  prefix a  = away team  (none = home team)");
            AppendSys("  num       = 1-2 digit jersey number");
            AppendSys("  action:   S=Serve  R=Reception  A=Attack");
            AppendSys("            B=Block  D=Dig  E=Set  F=FreeBall");
            AppendSys("  sub-type (serve):  H=float  M=jump-float");
            AppendSys("                     Q=jump-topspin  T=underhand");
            AppendSys("  sub-type (attack): X1 X5 X6 XP XM XD XC");
            AppendSys("                     V5 V6 VP  PP=setter-dump");
            AppendSys("  zone:     1-6  (standard volleyball positions)");
            AppendSys("  result:   #=perfect  +=positive  !=overpass");
            AppendSys("            /=freeball -=negative  ==error");
            AppendSys("Compound:   12SM6.17#  (serve then reception)");
            AppendSys("Commands:   /er h|a  /es  /em  /clear  /help");
            AppendSys("Examples:");
            AppendSys("  12SM6#        home #12 jump-float serve  Z6  perfect");
            AppendSys("  a17R+         away #17 reception         positive");
            AppendSys("  04X52#        home #04 attack X5  Z2     point");
            AppendSys("  a6X65#        away #6  attack X6  Z5     point");
            AppendSys("  12SM56.17#    compound: serve + reception");
            AppendSys("──────────────────────────────────────────────────────");
        }

        // ── Console append helpers ─────────────────────────────────────────────

        private void AppendLine(string text, SolidColorBrush color)
        {
            _consoleLines.Children.Add(new TextBlock
            {
                Text            = text,
                Foreground      = color,
                FontFamily      = ConsoleFont,
                FontSize        = ConsoleFontSize,
                TextWrapping    = TextWrapping.Wrap,
                Margin          = new Thickness(0, 1, 0, 1)
            });
            _consoleScroll.ScrollToBottom();
        }

        private void AppendOk(string text)    => AppendLine(text, ConsoleOk);
        private void AppendErr(string text)   => AppendLine(text, ConsoleErr);
        private void AppendSys(string text)   => AppendLine(text, ConsoleSys);
        private void AppendRally(string text) => AppendLine(text, ConsoleRally);

        private static string ResultLabel(string r) => r switch
        {
            "#" => "[#] PERFECT",  "+" => "[+] POSITIVE", "!" => "[!] OVERPASS",
            "/" => "[/] FREEBALL", "-" => "[-] NEGATIVE",  "=" => "[=] ERROR",
            _ => string.IsNullOrEmpty(r) ? "" : $"[{r}]"
        };

        private static string FormatLoadedAction(Models.Action a)
        {
            string zone = a.Zone.HasValue ? $"Z{a.Zone}" : "";
            return $"S{a.SetNumber} R{a.RallyNumber}  {a.PlayerName}  {a.ActionType}  {a.Result}  {zone}";
        }

        // ── Button factories ───────────────────────────────────────────────────

        private static Button MakeZoneButton(string text)
        {
            var b = new Button
            {
                Content         = text,
                Background      = Theme.BrushBgHover,
                Foreground      = Theme.BrushTextPrimary,
                FontFamily      = Theme.FontFamily,
                FontSize        = Theme.SizeH3,
                BorderBrush     = Theme.BrushBorder,
                BorderThickness = new Thickness(1),
                Margin          = new Thickness(2),
                Style           = (Style)Application.Current.Resources["FlatButton"]
            };
            return b;
        }

        private static Button MakeCtrlButton(string text, Color bg)
        {
            var b = new Button
            {
                Content         = text,
                Height          = 30,
                Background      = new SolidColorBrush(bg),
                Foreground      = Brushes.White,
                FontFamily      = Theme.FontFamily,
                FontSize        = Theme.SizeBody,
                Style           = (Style)Application.Current.Resources["FlatButton"]
            };
            return b;
        }
    }
}

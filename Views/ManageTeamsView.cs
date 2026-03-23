using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VolleyStatsPro.Data;
using VolleyStatsPro.Helpers;
using VolleyStatsPro.Models;

namespace VolleyStatsPro.Views
{
    public class ManageTeamsView : System.Windows.Controls.UserControl
    {
        private readonly TeamRepository   _teamRepo   = new();
        private readonly PlayerRepository _playerRepo = new();

        private ListBox _teamList   = null!;
        private ListBox _playerList = null!;
        private Team?   _selectedTeam;

        public ManageTeamsView()
        {
            Background = Theme.BrushBgDark;
            Content    = BuildUI();
            LoadTeams();
        }

        // ── UI ─────────────────────────────────────────────────────────────────

        private UIElement BuildUI()
        {
            var outer = new DockPanel { Background = Theme.BrushBgDark };

            var header = BuildHeader();
            DockPanel.SetDock(header, Dock.Top);
            outer.Children.Add(header);

            var mainGrid = new Grid { Margin = new Thickness(14, 0, 14, 14) };
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var teamsCard = BuildTeamsCard();
            Grid.SetColumn(teamsCard, 0);
            mainGrid.Children.Add(teamsCard);

            var splitter = new GridSplitter
            {
                Width               = 6,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background          = Theme.BrushBgDark
            };
            Grid.SetColumn(splitter, 1);
            mainGrid.Children.Add(splitter);

            var playersCard = BuildPlayersCard();
            Grid.SetColumn(playersCard, 2);
            mainGrid.Children.Add(playersCard);

            outer.Children.Add(mainGrid);
            return outer;
        }

        private UIElement BuildHeader()
        {
            var row = new Grid
            {
                Background = Theme.BrushBgDark,
                Margin     = new Thickness(20, 14, 20, 12)
            };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titles = new StackPanel();
            titles.Children.Add(new TextBlock
            {
                Text       = Loc.Get("manage.title"),
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeH2,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.BrushTextPrimary
            });
            titles.Children.Add(new TextBlock
            {
                Text       = Loc.Get("manage.subtitle"),
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeBody,
                Foreground = Theme.BrushTextSecond,
                Margin     = new Thickness(0, 4, 0, 0)
            });
            Grid.SetColumn(titles, 0);
            row.Children.Add(titles);

            var btnAdd = MakeButton(Loc.Get("manage.addteam"), Theme.BrushAccentBlue);
            btnAdd.VerticalAlignment = VerticalAlignment.Center;
            btnAdd.Click += (_, _) => AddTeam();
            Grid.SetColumn(btnAdd, 1);
            row.Children.Add(btnAdd);

            return row;
        }

        private UIElement BuildTeamsCard()
        {
            var card  = MakeCard();
            var inner = new DockPanel();

            var titleBar = new Border
            {
                Background      = Theme.BrushBgCard,
                BorderBrush     = Theme.BrushBorder,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Height          = 36
            };
            titleBar.Child = new TextBlock
            {
                Text              = Loc.Get("manage.teams"),
                FontFamily        = Theme.FontFamily,
                FontSize          = Theme.SizeH3,
                FontWeight        = FontWeights.Bold,
                Foreground        = Theme.BrushTextPrimary,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(12, 0, 0, 0)
            };
            DockPanel.SetDock(titleBar, Dock.Top);
            inner.Children.Add(titleBar);

            var btnDel = MakeButton(Loc.Get("manage.delteam"), Theme.BrushDanger);
            btnDel.Margin = new Thickness(8, 8, 8, 8);
            btnDel.Click += (_, _) => DeleteTeam();
            var btnBar = new Border
            {
                Background      = Theme.BrushBgCard,
                BorderBrush     = Theme.BrushBorder,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Height          = 44,
                Child           = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children    = { btnDel }
                }
            };
            DockPanel.SetDock(btnBar, Dock.Bottom);
            inner.Children.Add(btnBar);

            _teamList = new ListBox
            {
                Background      = Theme.BrushBgPanel,
                Foreground      = Theme.BrushTextPrimary,
                FontFamily      = Theme.FontFamily,
                FontSize        = Theme.SizeBody,
                BorderThickness = new Thickness(0)
            };
            _teamList.ItemContainerStyle = (Style)Application.Current.Resources["DarkListBoxItem"];
            _teamList.SelectionChanged  += (_, _) => OnTeamSelected();
            inner.Children.Add(_teamList);

            card.Child = inner;
            return card;
        }

        private UIElement BuildPlayersCard()
        {
            var card  = MakeCard();
            var inner = new DockPanel();

            var titleBar = new Border
            {
                Background      = Theme.BrushBgCard,
                BorderBrush     = Theme.BrushBorder,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Height          = 36
            };
            titleBar.Child = new TextBlock
            {
                Text              = Loc.Get("manage.players"),
                FontFamily        = Theme.FontFamily,
                FontSize          = Theme.SizeH3,
                FontWeight        = FontWeights.Bold,
                Foreground        = Theme.BrushTextPrimary,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(12, 0, 0, 0)
            };
            DockPanel.SetDock(titleBar, Dock.Top);
            inner.Children.Add(titleBar);

            var btnEdit = MakeButton(Loc.Get("common.edit"), Theme.BrushAccentBlue);
            btnEdit.Margin = new Thickness(8, 8, 0, 8);
            btnEdit.Click += (_, _) => EditPlayer();

            var btnDel = MakeButton(Loc.Get("common.delete"), Theme.BrushDanger);
            btnDel.Margin = new Thickness(6, 8, 0, 8);
            btnDel.Click += (_, _) => DeletePlayer();

            var btnAdd = MakeButton(Loc.Get("manage.addplayer"), Theme.BrushAccent);
            btnAdd.Margin               = new Thickness(0, 8, 8, 8);
            btnAdd.HorizontalAlignment  = HorizontalAlignment.Right;
            btnAdd.Click               += (_, _) => AddPlayer();

            var btnBarGrid = new Grid();
            btnBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            btnBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var leftBtns = new StackPanel { Orientation = Orientation.Horizontal };
            leftBtns.Children.Add(btnEdit);
            leftBtns.Children.Add(btnDel);
            Grid.SetColumn(leftBtns, 0);
            btnBarGrid.Children.Add(leftBtns);
            Grid.SetColumn(btnAdd, 1);
            btnBarGrid.Children.Add(btnAdd);

            var btnBar = new Border
            {
                Background      = Theme.BrushBgCard,
                BorderBrush     = Theme.BrushBorder,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Height          = 44,
                Child           = btnBarGrid
            };
            DockPanel.SetDock(btnBar, Dock.Bottom);
            inner.Children.Add(btnBar);

            _playerList = new ListBox
            {
                Background      = Theme.BrushBgPanel,
                Foreground      = Theme.BrushTextPrimary,
                FontFamily      = Theme.FontFamily,
                FontSize        = Theme.SizeBody,
                BorderThickness = new Thickness(0)
            };
            _playerList.ItemContainerStyle = (Style)Application.Current.Resources["DarkListBoxItem"];
            inner.Children.Add(_playerList);

            card.Child = inner;
            return card;
        }

        // ── Data ───────────────────────────────────────────────────────────────

        private void LoadTeams()
        {
            _teamList.Items.Clear();
            foreach (var t in _teamRepo.GetAll()) _teamList.Items.Add(t);
            if (_teamList.Items.Count > 0) _teamList.SelectedIndex = 0;
        }

        private void OnTeamSelected()
        {
            _selectedTeam = _teamList.SelectedItem as Team;
            if (_selectedTeam == null) return;
            LoadPlayers();
        }

        private void LoadPlayers()
        {
            if (_selectedTeam == null) return;
            _playerList.Items.Clear();
            foreach (var p in _playerRepo.GetByTeam(_selectedTeam.Id)) _playerList.Items.Add(p);
        }

        // ── Actions ────────────────────────────────────────────────────────────

        private void AddTeam()
        {
            var dlg = new TeamEditDialog { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true && dlg.Result != null)
            {
                _teamRepo.Insert(dlg.Result);
                LoadTeams();
            }
        }

        private void DeleteTeam()
        {
            if (_selectedTeam == null) return;
            if (MessageBox.Show(string.Format(Loc.Get("manage.del.team"), _selectedTeam.Name),
                Loc.Get("common.confirm"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _teamRepo.Delete(_selectedTeam.Id);
                _selectedTeam = null;
                _playerList.Items.Clear();
                LoadTeams();
            }
        }

        private void AddPlayer()
        {
            if (_selectedTeam == null) { MessageBox.Show(Loc.Get("manage.val.selectteam")); return; }
            var dlg = new PlayerEditDialog(_selectedTeam.Id) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true && dlg.Result != null)
            {
                _playerRepo.Insert(dlg.Result);
                LoadPlayers();
            }
        }

        private void EditPlayer()
        {
            if (_playerList.SelectedItem is not Player p) return;
            var dlg = new PlayerEditDialog(p.TeamId, p) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true && dlg.Result != null)
            {
                _playerRepo.Update(dlg.Result);
                LoadPlayers();
            }
        }

        private void DeletePlayer()
        {
            if (_playerList.SelectedItem is not Player p) return;
            if (MessageBox.Show(string.Format(Loc.Get("manage.del.player"), p.Name), Loc.Get("common.confirm"),
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _playerRepo.Delete(p.Id);
                LoadPlayers();
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static Border MakeCard() => new Border
        {
            Background      = Theme.BrushBgCard,
            BorderBrush     = Theme.BrushBorder,
            BorderThickness = new Thickness(1)
        };

        private static Button MakeButton(string text, SolidColorBrush bg) => new Button
        {
            Content         = text,
            Background      = bg,
            Foreground      = Brushes.White,
            FontFamily      = Theme.FontFamily,
            FontSize        = Theme.SizeSmall,
            Padding         = new Thickness(12, 0, 12, 0),
            Height          = 28,
            BorderThickness = new Thickness(0),
            Cursor          = Cursors.Hand,
            Style           = (Style)Application.Current.Resources["FlatButton"]
        };
    }

    // ─── Team Edit Dialog ─────────────────────────────────────────────────────

    public class TeamEditDialog : Window
    {
        public Team? Result;
        private TextBox _name  = null!;
        private TextBox _short = null!;
        private string  _color = "#2196F3";
        private Border  _colorPreview = null!;

        public TeamEditDialog(Team? existing = null)
        {
            Title                   = existing == null ? Loc.Get("manage.addteam.dlg") : Loc.Get("manage.editteam.dlg");
            Width                   = 360;
            Height                  = 250;
            Background              = Theme.BrushBgPanel;
            Foreground              = Theme.BrushTextPrimary;
            ResizeMode              = ResizeMode.NoResize;
            WindowStartupLocation   = WindowStartupLocation.CenterOwner;

            var grid = new Grid { Margin = new Thickness(16) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _name  = DlgTextBox();
            _short = DlgTextBox();
            AddRow(grid, Loc.Get("manage.teamname"), _name,  0);
            AddRow(grid, Loc.Get("manage.shortname"), _short, 1);

            // Color row
            _colorPreview = new Border
            {
                Width           = 28,
                Height          = 20,
                Background      = new SolidColorBrush(HexColor(_color)),
                BorderBrush     = Theme.BrushBorder,
                BorderThickness = new Thickness(1),
                Margin          = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            var btnPickColor = DlgButton(Loc.Get("manage.pickcolor"));
            btnPickColor.Click += (_, _) => PickColor();
            var colorRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            colorRow.Children.Add(btnPickColor);
            colorRow.Children.Add(_colorPreview);
            AddRow(grid, Loc.Get("manage.color"), colorRow, 2);

            // Buttons
            var ok = DlgButton(Loc.Get("common.save"));
            ok.Background = Theme.BrushAccentBlue;
            ok.Foreground = Brushes.White;
            ok.Margin     = new Thickness(0, 16, 0, 0);
            ok.Click += (_, _) => Save();
            var cancel = DlgButton(Loc.Get("common.cancel"));
            cancel.Margin = new Thickness(8, 16, 0, 0);
            cancel.Click += (_, _) => { DialogResult = false; Close(); };
            var btnRow = new StackPanel
            {
                Orientation         = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            btnRow.Children.Add(ok);
            btnRow.Children.Add(cancel);
            Grid.SetRow(btnRow, 4); Grid.SetColumn(btnRow, 0); Grid.SetColumnSpan(btnRow, 2);
            grid.Children.Add(btnRow);

            if (existing != null)
            {
                _name.Text  = existing.Name;
                _short.Text = existing.ShortName;
                _color      = existing.Color;
                _colorPreview.Background = new SolidColorBrush(HexColor(_color));
                Result = existing;
            }

            Content = grid;
            Theme.ApplyDialogChrome(this, Title);
        }

        private void AddRow(Grid g, string label, UIElement ctrl, int row)
        {
            var lbl = new TextBlock
            {
                Text              = label,
                Foreground        = Theme.BrushTextSecond,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily        = Theme.FontFamily,
                FontSize          = Theme.SizeBody
            };
            Grid.SetRow(lbl, row);  Grid.SetColumn(lbl, 0);
            g.Children.Add(lbl);
            if (ctrl is FrameworkElement fe) fe.Margin = new Thickness(0, 4, 0, 4);
            Grid.SetRow(ctrl, row); Grid.SetColumn(ctrl, 1);
            g.Children.Add(ctrl);
        }

        private void PickColor()
        {
            var dlg = new ColorPickerDialog(_color) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                _color = dlg.ResultHex;
                _colorPreview.Background = new SolidColorBrush(HexColor(_color));
            }
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(_name.Text)) { MessageBox.Show(Loc.Get("manage.val.name")); return; }
            if (Result == null) Result = new Team();
            Result.Name      = _name.Text.Trim();
            Result.ShortName = _short.Text.Trim();
            Result.Color     = _color;
            DialogResult = true; Close();
        }

        internal static Color HexColor(string hex)
        {
            try { return (Color)ColorConverter.ConvertFromString(hex); }
            catch { return Theme.AccentBlue; }
        }

        private static TextBox DlgTextBox() => new TextBox
        {
            Background      = Theme.BrushBgCard,
            Foreground      = Theme.BrushTextPrimary,
            FontFamily      = Theme.FontFamily,
            FontSize        = Theme.SizeBody,
            BorderBrush     = Theme.BrushBorder,
            BorderThickness = new Thickness(1),
            Padding         = new Thickness(4, 2, 4, 2)
        };

        private static Button DlgButton(string t) => new Button
        {
            Content         = t,
            Height          = 28,
            Padding         = new Thickness(12, 0, 12, 0),
            Background      = Theme.BrushBgHover,
            Foreground      = Theme.BrushTextPrimary,
            FontFamily      = Theme.FontFamily,
            FontSize        = Theme.SizeBody,
            BorderThickness = new Thickness(1),
            BorderBrush     = Theme.BrushBorder
        };
    }

    // ─── Player Edit Dialog ───────────────────────────────────────────────────

    public class PlayerEditDialog : Window
    {
        public Player? Result;
        private TextBox  _name     = null!;
        private TextBox  _number   = null!;
        private ComboBox _position = null!;
        private readonly int _teamId;

        public PlayerEditDialog(int teamId, Player? existing = null)
        {
            _teamId               = teamId;
            Title                 = existing == null ? Loc.Get("manage.addplayer.dlg") : Loc.Get("manage.editplayer.dlg");
            Width                 = 340;
            Height                = 260;
            Background            = Theme.BrushBgPanel;
            Foreground            = Theme.BrushTextPrimary;
            ResizeMode            = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var grid = new Grid { Margin = new Thickness(16) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _name   = DlgTextBox();
            _number = DlgTextBox();
            _position = new ComboBox
            {
                Background  = Theme.BrushBgCard,
                Foreground  = Theme.BrushTextPrimary,
                FontFamily  = Theme.FontFamily,
                FontSize    = Theme.SizeBody,
                IsEditable  = false,
                Style       = (Style)Application.Current.Resources["DarkComboBox"]
            };
            foreach (var pos in new[] { "OH", "MB", "S", "L", "OPP", "DS" })
                _position.Items.Add(pos);

            AddRow(grid, Loc.Get("common.name"),    _name,     0);
            AddRow(grid, Loc.Get("manage.number"),  _number,   1);
            AddRow(grid, Loc.Get("manage.position"), _position, 2);

            var ok = DlgButton(Loc.Get("common.save"));
            ok.Background = Theme.BrushAccentBlue;
            ok.Foreground = Brushes.White;
            ok.Margin     = new Thickness(0, 16, 0, 0);
            ok.Click     += (_, _) => Save();
            var cancel = DlgButton(Loc.Get("common.cancel"));
            cancel.Margin = new Thickness(8, 16, 0, 0);
            cancel.Click += (_, _) => { DialogResult = false; Close(); };
            var btnRow = new StackPanel
            {
                Orientation         = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            btnRow.Children.Add(ok);
            btnRow.Children.Add(cancel);
            Grid.SetRow(btnRow, 4); Grid.SetColumn(btnRow, 0); Grid.SetColumnSpan(btnRow, 2);
            grid.Children.Add(btnRow);

            if (existing != null)
            {
                _name.Text           = existing.Name;
                _number.Text         = existing.Number.ToString();
                _position.SelectedItem = existing.Position;
                Result = existing;
            }
            else { _position.SelectedIndex = 0; _number.Text = "1"; }

            Content = grid;
            Theme.ApplyDialogChrome(this, Title);
        }

        private void AddRow(Grid g, string label, UIElement ctrl, int row)
        {
            var lbl = new TextBlock
            {
                Text              = label,
                Foreground        = Theme.BrushTextSecond,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily        = Theme.FontFamily,
                FontSize          = Theme.SizeBody
            };
            Grid.SetRow(lbl, row);  Grid.SetColumn(lbl, 0);
            g.Children.Add(lbl);
            if (ctrl is FrameworkElement fe) fe.Margin = new Thickness(0, 4, 0, 4);
            Grid.SetRow(ctrl, row); Grid.SetColumn(ctrl, 1);
            g.Children.Add(ctrl);
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(_name.Text)) { MessageBox.Show(Loc.Get("manage.val.name")); return; }
            if (!int.TryParse(_number.Text, out int num) || num < 1 || num > 99)
            { MessageBox.Show(Loc.Get("manage.val.number")); return; }
            if (Result == null) Result = new Player { TeamId = _teamId };
            Result.Name     = _name.Text.Trim();
            Result.Number   = num;
            Result.Position = _position.SelectedItem?.ToString() ?? "";
            DialogResult = true; Close();
        }

        private static TextBox DlgTextBox() => new TextBox
        {
            Background      = Theme.BrushBgCard,
            Foreground      = Theme.BrushTextPrimary,
            FontFamily      = Theme.FontFamily,
            FontSize        = Theme.SizeBody,
            BorderBrush     = Theme.BrushBorder,
            BorderThickness = new Thickness(1),
            Padding         = new Thickness(4, 2, 4, 2)
        };

        private static Button DlgButton(string t) => new Button
        {
            Content         = t,
            Height          = 28,
            Padding         = new Thickness(12, 0, 12, 0),
            Background      = Theme.BrushBgHover,
            Foreground      = Theme.BrushTextPrimary,
            FontFamily      = Theme.FontFamily,
            FontSize        = Theme.SizeBody,
            BorderThickness = new Thickness(1),
            BorderBrush     = Theme.BrushBorder
        };
    }

    // ─── Custom Color Picker Dialog ───────────────────────────────────────────

    internal class ColorPickerDialog : Window
    {
        public string ResultHex { get; private set; } = "#2196F3";

        // HSV state (source of truth)
        private double _h = 210, _s = 0.78, _v = 0.96;
        private bool _updating;

        // SV canvas
        private Canvas    _svCanvas  = null!;
        private Rectangle _svWhiteH  = null!;   // white → hue gradient
        private Rectangle _svDarkV   = null!;   // transparent → black gradient
        private Ellipse   _svThumb   = null!;

        // Hue strip
        private Canvas    _hueCanvas = null!;
        private Rectangle _hueLine   = null!;   // thumb line

        // Other controls
        private Border  _previewNew = null!;
        private TextBox _hexBox     = null!;
        private TextBox _tbR        = null!;
        private TextBox _tbG        = null!;
        private TextBox _tbB        = null!;

        private const double SvSize   = 240;
        private const double HueW     = 22;
        private const double HueH     = SvSize;

        public ColorPickerDialog(string initialHex)
        {
            Title                 = Loc.Get("manage.pickcolor");
            Width                 = 360;
            Height                = 440;
            ResizeMode            = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background            = Theme.BrushBgPanel;

            var c = ParseHex(initialHex);
            ColorToHsv(c, out _h, out _s, out _v);
            ResultHex = ToHex(c);

            Content = Build();
            Theme.ApplyDialogChrome(this, Title);
            RefreshAll(null);
        }

        // ── Build UI ──────────────────────────────────────────────────────────

        private UIElement Build()
        {
            var root = new StackPanel { Margin = new Thickness(16) };

            // ── Row 1: SV picker + hue strip ──
            var row1 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };

            _svCanvas = BuildSvCanvas();
            row1.Children.Add(_svCanvas);

            row1.Children.Add(new Border { Width = 8 });

            _hueCanvas = BuildHueStrip();
            row1.Children.Add(_hueCanvas);

            root.Children.Add(row1);

            // ── Row 2: preview + hex ──
            var row2 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };

            _previewNew = new Border
            {
                Width           = 44,
                Height          = 30,
                CornerRadius    = new CornerRadius(4),
                BorderBrush     = Theme.BrushBorder,
                BorderThickness = new Thickness(1),
                Margin          = new Thickness(0, 0, 8, 0)
            };
            row2.Children.Add(_previewNew);

            row2.Children.Add(new TextBlock
            {
                Text              = "Hex",
                Foreground        = Theme.BrushTextSecond,
                FontFamily        = Theme.FontFamily,
                FontSize          = Theme.SizeBody,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(0, 0, 6, 0)
            });

            _hexBox = MakeTb(70);
            _hexBox.TextChanged += (_, _) => OnHexChanged();
            row2.Children.Add(_hexBox);

            root.Children.Add(row2);

            // ── Row 3: R G B inputs ──
            var row3 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14) };

            _tbR = MakeTb(44); _tbR.TextChanged += (_, _) => OnRgbChanged();
            _tbG = MakeTb(44); _tbG.TextChanged += (_, _) => OnRgbChanged();
            _tbB = MakeTb(44); _tbB.TextChanged += (_, _) => OnRgbChanged();

            row3.Children.Add(RgbLabel("R")); row3.Children.Add(_tbR);
            row3.Children.Add(new Border { Width = 8 });
            row3.Children.Add(RgbLabel("G")); row3.Children.Add(_tbG);
            row3.Children.Add(new Border { Width = 8 });
            row3.Children.Add(RgbLabel("B")); row3.Children.Add(_tbB);

            root.Children.Add(row3);

            // ── Row 4: buttons ──
            var btnOk = MakeBtn(Loc.Get("common.ok"), Theme.Accent);
            btnOk.Click += (_, _) => { ResultHex = ToHex(HsvToColor(_h, _s, _v)); DialogResult = true; Close(); };

            var btnCancel = MakeBtn(Loc.Get("common.cancel"), Theme.BgHover);
            btnCancel.Click += (_, _) => { DialogResult = false; Close(); };

            var btnRow = new StackPanel
            {
                Orientation         = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            btnRow.Children.Add(btnOk);
            btnRow.Children.Add(new Border { Width = 8 });
            btnRow.Children.Add(btnCancel);
            root.Children.Add(btnRow);

            return root;
        }

        private Canvas BuildSvCanvas()
        {
            var canvas = new Canvas
            {
                Width  = SvSize,
                Height = SvSize,
                Cursor = Cursors.Cross
            };

            // Checkerboard-style dark background
            canvas.Background = Theme.BrushBgCard;

            // White → hue gradient (horizontal)
            _svWhiteH = new Rectangle { Width = SvSize, Height = SvSize };
            Canvas.SetLeft(_svWhiteH, 0); Canvas.SetTop(_svWhiteH, 0);
            canvas.Children.Add(_svWhiteH);

            // Transparent → black gradient (vertical, overlay)
            _svDarkV = new Rectangle { Width = SvSize, Height = SvSize };
            _svDarkV.Fill = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Colors.Transparent, 0),
                    new GradientStop(Colors.Black, 1)
                },
                new Point(0, 0), new Point(0, 1));
            Canvas.SetLeft(_svDarkV, 0); Canvas.SetTop(_svDarkV, 0);
            canvas.Children.Add(_svDarkV);

            // Thumb
            _svThumb = new Ellipse
            {
                Width           = 12,
                Height          = 12,
                Stroke          = Brushes.White,
                StrokeThickness = 2,
                Fill            = Brushes.Transparent
            };
            Panel.SetZIndex(_svThumb, 10);
            canvas.Children.Add(_svThumb);

            canvas.MouseLeftButtonDown += SvCanvas_MouseDown;
            canvas.MouseMove           += SvCanvas_MouseMove;
            canvas.MouseLeftButtonUp   += SvCanvas_MouseUp;

            return canvas;
        }

        private Canvas BuildHueStrip()
        {
            var canvas = new Canvas { Width = HueW, Height = HueH, Cursor = Cursors.Hand };

            var hueRect = new Rectangle { Width = HueW, Height = HueH };
            hueRect.Fill = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Color.FromRgb(255, 0,   0),   0.000),
                    new GradientStop(Color.FromRgb(255, 255, 0),   0.167),
                    new GradientStop(Color.FromRgb(0,   255, 0),   0.333),
                    new GradientStop(Color.FromRgb(0,   255, 255), 0.500),
                    new GradientStop(Color.FromRgb(0,   0,   255), 0.667),
                    new GradientStop(Color.FromRgb(255, 0,   255), 0.833),
                    new GradientStop(Color.FromRgb(255, 0,   0),   1.000),
                },
                new Point(0, 0), new Point(0, 1));
            Canvas.SetLeft(hueRect, 0); Canvas.SetTop(hueRect, 0);
            canvas.Children.Add(hueRect);

            // Thumb line
            _hueLine = new Rectangle
            {
                Width           = HueW,
                Height          = 3,
                Fill            = Brushes.White,
                Stroke          = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                StrokeThickness = 1
            };
            Panel.SetZIndex(_hueLine, 10);
            canvas.Children.Add(_hueLine);

            canvas.MouseLeftButtonDown += HueCanvas_MouseDown;
            canvas.MouseMove           += HueCanvas_MouseMove;
            canvas.MouseLeftButtonUp   += HueCanvas_MouseUp;

            return canvas;
        }

        // ── Mouse handling: SV canvas ─────────────────────────────────────────

        private void SvCanvas_MouseDown(object s, MouseButtonEventArgs e)
        {
            ((Canvas)s).CaptureMouse();
            UpdateSvFromPoint(e.GetPosition((Canvas)s));
        }

        private void SvCanvas_MouseMove(object s, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                UpdateSvFromPoint(e.GetPosition((Canvas)s));
        }

        private void SvCanvas_MouseUp(object s, MouseButtonEventArgs e)
            => ((Canvas)s).ReleaseMouseCapture();

        private void UpdateSvFromPoint(Point p)
        {
            _s = Math.Clamp(p.X / SvSize, 0, 1);
            _v = Math.Clamp(1 - p.Y / SvSize, 0, 1);
            RefreshAll("sv");
        }

        // ── Mouse handling: hue strip ─────────────────────────────────────────

        private void HueCanvas_MouseDown(object s, MouseButtonEventArgs e)
        {
            ((Canvas)s).CaptureMouse();
            UpdateHueFromPoint(e.GetPosition((Canvas)s));
        }

        private void HueCanvas_MouseMove(object s, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                UpdateHueFromPoint(e.GetPosition((Canvas)s));
        }

        private void HueCanvas_MouseUp(object s, MouseButtonEventArgs e)
            => ((Canvas)s).ReleaseMouseCapture();

        private void UpdateHueFromPoint(Point p)
        {
            _h = Math.Clamp(p.Y / HueH, 0, 1) * 360;
            RefreshAll("hue");
        }

        // ── Text box handlers ─────────────────────────────────────────────────

        private void OnHexChanged()
        {
            if (_updating) return;
            var text = _hexBox.Text.TrimStart('#');
            if (text.Length != 6) return;
            try
            {
                var c = ParseHex("#" + text);
                ColorToHsv(c, out _h, out _s, out _v);
                RefreshAll("hex");
            }
            catch { }
        }

        private void OnRgbChanged()
        {
            if (_updating) return;
            if (!byte.TryParse(_tbR.Text, out byte r)) return;
            if (!byte.TryParse(_tbG.Text, out byte g)) return;
            if (!byte.TryParse(_tbB.Text, out byte b)) return;
            ColorToHsv(Color.FromRgb(r, g, b), out _h, out _s, out _v);
            RefreshAll("rgb");
        }

        // ── Refresh all controls ──────────────────────────────────────────────

        private void RefreshAll(string? skip)
        {
            if (_updating) return;
            _updating = true;
            try
            {
                var color = HsvToColor(_h, _s, _v);

                // SV square: update hue gradient
                _svWhiteH.Fill = new LinearGradientBrush(
                    Colors.White, HsvToColor(_h, 1, 1),
                    new Point(0, 0), new Point(1, 0));

                // SV thumb position
                double tx = _s * SvSize - 6;
                double ty = (1 - _v) * SvSize - 6;
                Canvas.SetLeft(_svThumb, tx);
                Canvas.SetTop(_svThumb,  ty);
                _svThumb.Stroke = _v < 0.4 ? Brushes.LightGray : Brushes.White;

                // Hue thumb position
                Canvas.SetTop(_hueLine, Math.Clamp(_h / 360 * HueH - 1.5, 0, HueH - 3));

                // Preview
                _previewNew.Background = new SolidColorBrush(color);

                // Text fields
                if (skip != "hex") _hexBox.Text = ToHex(color);
                if (skip != "rgb")
                {
                    _tbR.Text = color.R.ToString();
                    _tbG.Text = color.G.ToString();
                    _tbB.Text = color.B.ToString();
                }
            }
            finally { _updating = false; }
        }

        // ── Color conversion helpers ──────────────────────────────────────────

        private static Color HsvToColor(double h, double s, double v)
        {
            double r, g, b;
            if (s == 0) { r = g = b = v; }
            else
            {
                double sector = h / 60;
                int    i      = (int)sector;
                double f      = sector - i;
                double p      = v * (1 - s);
                double q      = v * (1 - s * f);
                double t      = v * (1 - s * (1 - f));
                (r, g, b) = i switch
                {
                    0 => (v, t, p),
                    1 => (q, v, p),
                    2 => (p, v, t),
                    3 => (p, q, v),
                    4 => (t, p, v),
                    _ => (v, p, q)
                };
            }
            return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        private static void ColorToHsv(Color c, out double h, out double s, out double v)
        {
            double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;
            v = max;
            s = max == 0 ? 0 : delta / max;
            if (delta == 0) { h = 0; return; }
            if (max == r)      h = 60 * (((g - b) / delta) % 6);
            else if (max == g) h = 60 * (((b - r) / delta) + 2);
            else               h = 60 * (((r - g) / delta) + 4);
            if (h < 0) h += 360;
        }

        private static Color ParseHex(string hex)
        {
            try { return (Color)ColorConverter.ConvertFromString(hex)!; }
            catch { return Color.FromRgb(33, 150, 243); }
        }

        private static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        // ── Small helpers ─────────────────────────────────────────────────────

        private static TextBox MakeTb(double width) => new TextBox
        {
            Width           = width,
            Background      = Theme.BrushBgCard,
            Foreground      = Theme.BrushTextPrimary,
            FontFamily      = Theme.FontFamily,
            FontSize        = Theme.SizeBody,
            BorderBrush     = Theme.BrushBorder,
            BorderThickness = new Thickness(1),
            Padding         = new Thickness(4, 2, 4, 2),
            Style           = (Style)Application.Current.Resources["DarkTextBox"]
        };

        private static TextBlock RgbLabel(string t) => new TextBlock
        {
            Text              = t,
            Foreground        = Theme.BrushTextSecond,
            FontFamily        = Theme.FontFamily,
            FontSize          = Theme.SizeBody,
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Thickness(0, 0, 4, 0)
        };

        private static Button MakeBtn(string text, Color bg) => new Button
        {
            Content         = text,
            Height          = 28,
            Padding         = new Thickness(16, 0, 16, 0),
            Background      = Theme.Brush(bg),
            Foreground      = Brushes.White,
            FontFamily      = Theme.FontFamily,
            FontSize        = Theme.SizeBody,
            BorderThickness = new Thickness(0),
            Style           = (Style)Application.Current.Resources["FlatButton"]
        };
    }
}

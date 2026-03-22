using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
                Text       = "Team Management",
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeH2,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.BrushTextPrimary
            });
            titles.Children.Add(new TextBlock
            {
                Text       = "Add and manage teams and their players",
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeBody,
                Foreground = Theme.BrushTextSecond,
                Margin     = new Thickness(0, 4, 0, 0)
            });
            Grid.SetColumn(titles, 0);
            row.Children.Add(titles);

            var btnAdd = MakeButton("+ Add Team", Theme.BrushAccentBlue);
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
                Text              = "Teams",
                FontFamily        = Theme.FontFamily,
                FontSize          = Theme.SizeH3,
                FontWeight        = FontWeights.Bold,
                Foreground        = Theme.BrushTextPrimary,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(12, 0, 0, 0)
            };
            DockPanel.SetDock(titleBar, Dock.Top);
            inner.Children.Add(titleBar);

            var btnDel = MakeButton("Delete Team", Theme.BrushDanger);
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
                Text              = "Players",
                FontFamily        = Theme.FontFamily,
                FontSize          = Theme.SizeH3,
                FontWeight        = FontWeights.Bold,
                Foreground        = Theme.BrushTextPrimary,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(12, 0, 0, 0)
            };
            DockPanel.SetDock(titleBar, Dock.Top);
            inner.Children.Add(titleBar);

            var btnEdit = MakeButton("Edit", Theme.BrushAccentBlue);
            btnEdit.Margin = new Thickness(8, 8, 0, 8);
            btnEdit.Click += (_, _) => EditPlayer();

            var btnDel = MakeButton("Delete", Theme.BrushDanger);
            btnDel.Margin = new Thickness(6, 8, 0, 8);
            btnDel.Click += (_, _) => DeletePlayer();

            var btnAdd = MakeButton("+ Add Player", Theme.BrushAccent);
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
            if (MessageBox.Show($"Delete team '{_selectedTeam.Name}'?\nAll players and match data will be removed.",
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _teamRepo.Delete(_selectedTeam.Id);
                _selectedTeam = null;
                _playerList.Items.Clear();
                LoadTeams();
            }
        }

        private void AddPlayer()
        {
            if (_selectedTeam == null) { MessageBox.Show("Select a team first."); return; }
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
            if (MessageBox.Show($"Delete player '{p.Name}'?", "Confirm",
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
            Title                   = existing == null ? "Add Team" : "Edit Team";
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
            AddRow(grid, "Team Name:", _name,  0);
            AddRow(grid, "Short Name:", _short, 1);

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
            var btnPickColor = DlgButton("Pick Color");
            btnPickColor.Click += (_, _) => PickColor();
            var colorRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            colorRow.Children.Add(btnPickColor);
            colorRow.Children.Add(_colorPreview);
            AddRow(grid, "Color:", colorRow, 2);

            // Buttons
            var ok = DlgButton("Save");
            ok.Background = Theme.BrushAccentBlue;
            ok.Foreground = Brushes.White;
            ok.Margin     = new Thickness(0, 16, 0, 0);
            ok.Click += (_, _) => Save();
            var cancel = DlgButton("Cancel");
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
            // WPF has no built-in ColorDialog; prompt for a hex value instead
            var dlg = new ColorInputDialog(_color) { Owner = this };
            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.HexColor))
            {
                _color = dlg.HexColor;
                _colorPreview.Background = new SolidColorBrush(HexColor(_color));
            }
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(_name.Text)) { MessageBox.Show("Name required."); return; }
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

    // ─── Simple hex color input dialog ────────────────────────────────────────

    internal class ColorInputDialog : Window
    {
        public string HexColor { get; private set; } = "";
        private TextBox _tb = null!;

        public ColorInputDialog(string current)
        {
            Title                 = "Pick Color";
            Width                 = 260;
            Height                = 130;
            Background            = Theme.BrushBgPanel;
            Foreground            = Theme.BrushTextPrimary;
            ResizeMode            = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var sp = new StackPanel { Margin = new Thickness(16) };
            sp.Children.Add(new TextBlock
            {
                Text       = "Enter hex color (e.g. #FF5500):",
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeBody,
                Foreground = Theme.BrushTextSecond,
                Margin     = new Thickness(0, 0, 0, 6)
            });
            _tb = new TextBox
            {
                Text            = current,
                Background      = Theme.BrushBgCard,
                Foreground      = Theme.BrushTextPrimary,
                FontFamily      = Theme.FontFamily,
                FontSize        = Theme.SizeBody,
                BorderBrush     = Theme.BrushBorder,
                BorderThickness = new Thickness(1),
                Padding         = new Thickness(4, 2, 4, 2),
                Margin          = new Thickness(0, 0, 0, 10)
            };
            sp.Children.Add(_tb);
            var ok = new Button
            {
                Content         = "OK",
                Height          = 28,
                Padding         = new Thickness(20, 0, 20, 0),
                Background      = Theme.BrushAccentBlue,
                Foreground      = Brushes.White,
                BorderThickness = new Thickness(0),
                FontFamily      = Theme.FontFamily,
                FontSize        = Theme.SizeBody,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ok.Click += (_, _) =>
            {
                HexColor     = _tb.Text.Trim();
                DialogResult = true;
                Close();
            };
            sp.Children.Add(ok);
            Content = sp;
        }
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
            Title                 = existing == null ? "Add Player" : "Edit Player";
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

            AddRow(grid, "Name:",     _name,     0);
            AddRow(grid, "Number:",   _number,   1);
            AddRow(grid, "Position:", _position, 2);

            var ok = DlgButton("Save");
            ok.Background = Theme.BrushAccentBlue;
            ok.Foreground = Brushes.White;
            ok.Margin     = new Thickness(0, 16, 0, 0);
            ok.Click     += (_, _) => Save();
            var cancel = DlgButton("Cancel");
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
            if (string.IsNullOrWhiteSpace(_name.Text)) { MessageBox.Show("Name required."); return; }
            if (!int.TryParse(_number.Text, out int num) || num < 1 || num > 99)
            { MessageBox.Show("Number must be 1–99."); return; }
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
}

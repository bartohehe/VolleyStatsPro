using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using VolleyStatsPro.Data;
using VolleyStatsPro.Helpers;
using VolleyStatsPro.Models;

namespace VolleyStatsPro.Views
{
    public class MatchesView : System.Windows.Controls.UserControl
    {
        private readonly MatchRepository _matchRepo    = new();
        private readonly TeamRepository  _teamRepo     = new();
        private readonly StatsService    _statsService = new();

        private ListView _matchList = null!;

        public event EventHandler<int>? OpenLiveMatch;

        public MatchesView()
        {
            Background = Theme.BrushBgDark;
            Content    = BuildUI();
            LoadMatches();
        }

        // ── UI construction ────────────────────────────────────────────────────

        private UIElement BuildUI()
        {
            var outer = new DockPanel { Background = Theme.BrushBgDark };

            // Header
            var header = BuildHeader();
            DockPanel.SetDock(header, Dock.Top);
            outer.Children.Add(header);

            // Bottom toolbar
            var toolbar = BuildToolbar();
            DockPanel.SetDock(toolbar, Dock.Bottom);
            outer.Children.Add(toolbar);

            // Match list (fills remaining)
            _matchList = BuildMatchList();
            outer.Children.Add(_matchList);

            return outer;
        }

        private UIElement BuildHeader()
        {
            var grid = new Grid { Height = 64, Background = Theme.BrushBgDark };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(20, 0, 0, 0) };
            left.Children.Add(new TextBlock
            {
                Text       = Loc.Get("matches.title"),
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeH2,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.BrushTextPrimary
            });
            left.Children.Add(new TextBlock
            {
                Text       = Loc.Get("matches.subtitle"),
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeBody,
                Foreground = Theme.BrushTextSecond,
                Margin     = new Thickness(0, 4, 0, 0)
            });
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            var btnNew = MakeButton(Loc.Get("matches.new"), Theme.Accent);
            btnNew.Margin = new Thickness(0, 0, 20, 0);
            btnNew.VerticalAlignment = VerticalAlignment.Center;
            btnNew.Click += (_, _) => NewMatch();
            Grid.SetColumn(btnNew, 1);
            grid.Children.Add(btnNew);

            return grid;
        }

        private UIElement BuildToolbar()
        {
            var sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Background  = Theme.BrushBgDark,
                Margin      = new Thickness(16, 4, 16, 8)
            };

            var btnOpen = MakeButton(Loc.Get("matches.openlive"), Theme.Accent);
            btnOpen.Margin = new Thickness(0, 0, 8, 0);
            btnOpen.Click += (_, _) => OpenSelectedMatch();
            sp.Children.Add(btnOpen);

            var btnDel = MakeButton(Loc.Get("common.delete"), Theme.Danger);
            btnDel.Click += (_, _) => DeleteMatch();
            sp.Children.Add(btnDel);

            return sp;
        }

        private ListView BuildMatchList()
        {
            var lv = new ListView
            {
                Background      = Theme.BrushBgCard,
                Foreground      = Theme.BrushTextPrimary,
                FontFamily      = Theme.FontFamily,
                FontSize        = Theme.SizeBody,
                BorderThickness = new Thickness(0),
                Margin          = new Thickness(16, 0, 16, 0),
                SelectionMode   = SelectionMode.Single
            };

            // Row coloring via DataTriggers
            var style = new Style(typeof(ListViewItem));
            style.Setters.Add(new Setter(ListViewItem.BackgroundProperty, Theme.BrushBgCard));
            style.Setters.Add(new Setter(ListViewItem.ForegroundProperty, Theme.BrushTextPrimary));
            var liveTrigger = new DataTrigger { Binding = new Binding("Status"), Value = "Live" };
            liveTrigger.Setters.Add(new Setter(ListViewItem.BackgroundProperty,
                new SolidColorBrush(Color.FromArgb(30, 0, 188, 140))));
            style.Triggers.Add(liveTrigger);
            var finishedTrigger = new DataTrigger { Binding = new Binding("Status"), Value = "Finished" };
            finishedTrigger.Setters.Add(new Setter(ListViewItem.ForegroundProperty, Theme.BrushTextSecond));
            style.Triggers.Add(finishedTrigger);
            lv.ItemContainerStyle = style;

            var gv = new GridView();
            gv.Columns.Add(new GridViewColumn { Header = Loc.Get("matches.col.date"),     Width = 100, DisplayMemberBinding = new Binding("DateStr") });
            gv.Columns.Add(new GridViewColumn { Header = Loc.Get("matches.col.home"),     Width = 160, DisplayMemberBinding = new Binding("Home") });
            gv.Columns.Add(new GridViewColumn { Header = Loc.Get("matches.col.score"),    Width = 80,  DisplayMemberBinding = new Binding("Score") });
            gv.Columns.Add(new GridViewColumn { Header = Loc.Get("matches.col.away"),     Width = 160, DisplayMemberBinding = new Binding("Away") });
            gv.Columns.Add(new GridViewColumn { Header = Loc.Get("matches.col.location"), Width = 140, DisplayMemberBinding = new Binding("Location") });
            gv.Columns.Add(new GridViewColumn { Header = Loc.Get("matches.col.status"),   Width = 90,  DisplayMemberBinding = new Binding("StatusDisplay") });
            gv.Columns.Add(BuildActionsColumn());
            lv.View = gv;

            lv.MouseDoubleClick += (_, _) => OpenSelectedMatch();

            return lv;
        }

        private GridViewColumn BuildActionsColumn()
        {
            // PDF button factory (blue)
            var pdfFactory = new FrameworkElementFactory(typeof(Button));
            pdfFactory.SetValue(Button.ContentProperty, "PDF");
            pdfFactory.SetValue(Button.WidthProperty, 40.0);
            pdfFactory.SetValue(Button.HeightProperty, 22.0);
            pdfFactory.SetValue(Button.MarginProperty, new Thickness(0, 0, 4, 0));
            pdfFactory.SetValue(Button.BackgroundProperty, Theme.BrushAccentBlue);
            pdfFactory.SetValue(Button.ForegroundProperty, Brushes.White);
            pdfFactory.SetValue(Button.FontFamilyProperty, Theme.FontFamily);
            pdfFactory.SetValue(Button.FontSizeProperty, Theme.SizeSmall);
            pdfFactory.SetValue(Button.PaddingProperty, new Thickness(0));
            pdfFactory.SetValue(Button.StyleProperty, (Style)System.Windows.Application.Current.Resources["FlatButton"]);
            pdfFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler((s, e) =>
            {
                if ((s as Button)?.DataContext is MatchRow row) ExportMatch(row.MatchId, "pdf");
                e.Handled = true;
            }));

            // CSV button factory (red)
            var csvFactory = new FrameworkElementFactory(typeof(Button));
            csvFactory.SetValue(Button.ContentProperty, "CSV");
            csvFactory.SetValue(Button.WidthProperty, 40.0);
            csvFactory.SetValue(Button.HeightProperty, 22.0);
            csvFactory.SetValue(Button.BackgroundProperty, Theme.BrushDanger);
            csvFactory.SetValue(Button.ForegroundProperty, Brushes.White);
            csvFactory.SetValue(Button.FontFamilyProperty, Theme.FontFamily);
            csvFactory.SetValue(Button.FontSizeProperty, Theme.SizeSmall);
            csvFactory.SetValue(Button.PaddingProperty, new Thickness(0));
            csvFactory.SetValue(Button.StyleProperty, (Style)System.Windows.Application.Current.Resources["FlatButton"]);
            csvFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler((s, e) =>
            {
                if ((s as Button)?.DataContext is MatchRow row) ExportMatch(row.MatchId, "csv");
                e.Handled = true;
            }));

            var spFactory = new FrameworkElementFactory(typeof(StackPanel));
            spFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            spFactory.SetValue(StackPanel.VerticalAlignmentProperty, VerticalAlignment.Center);
            spFactory.AppendChild(pdfFactory);
            spFactory.AppendChild(csvFactory);

            var template = new DataTemplate { VisualTree = spFactory };

            return new GridViewColumn { Header = "", Width = 96, CellTemplate = template };
        }

        // ── Data loading ───────────────────────────────────────────────────────

        public void LoadMatches()
        {
            _matchList.Items.Clear();
            foreach (var m in _matchRepo.GetAll())
            {
                _matchList.Items.Add(new MatchRow
                {
                    MatchId       = m.Id,
                    DateStr       = m.Date.ToString("yyyy-MM-dd"),
                    Home          = m.HomeTeamName,
                    Score         = $"{m.HomeScore} - {m.AwayScore}",
                    Away          = m.AwayTeamName,
                    Location      = m.Location,
                    Status        = m.Status,
                    StatusDisplay = m.Status switch
                    {
                        "Live"      => Loc.Get("status.live"),
                        "Finished"  => Loc.Get("status.finished"),
                        "Scheduled" => Loc.Get("status.scheduled"),
                        _           => m.Status
                    }
                });
            }
        }

        private void NewMatch()
        {
            var teams = _teamRepo.GetAll();
            if (teams.Count < 2)
            {
                System.Windows.MessageBox.Show(
                    Loc.Get("matches.noteams"),
                    Loc.Get("matches.noteams.title"),
                    MessageBoxButton.OK);
                return;
            }
            var dlg = new NewMatchDialog(teams);
            dlg.Owner = Window.GetWindow(this);
            if (dlg.ShowDialog() == true && dlg.Result != null)
            {
                _matchRepo.Insert(dlg.Result);
                LoadMatches();
            }
        }

        private void OpenSelectedMatch()
        {
            if (_matchList.SelectedItem is MatchRow row)
                OpenLiveMatch?.Invoke(this, row.MatchId);
        }

        private void ExportMatch(int matchId, string format)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog();
            if (format == "csv")
            {
                dlg.Filter   = "CSV files (*.csv)|*.csv";
                dlg.FileName = $"match_{matchId}.csv";
            }
            else
            {
                dlg.Filter   = "PDF files (*.pdf)|*.pdf";
                dlg.FileName = $"match_{matchId}.pdf";
            }

            if (dlg.ShowDialog() != true) return;

            var report = _statsService.GetMatchReport(matchId);
            if (format == "csv")
                StatsService.ExportMatchReportToCsv(report, dlg.FileName);
            else
                StatsService.ExportMatchReportToPdf(report, dlg.FileName);

            System.Windows.MessageBox.Show(Loc.Get("matches.exportdone"), Loc.Get("matches.exportdone"), MessageBoxButton.OK);
        }

        private void DeleteMatch()
        {
            if (_matchList.SelectedItem is not MatchRow row) return;
            var result = System.Windows.MessageBox.Show(
                Loc.Get("matches.deleteconfirm"),
                Loc.Get("common.confirm"),
                MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                _matchRepo.Delete(row.MatchId);
                LoadMatches();
            }
        }

        private static Button MakeButton(string text, Color bg, double width = 120, double height = 28)
        {
            var btn = new Button
            {
                Content    = text,
                Width      = width,
                Height     = height,
                Foreground = Brushes.White,
                Background = Theme.Brush(bg),
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeSmall,
                Padding    = new Thickness(0),
                Margin     = new Thickness(0)
            };
            btn.Style = (Style)System.Windows.Application.Current.Resources["FlatButton"];
            return btn;
        }

        // ── Row data class ─────────────────────────────────────────────────────

        private class MatchRow
        {
            public int    MatchId       { get; set; }
            public string DateStr       { get; set; } = "";
            public string Home          { get; set; } = "";
            public string Score         { get; set; } = "";
            public string Away          { get; set; } = "";
            public string Location      { get; set; } = "";
            public string Status        { get; set; } = ""; // raw DB value for DataTriggers
            public string StatusDisplay { get; set; } = ""; // translated for display
        }
    }

    // ── New Match Dialog ───────────────────────────────────────────────────────

    public class NewMatchDialog : Window
    {
        public Models.Match? Result { get; private set; }

        private ComboBox   _cbHome    = null!;
        private ComboBox   _cbAway    = null!;
        private DatePicker _date      = null!;
        private TextBox    _location  = null!;

        public NewMatchDialog(List<Team> teams)
        {
            Title                   = Loc.Get("newmatch.title");
            Width                   = 380;
            Height                  = 280;
            ResizeMode              = ResizeMode.NoResize;
            WindowStartupLocation   = WindowStartupLocation.CenterOwner;
            Background              = Theme.BrushBgPanel;

            Content = BuildUI(teams);
            Theme.ApplyDialogChrome(this, Loc.Get("newmatch.title"));
        }

        private UIElement BuildUI(List<Team> teams)
        {
            var root = new Grid { Margin = new Thickness(20) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Home Team
            AddRow(root, Loc.Get("newmatch.home"), 0);
            _cbHome = MakeComboBox();
            Grid.SetRow(_cbHome, 0); Grid.SetColumn(_cbHome, 1);
            root.Children.Add(_cbHome);

            // Away Team
            AddRow(root, Loc.Get("newmatch.away"), 1);
            _cbAway = MakeComboBox();
            Grid.SetRow(_cbAway, 1); Grid.SetColumn(_cbAway, 1);
            root.Children.Add(_cbAway);

            // Date
            AddRow(root, Loc.Get("newmatch.date"), 2);
            _date = new DatePicker
            {
                SelectedDate = DateTime.Today,
                FontFamily   = Theme.FontFamily,
                FontSize     = Theme.SizeBody,
                Margin       = new Thickness(0, 4, 0, 4)
            };
            Grid.SetRow(_date, 2); Grid.SetColumn(_date, 1);
            root.Children.Add(_date);

            // Location
            AddRow(root, Loc.Get("newmatch.location"), 3);
            _location = new TextBox
            {
                Background = Theme.BrushBgCard,
                Foreground = Theme.BrushTextPrimary,
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeBody,
                Margin     = new Thickness(0, 4, 0, 4),
                Padding    = new Thickness(4)
            };
            Grid.SetRow(_location, 3); Grid.SetColumn(_location, 1);
            root.Children.Add(_location);

            // Populate team combos
            foreach (var t in teams) { _cbHome.Items.Add(t); _cbAway.Items.Add(t); }
            if (teams.Count >= 1) _cbHome.SelectedIndex = 0;
            if (teams.Count >= 2) _cbAway.SelectedIndex = 1;

            // Buttons
            var btnRow = new StackPanel
            {
                Orientation         = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin              = new Thickness(0, 12, 0, 0)
            };
            Grid.SetRow(btnRow, 5); Grid.SetColumnSpan(btnRow, 2);
            root.Children.Add(btnRow);

            var btnCreate = MakeDialogButton(Loc.Get("newmatch.create"), Theme.Accent);
            btnCreate.Margin = new Thickness(0, 0, 8, 0);
            btnCreate.Click += (_, _) => Save();
            btnRow.Children.Add(btnCreate);

            var btnCancel = MakeDialogButton(Loc.Get("common.cancel"), Theme.BgHover);
            btnCancel.Click += (_, _) => { this.DialogResult = false; Close(); };
            btnRow.Children.Add(btnCancel);

            return root;
        }

        private static void AddRow(Grid grid, string labelText, int row)
        {
            var lbl = new TextBlock
            {
                Text              = labelText,
                FontFamily        = Theme.FontFamily,
                FontSize          = Theme.SizeBody,
                Foreground        = Theme.BrushTextSecond,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(0, 4, 8, 4)
            };
            Grid.SetRow(lbl, row);
            Grid.SetColumn(lbl, 0);
            grid.Children.Add(lbl);
        }

        private void Save()
        {
            if (_cbHome.SelectedItem is not Team home || _cbAway.SelectedItem is not Team away)
            {
                System.Windows.MessageBox.Show(Loc.Get("newmatch.val.teams"), Loc.Get("common.error"), MessageBoxButton.OK);
                return;
            }
            if (home.Id == away.Id)
            {
                System.Windows.MessageBox.Show(Loc.Get("newmatch.val.same"), Loc.Get("common.error"), MessageBoxButton.OK);
                return;
            }
            Result = new Models.Match
            {
                HomeTeamId = home.Id,
                AwayTeamId = away.Id,
                Date       = _date.SelectedDate ?? DateTime.Today,
                Location   = _location.Text.Trim(),
                Status     = "Scheduled"
            };
            this.DialogResult = true;
            Close();
        }

        private static ComboBox MakeComboBox() =>
            new ComboBox
            {
                Background = Theme.BrushBgCard,
                Foreground = Theme.BrushTextPrimary,
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeBody,
                Margin     = new Thickness(0, 4, 0, 4),
                Style      = (Style)Application.Current.Resources["DarkComboBox"]
            };

        private static Button MakeDialogButton(string text, Color bg) =>
            new Button
            {
                Content    = text,
                Width      = 90,
                Height     = 28,
                Background = Theme.Brush(bg),
                Foreground = Brushes.White,
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeSmall,
                Padding    = new Thickness(0)
            };
    }
}

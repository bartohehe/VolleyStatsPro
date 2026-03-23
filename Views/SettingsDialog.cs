using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using VolleyStatsPro.Data;
using VolleyStatsPro.Helpers;

namespace VolleyStatsPro.Views
{
    public class SettingsDialog : Window
    {
        private TextBox  _dirBox    = null!;
        private CheckBox _copyDb    = null!;
        private ComboBox _cbLang    = null!;

        public SettingsDialog()
        {
            Title                 = Loc.Get("settings.title");
            Width                 = 520;
            Height                = 360;
            ResizeMode            = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background            = Theme.BrushBgPanel;
            Content               = BuildUI();
            Theme.ApplyDialogChrome(this, Title);
        }

        private UIElement BuildUI()
        {
            var root = new Grid { Margin = new Thickness(24) };
            for (int i = 0; i < 8; i++)
                root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Title
            root.Children.Add(Row(0, new TextBlock
            {
                Text       = Loc.Get("settings.title"),
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeH2,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.BrushTextPrimary,
                Margin     = new Thickness(0, 0, 0, 4)
            }));

            // ── Language section ─────────────────────────────────────────────────
            root.Children.Add(Row(1, Label(Loc.Get("settings.language"), top: 16)));

            _cbLang = new ComboBox
            {
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeBody,
                Margin     = new Thickness(0, 4, 0, 0),
                Style      = (Style)Application.Current.Resources["DarkComboBox"]
            };
            string currentLang = SettingsManager.Current.Language;
            foreach (var (code, display) in Loc.Languages)
            {
                _cbLang.Items.Add(new ComboBoxItem
                {
                    Content = display,
                    Tag     = code,
                    Foreground = Theme.BrushTextPrimary,
                    FontFamily = Theme.FontFamily,
                    FontSize   = Theme.SizeBody
                });
                if (code == currentLang)
                    _cbLang.SelectedIndex = _cbLang.Items.Count - 1;
            }
            if (_cbLang.SelectedIndex < 0) _cbLang.SelectedIndex = 0;
            root.Children.Add(Row(2, _cbLang));

            // ── Database section ─────────────────────────────────────────────────
            root.Children.Add(Row(3, Label(Loc.Get("settings.datastorage"), top: 16, bold: true)));
            root.Children.Add(Row(4, Label(Loc.Get("settings.dblabel"))));

            // Path row
            var pathRow = new Grid();
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _dirBox = new TextBox
            {
                Text            = SettingsManager.Current.DatabaseDirectory,
                Background      = Theme.BrushBgCard,
                Foreground      = Theme.BrushTextPrimary,
                FontFamily      = Theme.FontFamily,
                FontSize        = Theme.SizeBody,
                Padding         = new Thickness(8, 5, 8, 5),
                BorderThickness = new Thickness(1),
                BorderBrush     = new SolidColorBrush(Theme.BorderColor),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_dirBox, 0);
            pathRow.Children.Add(_dirBox);

            var btnBrowse = FlatButton(Loc.Get("settings.browse"), Theme.BgHover, 90, Theme.BrushTextPrimary);
            btnBrowse.Margin = new Thickness(8, 0, 0, 0);
            btnBrowse.Click += (_, _) => BrowseFolder();
            Grid.SetColumn(btnBrowse, 1);
            pathRow.Children.Add(btnBrowse);

            root.Children.Add(Row(5, pathRow));

            _copyDb = new CheckBox
            {
                Content    = Loc.Get("settings.copydb"),
                IsChecked  = true,
                FontFamily = Theme.FontFamily,
                FontSize   = Theme.SizeBody,
                Foreground = Theme.BrushTextSecond,
                Margin     = new Thickness(0, 10, 0, 0)
            };
            root.Children.Add(Row(6, _copyDb));

            // ── Buttons ──────────────────────────────────────────────────────────
            var btnRow = new StackPanel
            {
                Orientation         = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin              = new Thickness(0, 8, 0, 0)
            };
            Grid.SetRow(btnRow, 9);
            root.Children.Add(btnRow);

            var btnReset = FlatButton(Loc.Get("settings.reset"), Theme.BgHover, 130, Theme.BrushTextSecond);
            btnReset.Margin = new Thickness(0, 0, 8, 0);
            btnReset.Click += (_, _) => _dirBox.Text = AppSettings.DefaultDirectory;
            btnRow.Children.Add(btnReset);

            var btnSave = FlatButton(Loc.Get("common.save"), Theme.Accent, 80, Brushes.White);
            btnSave.Margin = new Thickness(0, 0, 8, 0);
            btnSave.Click += (_, _) => Save();
            btnRow.Children.Add(btnSave);

            var btnCancel = FlatButton(Loc.Get("common.cancel"), Theme.BgHover, 80, Theme.BrushTextPrimary);
            btnCancel.Click += (_, _) => { DialogResult = false; Close(); };
            btnRow.Children.Add(btnCancel);

            return root;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static UIElement Row(int row, UIElement el)
        {
            Grid.SetRow(el, row);
            return el;
        }

        private TextBlock Label(string text, double top = 6, bool bold = false) => new TextBlock
        {
            Text       = text,
            FontFamily = Theme.FontFamily,
            FontSize   = Theme.SizeBody,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Foreground = bold ? Theme.BrushTextPrimary : Theme.BrushTextSecond,
            Margin     = new Thickness(0, top, 0, 4)
        };

        private Button FlatButton(string text, Color bg, double width, Brush fg) => new Button
        {
            Content    = text,
            Width      = width,
            Height     = 30,
            Background = Theme.Brush(bg),
            Foreground = fg,
            FontFamily = Theme.FontFamily,
            FontSize   = Theme.SizeBody,
            Padding    = new Thickness(0),
            Style      = (Style)Application.Current.Resources["FlatButton"]
        };

        // ── Actions ───────────────────────────────────────────────────────────────

        private void BrowseFolder()
        {
            var dlg = new OpenFolderDialog
            {
                Title            = Loc.Get("settings.dblabel"),
                InitialDirectory = _dirBox.Text
            };
            if (dlg.ShowDialog() == true)
                _dirBox.Text = dlg.FolderName;
        }

        private void Save()
        {
            bool langChanged = SaveLanguage();
            bool dbChanged   = SaveDatabase();

            if (!langChanged && !dbChanged)
            {
                DialogResult = false;
                Close();
                return;
            }

            if (langChanged)
            {
                var result = MessageBox.Show(
                    Loc.Get("settings.restart.msg"),
                    Loc.Get("settings.restart.title"),
                    MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                    RestartApp();
            }

            DialogResult = true;
            Close();
        }

        private bool SaveLanguage()
        {
            if (_cbLang.SelectedItem is not ComboBoxItem item) return false;
            string newLang = (string)item.Tag!;
            if (newLang == SettingsManager.Current.Language) return false;

            var updated = new AppSettings
            {
                DatabaseDirectory = SettingsManager.Current.DatabaseDirectory,
                Language          = newLang
            };
            SettingsManager.Save(updated);
            return true;
        }

        private bool SaveDatabase()
        {
            var newDir  = _dirBox.Text.Trim();
            if (string.IsNullOrEmpty(newDir)) { MessageBox.Show(Loc.Get("settings.val.folder")); return false; }

            var oldPath = SettingsManager.Current.DatabasePath;
            var newSettings = new AppSettings
            {
                DatabaseDirectory = newDir,
                Language          = SettingsManager.Current.Language
            };
            var newPath = newSettings.DatabasePath;

            if (string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase)) return false;

            try { Directory.CreateDirectory(newDir); }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Loc.Get("settings.val.nocreate"), ex.Message),
                    Loc.Get("common.error"), MessageBoxButton.OK);
                return false;
            }

            if (_copyDb.IsChecked == true && File.Exists(oldPath))
            {
                try { File.Copy(oldPath, newPath, overwrite: true); }
                catch (Exception ex)
                {
                    var r = MessageBox.Show(
                        string.Format(Loc.Get("settings.copy.failed"), ex.Message),
                        Loc.Get("settings.copy.title"), MessageBoxButton.YesNo);
                    if (r != MessageBoxResult.Yes) return false;
                }
            }

            SettingsManager.Save(newSettings);
            Database.Initialize();

            MessageBox.Show(
                string.Format(Loc.Get("settings.db.updated"), newPath),
                Loc.Get("settings.saved"), MessageBoxButton.OK);
            return true;
        }

        private static void RestartApp()
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exe))
                Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
            Application.Current.Shutdown();
        }
    }
}

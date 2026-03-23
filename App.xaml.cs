using System.Windows;

namespace VolleyStatsPro
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Helpers.Loc.Init(Helpers.SettingsManager.Current.Language);
            Data.Database.Initialize();
            base.OnStartup(e);
        }
    }
}

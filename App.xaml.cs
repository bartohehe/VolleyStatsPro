using System.Windows;

namespace VolleyStatsPro
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            VolleyStatsPro.Data.Database.Initialize();
            base.OnStartup(e);
        }
    }
}

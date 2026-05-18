using WorkTracker.App;
using WorkTracker.Data;

namespace WorkTracker;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        DatabaseInitializer.Initialize();
        Application.Run(new TrayApplicationContext());
    }
}
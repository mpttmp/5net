using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using AS_Chat.Core;
using AS_Chat.View.Window;

namespace AS_Chat;


public partial class App : Application
{
    public static IHost? AppHost { get; private set; }

    public App()
    {
        AppHost = Host.CreateDefaultBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<SessionContext>();

                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();

            }).Build();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        AppHost!.Start();

        AppHost!.Services.GetRequiredService<MainWindow>().Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await AppHost!.StopAsync();

        base.OnExit(e);
    }
}
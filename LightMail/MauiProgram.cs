using LightMail.Services;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace LightMail;
using MudBlazor.Services;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder.Services.AddLogging(logging =>
        {
            logging.AddDebug();
            logging.SetMinimumLevel(LogLevel.Trace);
        });

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        MudGlobal.UnhandledExceptionHandler = (exception) =>
        {
            Console.WriteLine($"MudBlazor Fehler aufgetreten: {exception.Message}");
            Console.WriteLine(exception.StackTrace);
        };

        builder.Services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
            config.SnackbarConfiguration.RequireInteraction = false;
            config.SnackbarConfiguration.PreventDuplicates = false;
            config.SnackbarConfiguration.NewestOnTop = false;
            config.SnackbarConfiguration.ShowCloseIcon = true;
            config.SnackbarConfiguration.VisibleStateDuration = 2000;
            config.SnackbarConfiguration.HideTransitionDuration = 500;
            config.SnackbarConfiguration.ShowTransitionDuration = 500;
            config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
        });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton<MailService>();
        builder.Services.AddSingleton<AccountStorageService>();
        builder.Services.AddSingleton<HtmlMailSanitizer>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine("=== UNHANDLED EXCEPTION (AppDomain) ===");
            System.Diagnostics.Debug.WriteLine(e.ExceptionObject);
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine("=== UNOBSERVED TASK EXCEPTION ===");
            System.Diagnostics.Debug.WriteLine(e.Exception);
            e.SetObserved();
        };

        return builder.Build();
    }
}
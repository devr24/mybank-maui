using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using MyBankApp.Config;
using System.Reflection;

namespace MyBankApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("MyBankApp.appsettings.json");

        var configurationBuilder = new ConfigurationBuilder()
            .AddJsonStream(stream);

        var configuration = configurationBuilder.Build();

        // Bind configuration
        var authConfig = new AuthConfig();
        configuration.GetSection("AzureAd").Bind(authConfig);

        // Register configuration
        builder.Services.AddSingleton(authConfig);

        // Register MSAL client
        builder.Services.AddSingleton<IPublicClientApplication>(sp =>
        {
            var config = sp.GetRequiredService<AuthConfig>();
            var pca = PublicClientApplicationBuilder
                .Create(config.ClientId)
                .WithAuthority(config.Authority)
                .WithRedirectUri($"msal{config.ClientId}://auth")
#if ANDROID
                .WithParentActivityOrWindow(() => Platform.CurrentActivity)
#endif               
                .Build();

            return pca;
        });

        // Register pages
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<ClaimsView>();

        return builder.Build();
    }
}
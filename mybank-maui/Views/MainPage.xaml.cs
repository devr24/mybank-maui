using Microsoft.Identity.Client;
using MyBankApp.Config;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using Microsoft.Maui.ApplicationModel;

namespace MyBankApp;

public partial class MainPage : ContentPage
{
    private readonly IPublicClientApplication _authClient;
    private readonly AuthConfig _authConfig;
    private readonly IFingerprint _fingerprint;

    public MainPage(IPublicClientApplication authClient, AuthConfig authConfig)
    {
        InitializeComponent();
        _authClient = authClient;
        _authConfig = authConfig;
        _fingerprint = CrossFingerprint.Current;
        this.Appearing += MainPage_Appearing;
    }

    private async void MainPage_Appearing(object? sender, EventArgs e)
    {
        await Dispatcher.DispatchAsync(() => LoadingOverlay.IsVisible = false);

        try
        {
            var accounts = await _authClient.GetAccountsAsync();
            if (accounts.Any())
            {
                try
                {
                    var result = await _authClient.AcquireTokenSilent(_authConfig.Scopes, accounts.FirstOrDefault()).ExecuteAsync();

                    if (result != null)
                    {
                        // Check if device supports biometric
                        var availability = await _fingerprint.GetAvailabilityAsync();
                        System.Diagnostics.Debug.WriteLine($"Biometric availability: {availability}");

                        if (availability == FingerprintAvailability.Available)
                        {
                            await Dispatcher.DispatchAsync(() => LoadingOverlay.IsVisible = true);

                            var authRequest = new AuthenticationRequestConfiguration(
                                "Verify identity",
                                "Please verify your identity to continue")
                            {
                                AllowAlternativeAuthentication = true,
                                ConfirmationRequired = true
                            };
                            await Dispatcher.DispatchAsync(async () =>
                            {
                                var biometricResult = await _fingerprint.AuthenticateAsync(authRequest);
                                System.Diagnostics.Debug.WriteLine($"Biometric result: {biometricResult.Status}");

                                if (biometricResult.Authenticated)
                                {
                                    //LoadingOverlay.IsVisible = false;

                                    await Shell.Current.GoToAsync("//ClaimsView");
                                }
                                else
                                {
                                    //LoadingOverlay.IsVisible = false;

                                    // Authentication failed or was canceled
                                    System.Diagnostics.Debug.WriteLine($"Authentication failed: {biometricResult.Status}");
                                }
                            });
                        }
                        else
                        {
                            // If biometric is not available, show why
                            await Dispatcher.DispatchAsync(async () =>
                            {
                                await DisplayAlert("Notice", $"Biometric authentication is not available: {availability}", "OK");
                            });
                            await Shell.Current.GoToAsync("//ClaimsView");
                        }
                    }
                }
                catch (MsalUiRequiredException)
                {
                    System.Diagnostics.Debug.WriteLine("Token expired or needs user interaction");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking login state: {ex.Message}");
        }
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        try
        {
            var accounts = await _authClient.GetAccountsAsync();
            AuthenticationResult result;

            await Dispatcher.DispatchAsync(() => LoadingOverlay.IsVisible = true);
            try
            {
                if (accounts.Any())
                {
                    System.Diagnostics.Debug.WriteLine("Found existing account, attempting silent login");
                    result = await _authClient.AcquireTokenSilent(_authConfig.Scopes, accounts.FirstOrDefault())
                        .ExecuteAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No existing account, starting interactive login");
                    result = await _authClient.AcquireTokenInteractive(_authConfig.Scopes).WithPrompt(Prompt.SelectAccount).ExecuteAsync();
                }

                System.Diagnostics.Debug.WriteLine($"Authentication successful. Access token: {result?.AccessToken.Substring(0, 10)}...");

                if (result != null)
                {
                    await Shell.Current.GoToAsync("//ClaimsView");
                }
            }
            catch (MsalUiRequiredException ex)
            {
                await Dispatcher.DispatchAsync(() => LoadingOverlay.IsVisible = false);
                System.Diagnostics.Debug.WriteLine($"MSAL UI Required Exception: {ex.Message}");
                try
                {
                    result = await _authClient.AcquireTokenInteractive(_authConfig.Scopes)
                        .ExecuteAsync();

                    if (result != null)
                    {
                        await Shell.Current.GoToAsync("//ClaimsView");
                    }
                }
                catch (Exception innerEx)
                {
                    await Dispatcher.DispatchAsync(() => LoadingOverlay.IsVisible = false);
                    System.Diagnostics.Debug.WriteLine($"Interactive login failed: {innerEx.Message}");
                    await DisplayAlert("Login Error", $"Interactive login failed: {innerEx.Message}", "OK");
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.DispatchAsync(() => LoadingOverlay.IsVisible = false);
                System.Diagnostics.Debug.WriteLine($"Authentication error: {ex.Message}");
                await DisplayAlert("Login Error", $"Authentication failed: {ex.Message}", "OK");
            }
        }
        catch (Exception ex)
        {
            await Dispatcher.DispatchAsync(() => LoadingOverlay.IsVisible = false);
            System.Diagnostics.Debug.WriteLine($"Outer error: {ex.Message}");
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
    }
}
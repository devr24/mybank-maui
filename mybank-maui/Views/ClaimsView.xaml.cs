using Microsoft.Identity.Client;
using Microsoft.Maui.ApplicationModel;
using System.Collections.ObjectModel;
using System.IdentityModel.Tokens.Jwt;

namespace MyBankApp;

public partial class ClaimsView : ContentPage
{
    private readonly IPublicClientApplication _authClient;
    public string UserDisplayName { get; set; } = "";
    public ObservableCollection<string> IdTokenClaims { get; set; } = new();

    public ClaimsView(IPublicClientApplication authClient)
    {
        InitializeComponent();
        _authClient = authClient;
        this.Appearing += ClaimsView_Appearing;
        this.BindingContext = this;
    }

    private async void ClaimsView_Appearing(object sender, EventArgs e)
    {
        await LoadUserClaimsAsync();
    }

    private async Task LoadUserClaimsAsync()
    {
        try
        {
            var accounts = await _authClient.GetAccountsAsync();
            if (accounts.Any())
            {
                var account = accounts.First();
                var result = await _authClient.AcquireTokenSilent(new[] { "openid", "profile" }, account).ExecuteAsync();

                if (result != null)
                {
                    var handler = new JwtSecurityTokenHandler();
                    var token = handler.ReadJwtToken(result.IdToken);

                    IdTokenClaims.Clear();

                    foreach (var claim in token.Claims)
                    {
                        IdTokenClaims.Add($"{claim.Type}: {claim.Value}");
                    }

                    UserDisplayName = token.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? "User";
                    OnPropertyChanged(nameof(UserDisplayName));
                }
            }
        }
        catch (Exception ex)
        {
            await Dispatcher.DispatchAsync(async () =>
            {
                await DisplayAlert("Error", $"Failed to load claims: {ex.Message}", "OK");
            });
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        try
        {
            var accounts = await _authClient.GetAccountsAsync();
            if (accounts.Any())
            {
                // Get the current account
                var account = accounts.First();

                // After the user selects "Sign out", remove the account and go back to main page
                await _authClient.RemoveAsync(account);
                await Shell.Current.GoToAsync("//MainPage");
            }
        }
        catch (MsalException ex)
        {
            // User likely clicked "No" in the account selection screen, just go back to main
            await _authClient.RemoveAsync(null); // Clear everything
            await Shell.Current.GoToAsync("//MainPage");
        }
        catch (Exception ex)
        {
            await Dispatcher.DispatchAsync(async () =>
            {
                await DisplayAlert("Logout Error", ex.Message, "OK");
            });
        }
    }
}
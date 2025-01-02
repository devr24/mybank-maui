namespace MyBankApp.Config;

public class AuthConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Authority { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = Array.Empty<string>();
}
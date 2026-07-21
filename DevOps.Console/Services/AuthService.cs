using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace DevOps.Services;

public static class AuthService
{
    // Well-known public client ID of the Azure CLI. It is a first-party public
    // client that is pre-consented for Azure DevOps and allows loopback redirects,
    // so no app registration is required.
    private const string ClientId = "04b07795-8ddb-461a-bbee-02f9e1bf7b46";

    // Azure DevOps resource ID. The /.default scope requests all statically
    // configured permissions for the resource.
    private static readonly string[] Scopes = ["499b84ac-1321-427f-aa17-267ca6975798/.default"];

    private const string CacheFileName = "msal_cache.bin";
    private const string CacheKeyChainService = "DevOps.Console";
    private const string CacheKeyChainAccount = "MSALCache";

    private static IPublicClientApplication _app;

    private static async Task<IPublicClientApplication> GetAppAsync(string tenantId)
    {
        if (_app != null)
            return _app;

        var authorityTenant = string.IsNullOrEmpty(tenantId) ? "organizations" : tenantId;

        var app = PublicClientApplicationBuilder
            .Create(ClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, authorityTenant)
            .WithRedirectUri("http://localhost")
            .Build();

        var cacheDir = ConfigService.GetConfigDirectory();
        Directory.CreateDirectory(cacheDir);

        var storage = new StorageCreationPropertiesBuilder(CacheFileName, cacheDir)
            .WithLinuxUnprotectedFile()
            .WithMacKeyChain(CacheKeyChainService, CacheKeyChainAccount)
            .Build();

        var cacheHelper = await MsalCacheHelper.CreateAsync(storage);
        cacheHelper.RegisterCache(app.UserTokenCache);

        _app = app;
        return _app;
    }

    /// <summary>
    /// Acquires an access token from the local cache without prompting. Throws a
    /// friendly error when no cached account is available or interaction is required.
    /// </summary>
    public static async Task<string> GetAccessTokenAsync(string tenantId, CancellationToken ct = default)
    {
        var app = await GetAppAsync(tenantId);
        var account = (await app.GetAccountsAsync()).FirstOrDefault();

        if (account == null)
            throw new InvalidOperationException("Not signed in. Run 'devops config --login' to authenticate with Microsoft Entra ID.");

        try
        {
            var result = await app.AcquireTokenSilent(Scopes, account).ExecuteAsync(ct);
            return result.AccessToken;
        }
        catch (MsalUiRequiredException)
        {
            throw new InvalidOperationException("Your session has expired. Run 'devops config --login' to sign in again.");
        }
    }

    /// <summary>
    /// Performs an interactive browser sign-in and returns the authentication result.
    /// </summary>
    public static async Task<AuthenticationResult> SignInAsync(string tenantId, CancellationToken ct = default)
    {
        var app = await GetAppAsync(tenantId);
        var account = (await app.GetAccountsAsync()).FirstOrDefault();

        if (account != null)
        {
            try
            {
                return await app.AcquireTokenSilent(Scopes, account).ExecuteAsync(ct);
            }
            catch (MsalUiRequiredException)
            {
                // Fall through to interactive sign-in.
            }
        }

        return await app.AcquireTokenInteractive(Scopes)
            .WithUseEmbeddedWebView(false)
            .ExecuteAsync(ct);
    }

    /// <summary>
    /// Removes all cached accounts and tokens.
    /// </summary>
    public static async Task SignOutAsync(string tenantId, CancellationToken ct = default)
    {
        var app = await GetAppAsync(tenantId);
        foreach (var account in await app.GetAccountsAsync())
            await app.RemoveAsync(account);
    }

    /// <summary>
    /// Returns the username (UPN/email) of the cached account, or null if not signed in.
    /// </summary>
    public static async Task<string> GetSignedInUsernameAsync(string tenantId)
    {
        var app = await GetAppAsync(tenantId);
        var account = (await app.GetAccountsAsync()).FirstOrDefault();
        return account?.Username;
    }
}

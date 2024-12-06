namespace Flammoth.Rcl.Pages;

// ReSharper disable once ClassNeverInstantiated.Global
// ReSharper disable once UnusedType.Global
public partial class Index
{
    private const string AppName = "Flammoth";
    private const string InstanceKey = "instance";
    private const string AccessTokenKey = "accessToken";

    private LoginModel LoginModel { get; } = new() { Instance = "mastodon.social" };

    private MastodonList<Status>? HomeTimeline { get; set; }

    private AuthenticationClient? authClient;
    private Auth? auth;
    private MastodonClient? client;

    private bool isLoading;

    protected override async Task OnInitializedAsync()
    {
        isLoading = true;

        var instance = await LocalStorageService.GetItemAsStringAsync(InstanceKey);
        var accessToken = await LocalStorageService.GetItemAsStringAsync(AccessTokenKey);

        if (instance is not { Length: > 0 } ||
            accessToken is not { Length: > 0 })
        {
            isLoading = false;
            return;
        }

        client = new(instance, accessToken, HttpClient);
        await DoStuffWithClient();

        isLoading = false;
    }

    private async Task LogOut()
    {
        await LocalStorageService.RemoveItemAsync(InstanceKey);
        await LocalStorageService.RemoveItemAsync(AccessTokenKey);
        LoginModel.AuthCode = null;
        HomeTimeline = null;
        authClient = null;
        auth = null;
        client = null;
    }

    private async Task LogInToMastodon()
    {
        if (LoginModel.Instance is not { Length: > 0 } instance)
        {
            return;
        }

        await LocalStorageService.SetItemAsStringAsync(InstanceKey, instance);

        try
        {
            authClient ??= new(instance, HttpClient);
            await authClient.CreateApp(AppName, scope: GranularScope.Read);
            var url = authClient.OAuthUrl();

            await JsRuntime.InvokeAsync<object>("open", url, "_blank");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task AuthorizeWithAuthCode()
    {
        if (authClient is null ||
            LoginModel.Instance is not { Length: > 0 } instance ||
            LoginModel.AuthCode is not { Length: > 0 } authCode)
        {
            throw new("Failed to authenticate");
        }

        isLoading = true;

        try
        {
            auth = await authClient.ConnectWithCode(authCode);
            if (auth is null)
            {
                throw new("Failed to authenticate");
            }
            isLoading = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            isLoading = false;
            throw;
        }

        await LocalStorageService.SetItemAsStringAsync(AccessTokenKey, auth.AccessToken);
        client = new(instance, auth.AccessToken, HttpClient);

        await DoStuffWithClient();
    }

    private async Task DoStuffWithClient()
    {
        if (client is null)
        {
            return;
        }
        isLoading = true;

        try
        {
            HomeTimeline = await client.GetPublicTimeline(options: new() { Limit = 10 });
        }
        catch (ServerErrorException mastEx)
        {
            Console.WriteLine(mastEx);
            if (mastEx.Message.Equals("The access token was revoked"))
            {
                await LogOut();
            }
            isLoading = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            isLoading = false;
            throw;
        }

        isLoading = false;
    }
}

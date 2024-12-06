namespace Flammoth.Rcl.Pages;

// ReSharper disable once ClassNeverInstantiated.Global
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

    private bool IsLoading;

    protected override async Task OnInitializedAsync()
    {
        IsLoading = true;

        var instance = await LocalStorageService.GetItemAsStringAsync(InstanceKey);
        var accessToken = await LocalStorageService.GetItemAsStringAsync(AccessTokenKey);

        if (instance is not { Length: > 0 } ||
            accessToken is not { Length: > 0 })
        {
            IsLoading = false;
            return;
        }

        client = new(instance, accessToken, HttpClient);
        await DoStuffWithClient();

        IsLoading = false;
    }

    private async Task LogOut()
    {
        await LocalStorageService.RemoveItemAsync(InstanceKey);
        await LocalStorageService.RemoveItemAsync(AccessTokenKey);
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

        IsLoading = true;

        try
        {
            auth = await authClient.ConnectWithCode(authCode);
            if (auth is null)
            {
                throw new("Failed to authenticate");
            }
            IsLoading = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            IsLoading = false;
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
        IsLoading = true;
        HomeTimeline = await client.GetPublicTimeline(options: new() { Limit = 10 });
        IsLoading = false;
    }
}

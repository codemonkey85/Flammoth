namespace Flammoth.Rcl.Pages;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class Index
{
    private const string PageTitle = "Flammoth";
    private const string AuthCodeKey = "authCode";

    private LoginModel LoginModel { get; } = new() { Instance = @"mastodon.social" };

    private AuthModel AuthModel { get; } = new();

    private MastodonList<Status>? HomeTimeline { get; set; }

    private AuthenticationClient? authClient;
    private AppRegistration? appRegistration;
    private Auth? auth;
    private MastodonClient? client;

    protected override async Task OnInitializedAsync()
    {
        var authToken = await LocalStorageService.GetItemAsStringAsync(AuthCodeKey);
        if (authToken is { Length: > 0 })
        {
            AuthModel.AuthCode = authToken;
            await AuthorizeAsync();
        }
    }

    private async Task AuthenticateAsync()
    {
        if (LoginModel is not { Instance.Length: > 0 })
        {
            return;
        }

        authClient = new AuthenticationClient(LoginModel.Instance, HttpClient);
        appRegistration = await authClient.CreateApp("Flammoth", Scope.Read);
        var url = authClient.OAuthUrl();
        try
        {
            await JsRuntime.InvokeAsync<object>("open", url, "_blank");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task AuthorizeViaFormAsync()
    {
        if (appRegistration is null)
        {
            return;
        }

        await AuthorizeAsync();
    }

    private async Task AuthorizeAsync()
    {
        if (LoginModel is not { Instance.Length: > 0 } || authClient is null || AuthModel is not { AuthCode.Length: > 0 })
        {
            return;
        }

        try
        {
            auth = await authClient.ConnectWithCode(AuthModel.AuthCode);
            if (auth is null)
            {
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return;
        }

        client = new MastodonClient(LoginModel.Instance, auth.AccessToken, HttpClient);

        await DoStuffWithClient();
    }

    private async Task DoStuffWithClient()
    {
        if (client is null)
        {
            return;
        }
        HomeTimeline = await client.GetPublicTimeline(options: new ArrayOptions { Limit = 10 });
    }
}

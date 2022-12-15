namespace Flammoth.Rcl.Pages;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class Index
{
    private const string PageTitle = "Flammoth";

    private LoginModel LoginModel { get; } = new() { Instance = @"mastodon.social" };

    private AuthModel AuthModel { get; } = new();
    private AuthenticationClient? authClient;
    private AppRegistration? appRegistration;
    private Auth? auth;
    private MastodonClient? client;

    private async Task AuthenticateAsync()
    {
        if (LoginModel is not { Instance.Length: > 0 })
        {
            return;
        }

        authClient = new AuthenticationClient(LoginModel.Instance, HttpClient);
        appRegistration = await authClient.CreateApp("Flammoth", Scope.Read | Scope.Write | Scope.Follow);
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

    private async Task AuthorizeAsync()
    {
        if (authClient is null || appRegistration is null || AuthModel is not { AuthCode.Length: > 0 })
        {
            return;
        }

        auth = await authClient.ConnectWithCode(AuthModel.AuthCode);
        if (auth is null)
        {
            return;
        }

        client = new MastodonClient("instance", auth.AccessToken, HttpClient);
    }
}
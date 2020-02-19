using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Linq;
using IdentityModel.Client;
using System.Threading.Tasks;

namespace XamarinFormsOidcClient.Core.Services
{
    public class StsService : IStsService
    {
        private readonly IBrowser _browser;
        private IDictionary<string, string> _stsList;
        private OidcClient _client;
        private HttpClientHandler _handler;
        
        private string _authority;
        private string _apiBaseAddress;
        private string _api;

        private LoginResult _loginResult;

        public StsService(IBrowser browser)
        {
            _browser = browser;
            _stsList = new Dictionary<string, string>
                        {
                            { "Local STS (localhost:5000)", "https://10.0.2.2:5000,https://10.0.2.2:5001/,identity" },
                            { "demo.identityserver.io", "https://demo.identityserver.io,https://demo.identityserver.io/,api/test" }
                        };

#if DEBUG
            // dev env setup - handle/bypass certificate errors
            _handler = _handler ?? new CustomHttphandler();
#endif

            var sts = _stsList.Values.FirstOrDefault();
            var values = (!String.IsNullOrEmpty(sts) ? sts : "").Split(',');

            _authority = values[0];
            _apiBaseAddress = values[1];
            _api = values[2];
            _handler = _handler ?? new HttpClientHandler();

            _client = GetClient(values[0], _browser, _handler);
        }

        private OidcClient GetClient(string authority, IBrowser browser, HttpClientHandler handler)
        {
            var options = new OidcClientOptions
            {
                Authority = authority,
                ClientId = "native.hybrid",
                ClientSecret = "secret",
                Scope = "openid profile email api offline_access",
                RedirectUri = "xamarinformsclients://callback",
                Browser = _browser,

                ResponseMode = OidcClientOptions.AuthorizeResponseMode.Redirect,
                Flow = OidcClientOptions.AuthenticationFlow.Hybrid,
                PostLogoutRedirectUri = "xamarinformsclients://callback",
                BackchannelHandler = handler
            };

            return new OidcClient(options);
        }

        public IDictionary<string, string> GetStsList()
        {
            return _stsList;
        }

        public async Task<DiscoveryDocumentResponse> GetDiscoAsync()
        {
            var client = new HttpClient(_handler);
            var disco = await client.GetDiscoveryDocumentAsync(
                new DiscoveryDocumentRequest
                {
                    Address = _authority
#if DEBUG
                    ,Policy = new DiscoveryPolicy
                    {
                        RequireHttps = false
                    }
#endif
                });
            return disco;
        }

        public async Task<string> GetOpenIdConfigurationAsync()
        {
            var client = new HttpClient(_handler);
            client.BaseAddress = new Uri(_authority);
            var config = await client.GetAsync(".well-known/openid-configuration");
            var result = await config.Content.ReadAsStringAsync();

            return result;
        }

        public async Task<LoginResult> Login()
        {
            if (_client == null)
                throw new InvalidOperationException("_client is null");

            _loginResult = await _client.LoginAsync();

            return _loginResult;
        }

        public async Task<LogoutResult> Logout()
        {
            if (_client == null || _loginResult == null || !_loginResult.User.Identity.IsAuthenticated)
                return null;

            var logoutResult = await _client.LogoutAsync(new LogoutRequest { IdTokenHint = _loginResult.IdentityToken });

            return logoutResult;
        }

        public async Task<string> CallApiAsync(string api)
        {
            var client = new HttpClient(_handler);
            client.BaseAddress = new Uri(_apiBaseAddress);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _loginResult?.AccessToken ?? "");
            var apiResult = await client.GetAsync(api);

            if (!apiResult.IsSuccessStatusCode)
            {
                return apiResult.ReasonPhrase;
            }
            else
            {
                return await apiResult.Content.ReadAsStringAsync();
            }
        }
    }

    public interface IStsService
    {
        Task<LoginResult> Login();
        Task<LogoutResult> Logout();
        Task<string> CallApiAsync(string api);
        Task<DiscoveryDocumentResponse> GetDiscoAsync();
        Task<string> GetOpenIdConfigurationAsync();
        IDictionary<string, string> GetStsList();
    }

#if DEBUG
    // dev env setup - handle/bypass certificate errors
    internal class CustomHttphandler : HttpClientHandler
    {
        public CustomHttphandler() : base()
        {
            ServerCertificateCustomValidationCallback = (message, certificate, chain, sslPolicyErrors) =>
            {
                return true;
            };
        }
    }
#endif
}

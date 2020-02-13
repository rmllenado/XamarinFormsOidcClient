using IdentityModel.Client;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XamarinFormsOidcClient.Core
{
    //[XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage
    {
        OidcClient _client;
        private static LoginResult _result;

        private static Lazy<HttpClient> _apiClient = null;

        private string _authority;
        private string _api;
        private HttpClientHandler _handler = null;

        public MainPage()
        {
            InitializeComponent();

            _handler = _handler ?? new System.Net.Http.HttpClientHandler();
            _apiClient = _apiClient ?? new Lazy<HttpClient>(() => new HttpClient(_handler));

            var useLocalIdentityServer = false;
            var useSecureLocal = true;

            if (useLocalIdentityServer)
            {
                if (useSecureLocal)
                {
                    _authority = "https://10.0.2.2:5000";
                    _api = "https://10.0.2.2:5001/";
                }
                else
                {
                    _authority = "http://10.0.2.2:5000";
                    _api = "http://10.0.2.2:5001/";
                }
            }
            else
            {
                _authority = "https://demo.identityserver.io";
                _api = "https://demo.identityserver.io/";
            }

            CallApiWeatherforecast.Clicked += CallApiWeatherforecast_Clicked;
            OpenIDConfiguration.Clicked += OpenIDConfiguration_Clicked;
            AuthorizeCallApi.Clicked += AuthorizeCallApi_Clicked;
            Login.Clicked += Login_Clicked;
            CallApi.Clicked += CallApi_Clicked;

            var browser = DependencyService.Get<IBrowser>();

            //var options = new OidcClientOptions
            //{
            //    Authority = "https://demo.identityserver.io",
            //    ClientId = "native.hybrid",
            //    Scope = "openid profile email api offline_access",
            //    RedirectUri = "xamarinformsclients://callback",
            //    Browser = browser,

            //    ResponseMode = OidcClientOptions.AuthorizeResponseMode.Redirect
            //};

            var options = new OidcClientOptions
            {
                Authority = _authority,
                ClientId = "native.hybrid",
                ClientSecret = "secret",
                //Scope = "openid profile email web_api offline_access",
                Scope = "openid profile email api offline_access",
                RedirectUri = "xamarinformsclients://callback",
                Browser = browser,

                ResponseMode = OidcClientOptions.AuthorizeResponseMode.Redirect,

                Flow = OidcClientOptions.AuthenticationFlow.Hybrid
            };

            _client = new OidcClient(options);
        }

        private async void OpenIDConfiguration_Clicked(object sender, EventArgs e)
        {
            //// Cannot use http in _authority
            //var client = new HttpClient();
            //var disco = await GetDisco(client, _authority);
            //if (disco.IsError)
            //{
            //    OutputText.Text = disco.Error;
            //}
            //else
            //{
            //    OutputText.Text = Newtonsoft.Json.JsonConvert.SerializeObject(disco);
            //    //OutputText.Text = JArray.FromObject(disco).ToString();
            //}

            var client = new HttpClient(_handler);

            client.BaseAddress = new Uri(_authority);
            try
            {
                HttpResponseMessage result = null;
                result = await client.GetAsync(".well-known/openid-configuration");
                //OutputText.Text = JArray.Parse(await result.Content.ReadAsStringAsync()).ToString();
                OutputText.Text = await result.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                OutputText.Text = Newtonsoft.Json.JsonConvert.SerializeObject(ex);
                return;
            }

            return;
        }

        private async Task<DiscoveryDocumentResponse> GetDisco(HttpClient client, string url)
        {
            var disco = await client.GetDiscoveryDocumentAsync(url);
            return disco;
        }

        private async void AuthorizeCallApi_Clicked(object sender, EventArgs e)
        {
            var client = new HttpClient(_handler);
            var disco = await GetDisco(client, _authority);
            if (disco.IsError)
            {
                OutputText.Text = disco.Error;
                return;
            }

            var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = "client",
                ClientSecret = "secret",

                Scope = "web_api"
            });

            if (tokenResponse.IsError)
            {
                OutputText.Text = tokenResponse.Error;
            }
            else
            {
                OutputText.Text = tokenResponse.Json.ToString();
            }

            return;
        }

        private async void Login_Clicked(object sender, EventArgs e)
        {
            try
            {
                _result = await _client.LoginAsync(new LoginRequest());
            }
            catch (Exception ex)
            {
                //OutputText.Text = JArray.FromObject(ex).ToString();
                OutputText.Text = Newtonsoft.Json.JsonConvert.SerializeObject(ex);
                return;
            }

            if (_result.IsError)
            {
                OutputText.Text = _result.Error;
                return;
            }

            var sb = new StringBuilder(128);
            foreach (var claim in _result.User.Claims)
            {
                sb.AppendFormat("{0}: {1}\n", claim.Type, claim.Value);
            }

            sb.AppendFormat("\n{0}: {1}\n", "refresh token", _result?.RefreshToken ?? "none");
            sb.AppendFormat("\n{0}: {1}\n", "access token", _result.AccessToken);

            OutputText.Text = sb.ToString();

            _apiClient.Value.SetBearerToken(_result?.AccessToken ?? "");
            _apiClient.Value.BaseAddress = new Uri(_api);

        }

        private async void CallApi_Clicked(object sender, EventArgs e)
        {
            HttpResponseMessage result = null;
            try
            {
                result = await _apiClient.Value.GetAsync("api/test");
                //result = await _apiClient.Value.GetAsync("identity");
            }
            catch (Exception ex)
            {
                //OutputText.Text = JArray.FromObject(ex).ToString();
                OutputText.Text = Newtonsoft.Json.JsonConvert.SerializeObject(ex);
            }

            if (result != null && result.IsSuccessStatusCode)
            {
                OutputText.Text = JArray.Parse(await result.Content.ReadAsStringAsync()).ToString();
            }
            else
            {
                OutputText.Text = result?.ReasonPhrase;
            }
        }

        private async void CallApiWeatherforecast_Clicked(object sender, EventArgs e)
        {
            HttpResponseMessage result = null;
            try
            {
                var apiClient = new HttpClient(_handler);
                //apiClient.Value.SetBearerToken(_result?.AccessToken ?? "");
                apiClient.BaseAddress = new Uri(_api);
                result = await apiClient.GetAsync("weatherforecast");
            }
            catch (Exception ex)
            {
                //OutputText.Text = JArray.FromObject(ex).ToString();
                OutputText.Text = Newtonsoft.Json.JsonConvert.SerializeObject(ex);
            }

            if (result != null && result.IsSuccessStatusCode)
            {
                OutputText.Text = JArray.Parse(await result.Content.ReadAsStringAsync()).ToString();
            }
            else
            {
                OutputText.Text = result?.ReasonPhrase;
            }
        }
    }
}
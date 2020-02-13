using IdentityModel.Client;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
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

#if DEBUG
    internal class CustomHttphandler : HttpClientHandler
    {
        public CustomHttphandler() : base()
        {
            ServerCertificateCustomValidationCallback = (message, certificate, chain, sslPolicyErrors) => true;
        }
    }
#endif

    //[XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage
    {
        OidcClient _client;
        private static LoginResult _result;

        private static Lazy<HttpClient> _apiClient = null;
        private static HttpClientHandler _handler = null;
        private static string _callbackOutput = null;

        private string _authority;
        private string _apiBaseAddress;
        private string _api;

        Dictionary<string, string> _stsList = new Dictionary<string, string>
        {
            { "Local STS (localhost:5000)", "https://10.0.2.2:5000,https://10.0.2.2:5001/,identity" },
            { "demo.identityserver.io", "https://demo.identityserver.io,https://demo.identityserver.io/,api/test" }
        };

        public MainPage()
        {
            InitializeComponent();

            ViewDisco.Clicked += ViewDisco_Clicked;
            ViewOpenIDConfiguration.Clicked += ViewOpenIDConfiguration_Clicked;
            AuthorizeCallApi.Clicked += AuthorizeCallApi_Clicked;
            Login.Clicked += Login_Clicked;
            CallApi.Clicked += CallApi_Clicked;

            if (_callbackOutput != null)
            {
                DisplayOutput(_callbackOutput);
                _callbackOutput = null;
            }

            foreach (var sts in _stsList.Keys)
            {
                StsPicker.Items.Add(sts);
            }

#if DEBUG
            // dev env setup - handle/bypass certificate errors
            //DependencyService.Register<CustomHttphandler>();
            //_handler = DependencyService.Resolve<CustomHttphandler>();
            _handler = new CustomHttphandler();
#endif

            StsPicker.SelectedIndexChanged += (sender, args) =>
            {
                if (StsPicker.SelectedIndex >= 0)
                {
                    
                    var selectedItem = StsPicker.SelectedItem.ToString();

                    if (selectedItem == "Local STS (localhost:5000)")
                    {
                        CallApiWeatherforecast.Clicked += CallApiWeatherforecast_Clicked;
                        CallApiWeatherforecast.IsVisible = true;
                    }
                    else
                    {
                        CallApiWeatherforecast.Clicked -= CallApiWeatherforecast_Clicked;
                        CallApiWeatherforecast.IsVisible = false;
                    }

                    var stsAndApi = _stsList[selectedItem].Split(',');
                    _authority = stsAndApi[0];
                    _apiBaseAddress = stsAndApi[1];
                    _api = stsAndApi[2];

                    _handler = _handler ?? new HttpClientHandler();

                    _apiClient = _apiClient ?? new Lazy<HttpClient>(() =>
                    {
                        return new HttpClient(_handler);
                    });

                    var browser = DependencyService.Get<IBrowser>();

                    var options = new OidcClientOptions
                    {
                        Authority = _authority,
                        ClientId = "native.hybrid",
                        ClientSecret = "secret",
                        Scope = "openid profile email api offline_access",
                        RedirectUri = "xamarinformsclients://callback",
                        Browser = browser,

                        ResponseMode = OidcClientOptions.AuthorizeResponseMode.Redirect,

                        Flow = OidcClientOptions.AuthenticationFlow.Hybrid

#if DEBUG
                    // dev env setup - handle/bypass certificate errors
                    ,
                        BackchannelHandler = _handler
#endif
                    };

                    _client = new OidcClient(options);
                }
            };

            StsPicker.SelectedIndex = 1;
        }

        private void ResetOutput()
        {
            OutputText.Text = String.Empty;
        }

        private void DisplayOutput(object output)
        {
            OutputText.Text += BeautifyJson(output) + Environment.NewLine;
        }

        private string BeautifyJson(object json)
        {
            try
            {
                if (json?.GetType() == typeof(string))
                {
                    //return JArray.Parse(Convert.ToString(json)).ToString(Formatting.Indented);

                    return JObject.Parse(Convert.ToString(json)).ToString(Formatting.Indented);
                }
                else if (json != null)
                {
                    return JsonConvert.SerializeObject(json, Formatting.Indented);
                }
            }
            catch (Exception ex)
            {
                if (json?.GetType() == typeof(string))
                    return Convert.ToString(json);

                //result = ex.Message + Environment.NewLine + ex.StackTrace.ToString();
            }
            return "";
        }

        private async void ViewDisco_Clicked(object sender, EventArgs e)
        {
            ResetOutput();
            // Cannot use http in _authority
            var client = new HttpClient(_handler);
            var disco = await GetDisco(client, _authority);
            if (disco.IsError)
            {
                DisplayOutput(disco.Error);
            }
            else
            {
                DisplayOutput(disco);
            }
        }

        private async void ViewOpenIDConfiguration_Clicked(object sender, EventArgs e)
        {
            ResetOutput();
            var client = new HttpClient(_handler);

            client.BaseAddress = new Uri(_authority);
            try
            {
                HttpResponseMessage result = null;
                result = await client.GetAsync(".well-known/openid-configuration");
                DisplayOutput(await result.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                DisplayOutput(ex);
            }
        }

        private async Task<DiscoveryDocumentResponse> GetDisco(HttpClient client, string url)
        {
            var disco = await client.GetDiscoveryDocumentAsync(url);
            return disco;
        }

        private async void AuthorizeCallApi_Clicked(object sender, EventArgs e)
        {
            ResetOutput();
            // discover endpoints from metadata
            var client = new HttpClient(_handler);
            var disco = await GetDisco(client, _authority);
            if (disco.IsError)
            {
                DisplayOutput(disco.Error);
                return;
            }

            // request token
            var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = "client",
                ClientSecret = "secret",

                Scope = "api"
            });

            if (tokenResponse.IsError)
            {
                DisplayOutput(tokenResponse.Error);
                return;
            }

            DisplayOutput(tokenResponse.Json);

            // call api
            var apiClient = new HttpClient(_handler);
            apiClient.BaseAddress = new Uri(_apiBaseAddress);
            apiClient.SetBearerToken(tokenResponse.AccessToken);

            var response = await apiClient.GetAsync(_api);
            if (!response.IsSuccessStatusCode)
            {
                DisplayOutput(response.StatusCode);
            }
            else
            {
                DisplayOutput(await response.Content.ReadAsStringAsync());
            }

            return;
        }

        private async void Login_Clicked(object sender, EventArgs e)
        {
            ResetOutput();
            try
            {
                _result = await _client.LoginAsync(new LoginRequest());
            }
            catch (Exception ex)
            {
                DisplayOutput(ex);
                return;
            }

            if (_result.IsError)
            {
                DisplayOutput(_result.Error);
                return;
            }

            var sb = new StringBuilder(128);
            foreach (var claim in _result.User.Claims)
            {
                sb.AppendFormat("{0}: {1}\n", claim.Type, claim.Value);
            }

            sb.AppendFormat("\n{0}: {1}\n", "refresh token", _result?.RefreshToken ?? "none");
            sb.AppendFormat("\n{0}: {1}\n", "access token", _result.AccessToken);

            // instance state seems to be not statefull
            // So I'm trying to callback pattern to put the message back
            //DisplayOutput(sb.ToString());

            _callbackOutput = sb.ToString();

            _apiClient.Value.SetBearerToken(_result?.AccessToken ?? "");
            _apiClient.Value.BaseAddress = new Uri(_apiBaseAddress);

        }

        private async void CallApi_Clicked(object sender, EventArgs e)
        {
            ResetOutput();
            HttpResponseMessage result = null;
            try
            {
                result = await _apiClient.Value.GetAsync(_api);
            }
            catch (Exception ex)
            {
                DisplayOutput(ex);
            }

            if (result != null && result.IsSuccessStatusCode)
            {
                DisplayOutput(await result.Content.ReadAsStringAsync());
            }
            else
            {
                DisplayOutput(result?.ReasonPhrase);
            }
        }

        private async void CallApiWeatherforecast_Clicked(object sender, EventArgs e)
        {
            ResetOutput();
            HttpResponseMessage result = null;
            try
            {
                var apiClient = new HttpClient(_handler);
                apiClient.BaseAddress = new Uri(_apiBaseAddress);
                result = await apiClient.GetAsync("weatherforecast");
            }
            catch (Exception ex)
            {
                DisplayOutput(ex);
            }

            if (result != null && result.IsSuccessStatusCode)
            {
                DisplayOutput(await result.Content.ReadAsStringAsync());
            }
            else
            {
                DisplayOutput(result?.ReasonPhrase);
            }
        }
    }
}
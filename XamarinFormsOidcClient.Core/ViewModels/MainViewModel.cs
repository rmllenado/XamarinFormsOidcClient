using IdentityModel.OidcClient.Browser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;
using XamarinFormsOidcClient.Core.Services;

namespace XamarinFormsOidcClient.Core.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        ObservableCollection<KeyValuePair<string, string>> _stsList;

        public ObservableCollection<KeyValuePair<string, string>> StsList
        {
            get { return _stsList; }
            set
            {
                _stsList = value;
                SetProperty(ref _stsList, value);
            }
        }

        private KeyValuePair<string,string> _selectedSts;
        public KeyValuePair<string, string> SelectedSts
        {
            get { return _selectedSts; }
            set
            {
                SetProperty(ref _selectedSts, value);
            }
        }

        private string _output;

        public string Output
        {
            get { return _output; }
            set
            {
                SetProperty(ref _output, value);
            }
        }

        private bool _isAuthenticated;
        public bool IsAuthenticated
        {
            get { return _isAuthenticated; }
            set
            {
                SetProperty(ref _isAuthenticated, value);
            }
        }

        public bool IsUnauthenticated
        {
            get { return !_isAuthenticated; }
        }

        public ICommand GetDiscoCommand { protected set; get; }
        public ICommand GetOpenIdConfigurationCommand { protected set; get; }
        public ICommand LoginCommand { protected set; get; }
        public ICommand LogoutCommand { protected set; get; }
        public ICommand CallApiCommand { protected set; get; }

        public MainViewModel()
        {
            IStsService stsService = new StsService(DependencyService.Get<IBrowser>());

            StsList = new ObservableCollection<KeyValuePair<string, string>>(stsService.GetStsList());

            SelectedSts = StsList[0];

            IsAuthenticated = false;

            GetDiscoCommand = new Command(async () =>
            {
                ResetOutput();
                var disco = await stsService.GetDiscoAsync();
                if (disco.IsError)
                {
                    DisplayOutput(disco.Error);
                }
                else
                {
                    DisplayOutput(disco);
                }
            });

            GetOpenIdConfigurationCommand = new Command(async () =>
            {
                ResetOutput();
                var opendIdConfig = await stsService.GetOpenIdConfigurationAsync();

                DisplayOutput(opendIdConfig);
            });

            LoginCommand = new Command(async () =>
            {
                ResetOutput();
                var loginResult = await stsService.Login();

                if (loginResult.IsError)
                {
                    DisplayOutput(loginResult.Error);
                }
                else
                {
                    IsAuthenticated = true;

                    var sb = new StringBuilder(128);
                    foreach (var claim in loginResult.User.Claims)
                    {
                        sb.AppendFormat("{0}: {1}\n", claim.Type, claim.Value);
                    }

                    sb.AppendFormat("\n{0}: {1}\n", "refresh token", loginResult?.RefreshToken ?? "none");
                    sb.AppendFormat("\n{0}: {1}\n", "access token", loginResult.AccessToken);

                    DisplayOutput(sb.ToString());
                }
            });

            LogoutCommand = new Command(async () =>
            {
                ResetOutput();
                var logoutResult = await stsService.Logout();

                if (logoutResult.IsError)
                {
                    DisplayOutput(logoutResult.Error);
                }
                else
                {
                    IsAuthenticated = false;
                }
            });

            CallApiCommand = new Command(async () =>
            {
                ResetOutput();
                var result = await stsService.CallApiAsync("api/test");
                DisplayOutput(result);
            });
        }

        private void ResetOutput()
        {
            Output = String.Empty;
        }

        private void DisplayOutput(object output)
        {
            Output += BeautifyJson(output) + Environment.NewLine;
        }

        private string BeautifyJson(object json)
        {
            try
            {
                if (json?.GetType() == typeof(string))
                {
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
            }
            return "";
        }
    }
}

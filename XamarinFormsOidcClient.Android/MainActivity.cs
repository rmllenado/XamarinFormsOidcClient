
using Android.App;
using Android.Content.PM;
using Android.OS;
using Xamarin.Forms;
using XamarinFormsOidcClient.Core;

namespace XamarinFormsOidcClient.Droid
{
    [Activity(Label = "XamarinFormsOidcClient", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        //protected override void OnCreate(Bundle savedInstanceState)
        //{
        //    TabLayoutResource = Resource.Layout.Tabbar;
        //    ToolbarResource = Resource.Layout.Toolbar;

        //    base.OnCreate(savedInstanceState);

        //    Xamarin.Essentials.Platform.Init(this, savedInstanceState);
        //    global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
        //    LoadApplication(new App());
        //}
        //public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        //{
        //    Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        //    base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        //}

        protected override void OnCreate(Bundle bundle)
        {
            DependencyService.Register<ChromeCustomTabsBrowser>();

            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

#if DEBUG
            //https://www.midnightcreative.io/coding/ignore-ssl-certificate-errors-in-xamarin-net-core-servercertificatevalidationcallback-is-not-called-anymore-on-android/
            //Since the Xamarin.Android version 10 update, the ServicePointManager.ServerCertificateValidationCallback is not called anymore for Xamarin.Android as the team is going towards full compatibility with .NET Core.
            //ServicePointManager.ServerCertificateValidationCallback += (o, cert, chain, sslPolicyErrors) => true;
#endif

            global::Xamarin.Forms.Forms.Init(this, bundle);
            LoadApplication(new App());
        }
    }
}
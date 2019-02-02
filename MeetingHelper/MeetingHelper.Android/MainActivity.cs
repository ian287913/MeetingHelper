using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Android.Support.V4.App;
using Android.Support.V4.Content;


namespace MeetingHelper.Droid
{
    [Activity(Label = "MeetingHelper", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        public static MainActivity MA;  /// For XamarinForms

        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;
            
            base.OnCreate(bundle);

            //  hide status bar and navigation bar
            Window.DecorView.SystemUiVisibility = (StatusBarVisibility)SystemUiFlags.HideNavigation | (StatusBarVisibility)SystemUiFlags.Fullscreen | (StatusBarVisibility)SystemUiFlags.ImmersiveSticky;
            Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#282828"));


            MA = this;
            /// Permissions
            Plugin.CurrentActivity.CrossCurrentActivity.Current.Init(this, bundle);
            // Check whether all permissions are granted.
            bool AllGranted = true;
            String[] permissions = new String[12] { Android.Manifest.Permission.RecordAudio, Android.Manifest.Permission.Bluetooth, Android.Manifest.Permission.BluetoothAdmin, Android.Manifest.Permission.AccessCoarseLocation, Android.Manifest.Permission.AccessFineLocation, Android.Manifest.Permission.ChangeWifiState, Android.Manifest.Permission.AccessWifiState, Android.Manifest.Permission.ChangeWifiMulticastState, Android.Manifest.Permission.ReadExternalStorage, Android.Manifest.Permission.WriteExternalStorage, Android.Manifest.Permission.Bluetooth, Android.Manifest.Permission.BluetoothAdmin };
            for (int i = 0; i < permissions.Length; i++)
            {
                if (ContextCompat.CheckSelfPermission(this, permissions[i]) != Permission.Granted)
                    AllGranted = false;
            }
            if (AllGranted == false)
            {
                ActivityCompat.RequestPermissions(this, permissions, 0);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Permission Granted!!!");
            }
            /// Permissions
            global::Xamarin.Forms.Forms.Init(this, bundle);
            LoadApplication(new App());
        }
    }
}


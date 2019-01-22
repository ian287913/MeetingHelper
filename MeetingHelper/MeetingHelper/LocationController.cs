using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using System;
namespace Controller
{
    public class LocationController
    {
        public Location location { get; private set; } = null;
        private LocationManager manager = null;
        public LocationListener locationListener;
        //  For wifi scanning - without real location function
        public LocationController(Action<string> OnException)
        {
            try
            {
                manager = (LocationManager)Android.App.Application.Context.GetSystemService(Context.LocationService);
                if (!manager.IsProviderEnabled(LocationManager.GpsProvider))    //  No location service
                    OnException?.Invoke("To scan nearby wifi, please turn on the Location Service.");
            }
            catch (Exception e)
            {
                OnException?.Invoke("LocationManager init failed: " + e.Message);
            }
        }
        //  For location - real location function
        public LocationController(Action<Location> LocationChange)
        {
            manager = (LocationManager)Android.App.Application.Context.GetSystemService(Context.LocationService);
            if (!manager.IsProviderEnabled(LocationManager.GpsProvider))    //  No location service
            {
                Android.App.Application.Context.StartActivity(new Intent(Settings.ActionLocationSourceSettings));
            }
            locationListener = new LocationListener();
            locationListener.LocationChanged += OnLocationChanged;
            locationListener.LocationChanged += LocationChange;
            manager.RequestLocationUpdates(LocationManager.NetworkProvider, 1000, 0, locationListener);
        }
        private void OnLocationChanged(Location newLocation)
        {
            location = newLocation;
            manager.RemoveUpdates(locationListener);
        }
        public bool IsManagerEnable()
        {
            if (manager != null)
                return manager.IsProviderEnabled(LocationManager.GpsProvider);
            else
                return false;
        }
        public void EnableManager()
        {
            if (manager != null)
                Android.App.Application.Context.StartActivity(new Intent(Settings.ActionLocationSourceSettings));
        }
        public float Distance(double lng, double lat)
        {
            Location l = new Location("")
            {
                Longitude = lng,
                Latitude = lat
            };
            return location == null ? 0.0f : location.DistanceTo(l);
        }
    }
    public class LocationListener : Java.Lang.Object, ILocationListener
    {
        public Action<Location> LocationChanged;

        public void OnLocationChanged(Location location)
        {
            LocationChanged?.Invoke(location);
        }
        public void OnProviderDisabled(string provider)
        {
            //throw new NotImplementedException();
        }
        public void OnProviderEnabled(string provider)
        {
            //throw new NotImplementedException();
        }
        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
            //throw new NotImplementedException();
        }
    }
}

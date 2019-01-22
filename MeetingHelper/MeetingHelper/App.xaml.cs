using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation (XamlCompilationOptions.Compile)]
namespace MeetingHelper
{
	public partial class App : Application
	{
        //  Save & Load for Username
        const string Property_Username = "UserName";
        public string UserName { set; get; }
        //  Save & Load for Debug_LastSleep
        const string Property_LastSleep = "LastSleep";
        public bool LastSleep { set; get; }
        
        public App ()
		{
			InitializeComponent();

            //  Debug_LastSleep
            
            if (Properties.ContainsKey(Property_LastSleep))
                LastSleep = (bool)Properties[Property_LastSleep];
            Properties[Property_LastSleep] = false;

            //  UserName
            if (Properties.ContainsKey(Property_Username))
                UserName = (string)Properties[Property_Username];
            else
                UserName = "Unnamed";

            MainPage = new MainPage();
		}

        protected override void OnStart ()
		{
			// Handle when your app starts
		}

		protected override void OnSleep ()
		{
            // Handle when your app sleeps

            // Debug_LastSleep
            Properties[Property_LastSleep] = true;
            // Save UserName
            Properties[Property_Username] = UserName;
        }

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}
	}
}

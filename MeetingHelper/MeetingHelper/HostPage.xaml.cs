using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Controller;
using Controller.Component;
using System.Collections.ObjectModel;

namespace MeetingHelper
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class HostPage : ContentPage
	{
        App app = Application.Current as App;

        ObservableCollection<ianGuest> Guests;

        public HostPage ()
		{
			InitializeComponent ();

            //  init list
            Guests = new ObservableCollection<ianGuest>();
            ListView_Guests.ItemsSource = Guests;

            //  get room info
            Debug_Label.Text = $"Name:{app.user.Config.Name}, inRoom:{app.user.Config.IsInRoom}, HaveMic:{app.user.Config.HaveMic}";
            Debug_Label2.Text = $"Name:{app.user.RoomConfig.Name}, Speaker:{app.user.RoomConfig.Speaker}";

            Guests.Clear();
            foreach (string guest in app.user.RoomConfig.UserList)
            {
                Guests.Add(new ianGuest(guest));
            }

            //  start broadcast
            app.myRoom.StartBroadcast(0, TimeUnit.Second);
        }

        //  Warning
        private void Warning(string title, string message)
        {
            //  show warning and disable main layout
            Device.BeginInvokeOnMainThread(() =>
            {
                Warning_Title.Text = title;
                Warning_Content.Text = message;
                Warning_Layout.IsVisible = true;
            });
        }
        private void Warning_Clicked(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                Warning_Layout.IsVisible = false;
            });
        }
    }

    public class ianGuest : BindableObject
    {
        public string Name { get; set; }
        public string IsSpeaking { get; set; }
        
        public ianGuest(string name)
        {
            Name = name;
            IsSpeaking = "False";
        }
    }
}
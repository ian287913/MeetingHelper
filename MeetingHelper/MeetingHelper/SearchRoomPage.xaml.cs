using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using System.Collections.ObjectModel;
using Controller;

namespace MeetingHelper
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SearchRoomPage : ContentPage
	{
        //  Rooms ItemSource
        ObservableCollection<Room> Rooms;
        //  Wifi controller
        WifiController mWifiController;
        // LocationController mLocationController;
        bool show_Warning;
        bool show_Password;
        bool show_Create;

        Room targetRoom;

        public SearchRoomPage ()
		{
			InitializeComponent ();
            
            //  init Room ItemSource
            Rooms = new ObservableCollection<Room>();
            /// FAKE DATA
            Rooms.Add(new Room("Alpha Room", "Ian287913", "2019/1/21 19:35", ""));
            Rooms.Add(new Room("Bravo", "Founder", "2018/1/1 19:08", "ADCB"));
            Rooms.Add(new Room("Charlie", "someone", "1998/12/16 07:32", "1234"));

            ListView_Rooms.ItemsSource = Rooms;

            //  init WiFi
            mWifiController = new WifiController();
            mWifiController.OnNetworkChanged += OnStatusChanged;

            //  init Visibility
            show_Warning = false;
            show_Password = false;
            show_Create = false;
            show_Layout();
            
            //  click event
            ListView_Rooms.ItemTapped += (sender, e) =>
            {
                //  get target room
                targetRoom = e.Item as Room;
                ((ListView)sender).SelectedItem = null;
                if(targetRoom.Password != "")
                {
                    //  enable password layout
                    show_Password = true;
                    show_Layout();
                }
                else
                {
                    /// Enter room...
                    Warning($"{targetRoom.Name}", $"Entering \"{targetRoom.Name}\"...");
                }
            };
        }


        //  update Layouts' visibility
        private void show_Layout()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                Main_Layout.IsEnabled = false;

                Create_Layout.IsVisible = false;
                Create_Layout.IsEnabled = false;
                Warning_Layout.IsVisible = false;
                Warning_Layout.IsEnabled = false;
                Password_Layout.IsVisible = false;
                Password_Layout.IsEnabled = false;

                if (show_Warning)
                {
                    Warning_Layout.IsVisible = true;
                    Warning_Layout.IsEnabled = true;
                }
                else if (show_Create)
                {
                    Create_Layout.IsVisible = true;
                    Create_Layout.IsEnabled = true;
                }
                else if (show_Password)
                {
                    Password_Layout.IsVisible = true;
                    Password_Layout.IsEnabled = true;
                }
                else
                    Main_Layout.IsEnabled = true;
            });
        }

        #region delegeates

        //  Create Room
        private void OnClicked_Create(object sender, EventArgs e)
        {
            show_Create = true;
            show_Layout();
        }
        //  Create Confirm
        private void Create_Confirm_Clicked(object sender, EventArgs e)
        {
            ///...
        }

        //  Enter Password
        private void Password_Clicked(object sender, EventArgs e)
        {
            /// validate password...
            if (Password_Entry.Text == targetRoom.Password)
            {
                Password_Label_Wrong.IsVisible = false;
                show_Password = false;
                show_Layout();

                /// Enter Room...
                //...
            }
            else
                Password_Label_Wrong.IsVisible = true;
        }

        //  Go back
        private void GoBack(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                Password_Label_Wrong.IsVisible = false;
                Password_Entry.Text = "";
                Create_RoomName_Entry.Text = "";
                Create_Password_Entry.Text = "";
            });

            show_Create = false;
            show_Password = false;
            show_Layout();
        }

        //  WiFi status changed
        private void OnStatusChanged(object sender, NetworkChangedEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (e.State.Equals(WifiDetailedState.Connected))
                {
                    Label_WiFi_Name.Text = mWifiController.ConnectionInfo.SSID;
                    Label_WiFi_Content.Text = $"BSSID: {mWifiController.ConnectionInfo.BSSID}\nLink Speed: {mWifiController.ConnectionInfo.LinkSpeed} Mbps";
                }
                else
                {
                    Label_WiFi_Name.Text = e.State.ToString();
                    Label_WiFi_Content.Text = "Please connect to any WiFi to join or create a room.";
                }
            });
        }
        
        private void Warning(string title, string message)
        {
            //  show warning and disable main layout
            Device.BeginInvokeOnMainThread(() =>
            {
                Warning_Title.Text = title;
                Warning_Content.Text = message;
            });
            show_Warning = true;
            show_Layout();
        }
        private void Warning_Clicked(object sender, EventArgs e)
        {
            show_Warning = false;
            show_Layout();
        }

        #endregion
    }

    public class Room : BindableObject
    {
        public string Name { get; set; }
        public string Founder { get; set; }
        public string Found_Time { get; set; }
        public string Password { get; set; }
        private bool _isSelected;

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }
        
        public Room(string name, string founder, string foundTime, string password)
        {
            Name = name;
            Founder = founder;
            Found_Time = foundTime;
            Password = password;
            _isSelected = false;
        }
    }
}
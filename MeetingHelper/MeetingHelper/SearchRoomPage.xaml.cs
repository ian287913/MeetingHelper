using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using System.Collections.ObjectModel;
using Controller;
using Controller.Component;
using System.Net;

/// <summary>
/// 目前房間資訊沒有: Host名、建立時間、有無密碼
/// TODO: 防轉向、頂條、implementation、WiFi內容更新
/// </summary>

namespace MeetingHelper
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SearchRoomPage : ContentPage
	{
        //  Include WiFi, User, Room...
        App app = Application.Current as App;
        
        //  Rooms ItemSource
        ObservableCollection<ianRoom> Rooms;
        
        // LocationController mLocationController;
        bool show_Warning;
        bool show_Password;
        bool show_Create;

        ianRoom targetRoom;

        public SearchRoomPage ()
		{
			InitializeComponent ();

            ///  init DJ
            app.user.StartListener();
            app.user.OnEnterRoom += User_OnEnterRoom;
            app.user.OnRoomListChanged += User_OnRoomListChanged;

            //  init Room ItemSource
            Rooms = new ObservableCollection<ianRoom>();

            /// FAKE DATA
            //Rooms.Add(new ianRoom("Alpha Room", "Ian287913", "2019/1/21 19:35", ""));
            //Rooms.Add(new ianRoom("Bravo", "Founder", "2018/1/1 19:08", "ABCD"));
            //Rooms.Add(new ianRoom("Charlie", "someone", "1998/12/16 07:32", "1234"));
            //Rooms.Add(new ianRoom("Charlie", "someone", "1998/12/16 07:32", "1234"));
            //Rooms.Add(new ianRoom("Charlie", "someone", "1998/12/16 07:32", "1234"));
            //Rooms.Add(new ianRoom("Charlie", "someone", "1998/12/16 07:32", "1234"));
            /// FAKE DATA

            ListView_Rooms.ItemsSource = Rooms;

            //  init WiFi
            app.mWifiController.OnNetworkChanged += OnStatusChanged;

            //  init Entry
            Create_Password_Entry.Text = "";
            Create_RoomName_Entry.Text = "";
            Password_Entry.Text = "";

            //  init Visibility
            show_Warning = false;
            show_Password = false;
            show_Create = false;
            show_Layout();
            
            //  click event
            ListView_Rooms.ItemTapped += (sender, e) =>
            {
                //  get target room
                targetRoom = e.Item as ianRoom;
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

        

        #region User_Events

        private void User_OnEnterRoom(object sender, EventArgs e)
        {
            Create_Label.Text = "entering room...";
            app.user.StopListener();
            // Host page
            Device.BeginInvokeOnMainThread(() =>
            {
                Navigation.PushModalAsync(new HostPage());
            });
        }

        private void User_OnRoomListChanged(object sender, EventArgs e)
        {
            //  get room list into listview
            Device.BeginInvokeOnMainThread(() =>
            {
                Rooms.Clear();
                foreach (string room in app.user.RoomList)
                {
                    Rooms.Add(new ianRoom(room, "undefined", "2019/1/21 19:35", "1234"));
                }
            });
        }

        #endregion

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

        //  disable BackButton
        protected override bool OnBackButtonPressed()
        {
            ///...
            return true;
        }

        #region delegeates

        //  Create Room
        private void OnClicked_Create(object sender, EventArgs e)
        {
            show_Create = true;
            show_Layout();
        }
        //  Create Confirm (Validation)
        private void Create_Confirm_Clicked(object sender, EventArgs e)
        {
            if(Create_RoomName_Entry.Text.Length > 12)
            {
                Create_Label.TextColor = Color.FromHex("FF4444");
                Create_Label.Text = "the length of Room-Name should less than 12.";
                Create_Label.IsVisible = true;
            }
            else if (Create_RoomName_Entry.Text == "")
            {
                Create_Label.TextColor = Color.FromHex("FF4444");
                Create_Label.Text = "Room-Name can not be unnamed.";
                Create_Label.IsVisible = true;
            }
            else if(Create_Password_Entry.Text.Length > 8)
            {
                Create_Label.TextColor = Color.FromHex("FF4444");
                Create_Label.Text = "the length of Password should less than 8.";
                Create_Label.IsVisible = true;
            }
            else
            {
                Create_Label.TextColor = Color.FromHex("44FF44");
                Create_Label.Text = "creating...\nscaning nearby roomname...";
                Create_Label.IsVisible = true;

                /// Create a room
                app.myRoom = new Room(Create_RoomName_Entry.Text, Create_Password_Entry.Text);
                app.myRoom.Open();
                app.myRoom.StartBroadcast(0, TimeUnit.Second);
                //  get current IP
                string strHostName = Dns.GetHostName();
                IPHostEntry iphostentry = Dns.GetHostEntry(strHostName);
                IPAddress ip = iphostentry.AddressList[0];
                //  (get into the room)
                app.user.BecomeHost(Create_RoomName_Entry.Text, Create_Password_Entry.Text, app.UserName, ip);
                //  wait for enter event...
            }
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
                Create_Label.IsVisible = false;
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
                    Label_WiFi_Name.Text = app.mWifiController.ConnectionInfo.SSID;
                    Label_WiFi_Content.Text = $"BSSID: {app.mWifiController.ConnectionInfo.BSSID}\nLink Speed: {app.mWifiController.ConnectionInfo.LinkSpeed} Mbps";
                }
                else
                {
                    Label_WiFi_Name.Text = e.State.ToString();
                    Label_WiFi_Content.Text = "Please connect to any WiFi to join or create a room.";
                }
            });
        }
        
        //  Warning
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

    public class ianRoom : BindableObject
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
        
        public ianRoom(string name, string founder, string foundTime, string password)
        {
            Name = name;
            Founder = founder;
            Found_Time = foundTime;
            Password = password;
            _isSelected = false;
        }
    }
}
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


/// <summary>
/// ToDo:
/// =Guest=
/// Guest可以主動還回Mic嗎?
/// Guest可以取消 Ask 嗎?
/// =Host=
/// ??  AcceptAsker("Unasked Guest")?
/// Host可以直接從Guest取回Mic嗎?
/// Host可以 give mic 給沒有 Ask 的Guest嗎? - AcceptAsker("Unasked Guest")?
/// 發現一個怪怪的function呼叫: OpenMic() -> Open() -> CloseMic()...
/// </summary>
namespace MeetingHelper
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class HostPage : ContentPage
	{
        App app = Application.Current as App;

        ObservableCollection<ianGuest> Guests;
        bool Do_Update_WiFi = false;
        //  Button(switch) status
        bool isDoorOpen;
        bool isSoundOn;

        public HostPage ()
		{
			InitializeComponent ();

            //  init list
            Guests = new ObservableCollection<ianGuest>();
            ListView_Guests.ItemsSource = Guests;

            //  bind events
            app.user.OnRoomListChanged += User_OnRoomListChanged;
            app.user.OnSpeakerChanged += User_OnSpeakerChanged;
            app.user.OnRequest += User_OnRequest;
            app.user.OnForbid += User_OnForbid;
            app.user.OnError += User_OnError;

            //  start update WiFi
            Update_WiFi();

            //  Button(switch) status
            isDoorOpen = true;
            isSoundOn = false;
            //  start broadcast
            app.myRoom.StartBroadcast(1, TimeUnit.Hour);

            //  List Click Event
            ListView_Guests.ItemTapped += (sender, e) =>
            {
                //  get target guest
                ianGuest target = e.Item as ianGuest;
                
                ((ListView)sender).SelectedItem = null;
                //  give Mic to target
                app.user.AcceptAsker(target.Name);
                /// debug
                UpdateList();
                UpdateButton();
            };
        }

        //  init page
        protected override void OnAppearing()
        {
            base.OnAppearing();

            //  get room info
            Device.BeginInvokeOnMainThread(() =>
            {
                RoomName_Label.Text = app.user.RoomConfig.Name;
            });
            //  update list
            UpdateList();
            UpdateButton();
            //  update WiFi
            Do_Update_WiFi = true;
        }

        private void UpdateList()
        {
            //  update list
            ianGuest tempGuest;
            Guests.Clear();
            foreach (string guest in app.user.RoomConfig.UserList)
            {
                tempGuest = new ianGuest(guest);
                if (guest == app.user.RoomConfig.Host)
                    tempGuest.BeHost();
                if (guest == app.user.RoomConfig.Speaker)
                    tempGuest.BeSpeaker();
                if (app.user.RoomConfig.AskerList.Contains(guest))
                    tempGuest.BeRequested();
                Guests.Add(tempGuest);
            }
        }
        private void UpdateButton()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                //  Guest Speaking
                if (!app.user.Config.HaveMic)
                {
                    Action_Button.Text = "Take Mic";
                    Action_Button.BackgroundColor = Color.FromHex("#555555");
                    Action_BoxView_L.BackgroundColor = Color.FromHex("#808080");
                    Action_BoxView_R.BackgroundColor = Color.FromHex("#808080");
                    Action_Button.IsEnabled = true;
                }
                //  Guest Requested
                else if (app.user.RoomConfig.AskerList.Length != 0)
                {
                    Action_Button.Text = $"[{app.user.RoomConfig.AskerList[0]}]";
                    Action_Button.BackgroundColor = Color.FromHex("#88FFA922");
                    Action_BoxView_L.BackgroundColor = Color.FromHex("#44FF44");
                    Action_BoxView_R.BackgroundColor = Color.FromHex("#44FF44");
                    Action_Button.IsEnabled = true;
                }
                //  No Guest Requested
                else
                {
                    Action_Button.Text = "Speaking...";
                    Action_Button.BackgroundColor = Color.FromHex("#88FFA922");
                    Action_BoxView_L.BackgroundColor = Color.FromHex("#FFA500");
                    Action_BoxView_R.BackgroundColor = Color.FromHex("#FFA500");
                    Action_Button.IsEnabled = true;
                }
            });
        }

        //  Async - Update WiFi info
        async void Update_WiFi()
        {
            await Task.Delay(1000);
            while (Do_Update_WiFi)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    if (app.mWifiController.currentStatus.State.Equals(WifiDetailedState.Connected))
                        SSID_Label.Text = app.mWifiController.ConnectionInfo.SSID;
                    else
                    {
                        SSID_Label.Text = $"({app.mWifiController.currentStatus.State.ToString()})";
                        /// debug : warning
                    }
                });
                await Task.Delay(500);
            }
        }

        #region User Events
        private void User_OnRoomListChanged(object sender, EventArgs e)
        {
            UpdateList();
            UpdateButton();
        }
        private void User_OnSpeakerChanged(object sender, UserEventArgs e)
        {
            UpdateList();
            UpdateButton();
        }
        private void User_OnRequest(object sender, UserEventArgs e)
        {
            UpdateList();
            UpdateButton();
        }
        private void User_OnForbid(object sender, EventArgs e)
        {
            Warning("Error_Forbid", e.ToString());
            /// exit room
            /// ...(implement after "dismiss")
        }
        private void User_OnError(object sender, ErrorEventArgs e)
        {
            Warning("Error", e.Exception.Message);
        }
        #endregion

        #region Button Clicks
        private void Door_Clicked(object sender, EventArgs e)
        {
            if (isDoorOpen)
            {
                //  close door (stop broadcast)
                isDoorOpen = false;
                Device.BeginInvokeOnMainThread(()=> Door_BoxView.BackgroundColor = Color.FromHex("FF4444"));
                app.myRoom.StopBroadcast();
            }
            else
            {
                //  open door (start broadcast)
                isDoorOpen = true;
                Device.BeginInvokeOnMainThread(() => Door_BoxView.BackgroundColor = Color.FromHex("44FF44"));
                app.myRoom.StartBroadcast(0, TimeUnit.Second);
            }
        }
        private void Sound_Clicked(object sender, EventArgs e)
        {
            if (isSoundOn)
            {
                //  close mic
                isSoundOn = false;
                Device.BeginInvokeOnMainThread(() => Sound_BoxView.BackgroundColor = Color.FromHex("FF4444"));
                app.myRoom.CloseMic();
            }
            else
            {
                //  open mic
                isSoundOn = true;
                Device.BeginInvokeOnMainThread(() => Sound_BoxView.BackgroundColor = Color.FromHex("44FF44"));
                app.myRoom.OpenMic();
            }
        }
        private void Exit_Clicked(object sender, EventArgs e)
        {
            //  close room
            //...
            Do_Update_WiFi = false;
            app.myRoom.Close();
            //  Exit page
            Navigation.PopModalAsync();
        }
        private void Action_Clicked(object sender, EventArgs e)
        {
            //  Guest Speaking
            if (!app.user.Config.HaveMic)
            {
                //  Take Mic
                ///...
            }
            //  Guest Requested
            else if (app.user.RoomConfig.AskerList.Length != 0)
            {
                //  Give Mic to the first name in the AskList
                app.user.AcceptAsker(app.user.RoomConfig.AskerList[0]);
            }
            //  No Guest Requested
            else
            {
                /// do nothing...
            }
            UpdateButton();
        }
        #endregion

        #region Warning
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
        #endregion
    }

    public class ianGuest : BindableObject
    {
        public string Name { get; set; }
        public string Color { get; set; }
        public string SideColor { get; set; }
        public string Host { get; set; }
        /// <summary>
        /// NSR { 0:normal 1:speaking 2:request }
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isSpeaking"></param>
        public ianGuest(string name)
        {
            Name = name;
            Host = "";
            SideColor = "#707070";
            Color = "#444444";
        }
        public void BeSpeaker()
        {
            SideColor = "#FFA500";
            Color = "#88FFA922";
        }
        public void BeHost()
        {
            Host = "H";
        }
        public void BeRequested()
        {
            SideColor = "#FFA500";
        }
    }
}
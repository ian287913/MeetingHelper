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
	public partial class GuestPage : ContentPage
	{
        App app = Application.Current as App;

        ObservableCollection<ianGuest> Guests;

        bool Do_Update_WiFi = false;

        public GuestPage ()
		{
			InitializeComponent ();
            
            //  init list
            Guests = new ObservableCollection<ianGuest>();
            ListView_Guests.ItemsSource = Guests;
            Debug_ListView.ItemsSource = app.DebugList;

            //  bind events
            app.user.OnMicCapture += User_OnMicCapture;
            app.user.OnMicMissing += User_OnMicMissing;
            app.user.OnRoomListChanged += User_OnRoomListChanged;
            app.user.OnSpeakerChanged += User_OnSpeakerChanged;
            app.user.OnRequest += User_OnRequest;
            app.user.OnForbid += User_OnForbid;
            app.user.OnError += User_OnError;

            //  start update WiFi
            Update_WiFi();

            //  List Click Event
            ListView_Guests.ItemTapped += (sender, e) =>
            {
                //  get target guest
                ianGuest target = e.Item as ianGuest;
                ((ListView)sender).SelectedItem = null;
                /// Guest do nothing...
            };
            Debug_ListView.ItemTapped += (sender, e) =>
            {
                ((ListView)sender).SelectedItem = null;
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

            //  Debug
            Debug("Page OnAppearing");
            Switch_Debug(false);
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
                //  Speaking
                if (app.user.Config.HaveMic)
                {
                    Action_Button.Text = "Speaking...";
                    Action_Button.BackgroundColor = Color.FromHex("#88FFA922");
                    Action_BoxView_L.BackgroundColor = Color.FromHex("#FFA500");
                    Action_BoxView_R.BackgroundColor = Color.FromHex("#FFA500");
                    Action_Button.IsEnabled = true;
                }
                //  Requested
                else if(app.user.RoomConfig.AskerList.Contains(app.user.Config.Name))   
                {
                    Action_Button.Text = "Requested";
                    Action_Button.BackgroundColor = Color.FromHex("#555555");
                    Action_BoxView_L.BackgroundColor = Color.FromHex("#FFA500");
                    Action_BoxView_R.BackgroundColor = Color.FromHex("#FFA500");
                    Action_Button.IsEnabled = true;
                }
                //  IrRequested
                else
                {
                    Action_Button.Text = "Request";
                    Action_Button.BackgroundColor = Color.FromHex("#555555");
                    Action_BoxView_L.BackgroundColor = Color.FromHex("#808080");
                    Action_BoxView_R.BackgroundColor = Color.FromHex("#808080");
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
        private void User_OnMicMissing(object sender, EventArgs e)
        {
            UpdateList();
            UpdateButton();
        }
        private void User_OnMicCapture(object sender, EventArgs e)
        {
            UpdateList();
            UpdateButton();
        }
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
        //  disable BackButton
        protected override bool OnBackButtonPressed()
        {
            return true;
        }

        private void Exit_Clicked(object sender, EventArgs e)
        {
            //  close room
            Do_Update_WiFi = false;
            //  Exit page
            Navigation.PopModalAsync();
        }
        private void Action_Clicked(object sender, EventArgs e)
        {
            //  Speaking
            if (app.user.Config.HaveMic)
            {
                // return mic
                app.user.BackMic();
            }
            //  Requested
            else if (app.user.RoomConfig.AskerList.Contains(app.user.Config.Name))
            {
                /// Disable Request
            }
            else
            {
                //  send request
                app.user.WantMic();
            }
            /// This may not works because the data iss updated by the host(room)
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
        
        #region Debug
        private void Debug(string message)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (app.DebugList.Count <= 0)
                    app.DebugList.Add(new DebugInfo("---", 0));

                if (app.DebugList[0].Debug == message)
                {
                    app.DebugList.Insert(0, new DebugInfo(app.DebugList[0].Debug, app.DebugList[0].Count + 1));
                    app.DebugList.RemoveAt(1);
                }
                else
                    app.DebugList.Insert(0, new DebugInfo(message, 1));
            });
        }
        private void Debug_Clicked(object sender, EventArgs e)
        {
            Switch_Debug(true);
        }
        private void Switch_Debug(bool next)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (next)
                    app.Debug_Status = (app.Debug_Status + 1) % 3;
                switch (app.Debug_Status)
                {
                    case 1:
                        Debug_Layout.IsVisible = true;
                        Debug_Layout.InputTransparent = true;
                        Debug_Layout.BackgroundColor = Color.FromHex("#77000000");
                        break;
                    case 2:
                        Debug_Layout.IsVisible = true;
                        Debug_Layout.InputTransparent = false;
                        Debug_Layout.BackgroundColor = Color.FromHex("#000000");
                        break;
                    default:
                        Debug_Layout.IsVisible = false;
                        Debug_Layout.InputTransparent = true;
                        break;
                }
            });
        }
        private void Debug_Action_Clicked(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                switch (app.Debug_Status)
                {
                    case 1:
                        Debug($"Speaker: { app.user.RoomConfig.Speaker}\nHaveMic: {app.user.Config.HaveMic.ToString()}");
                        break;
                    case 2:
                        app.DebugList.Clear();
                        break;
                    default:
                        break;
                }
            });
        }
        #endregion
    }

}
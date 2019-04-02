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
using System.Net;

namespace MeetingHelper
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class GuestPage : ContentPage
    {
        App app = Application.Current as App;
        Attendant Attendant;
        ObservableCollection<ianGuest> Guests;

        bool Do_Update_WiFi = false;
        //  Attendance
        bool Sign_In_Sent = false;
        bool Sign_In_Recording = false;

        public GuestPage()
        {
            InitializeComponent();

            //  init Attendant
            IPEndPoint ipe = new IPEndPoint(app.user.RoomConfig.Address, NetWorkPort.Signing);
            Attendant = new Attendant(app.UserName, ipe);

            //  init list
            Guests = new ObservableCollection<ianGuest>();
            ListView_Guests.ItemsSource = Guests;
            Debug_ListView.ItemsSource = app.DebugList;

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
            //  Hook Events - User
            app.user.RemoveAllEventHandlers();
            app.user.OnMicCapture += User_OnMicCapture;
            app.user.OnMicMissing += User_OnMicMissing;
            app.user.OnRoomListChanged += User_OnRoomListChanged;
            app.user.OnSpeakerChanged += User_OnSpeakerChanged;
            app.user.OnRequest += User_OnRequest;
            app.user.OnForbid += User_OnForbid;
            app.user.OnError += User_OnError;
            app.user.OnUpdate += User_OnUpdate;
            app.user.OnDisconnect += User_OnDisconnect;
            //  Hook Events - WiFi
            app.mWifiController.ClearEvents();
            WifiController.OnException += MWifiController_OnException;
            //  Hook Events - Audio
            app.audioControl.ClearEvents();
            app.audioControl.OnSendAudio += AudioControl_OnSendAudio;
            AudioControl.OnException += AudioControl_OnException;
            //  Debug
            Debug("GuestPage OnAppearing");
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
                    Action_Button.Text = "發言中...";
                    Action_Button.BackgroundColor = Color.FromHex("#88FFA922");
                    Action_BoxView_L.BackgroundColor = Color.FromHex("#FFA500");
                    Action_BoxView_R.BackgroundColor = Color.FromHex("#FFA500");
                    Action_Button.IsEnabled = true;
                }
                //  Requested
                else if (app.user.RoomConfig.AskerList.Contains(app.user.Config.Name))
                {
                    Action_Button.Text = "已請求";
                    Action_Button.BackgroundColor = Color.FromHex("#555555");
                    Action_BoxView_L.BackgroundColor = Color.FromHex("#FFA500");
                    Action_BoxView_R.BackgroundColor = Color.FromHex("#FFA500");
                    Action_Button.IsEnabled = true;
                }
                //  IrRequested
                else
                {
                    Action_Button.Text = "請求發言";
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
                    {
                        SSID_Label.Text = app.mWifiController.ConnectionInfo.SSID;
                    }
                    else
                    {
                        SSID_Label.Text = $"({app.mWifiController.currentStatus.State})";
                        /// debug : warning
                    }
                });
                await Task.Delay(1000);
            }
        }

        #region User Events
        private void User_OnMicMissing(object sender, EventArgs e)
        {
            UpdateList();
            UpdateButton();
            /// Stop record audio
            Debug("Stop recording...");
            if (app.audioControl.isRecording)
                app.audioControl.StopRecord();
        }
        private void User_OnMicCapture(object sender, EventArgs e)
        {
            UpdateList();
            UpdateButton();
            /// Start record audio
            Debug("Start recording...");
            if (app.audioControl.isRecording)
                app.audioControl.StopRecord();
            app.audioControl.StartRecord();
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
        private void User_OnDisconnect(object sender, EventArgs e)
        {
            Warning("Disconnected", "The current room have been closed.");
            Debug("Disconnected: leave room");
            //  room closed -> exit
            Do_Update_WiFi = false;
            //  Exit Room
            app.user.ExitRoom();
            //  Exit page
            /// Should be: Show Warning -> press OK -> PopPage.
            Navigation.PopModalAsync();
        }
        private void User_OnUpdate(object sender, EventArgs e)
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
            Warning("User_Error", e.Exception.Message);
            Debug($"User_Error:\n{e.Exception.Message}");
        }
        #endregion

        #region WiFi Events
        private void MWifiController_OnException(string message)
        {
            Warning("WiFi_Error", message);
            Debug($"WiFi_Error:\n{message}");
        }
        #endregion

        #region Audio Events
        private void AudioControl_OnSendAudio(short[] buffer, int bufferReadResult)
        {
            /// if(has mic)...
            //  Convert Data To Bytes (ref int result, ref short[] Sarray, out byte[] Barray)
            byte[] Barray;

            int num = 0, Snum = 0;
            Barray = new byte[buffer.Length * 2 + 4];
            byte[] itmp = BitConverter.GetBytes(bufferReadResult);
            for (num = 0; num < 4; num++)
                Barray[num] = itmp[num];
            while (Snum < (buffer.Length))
            {
                byte[] tmp = new byte[2];
                tmp = BitConverter.GetBytes(buffer[Snum]);
                Barray[num] = tmp[0];
                num++;
                Barray[num] = tmp[1];
                num++;
                Snum++;
            }
            //  Send audio(bytep[]) to guest's "Room"
            app.user.SendAudio(Barray);
        }
        private void AudioControl_OnException(string message)
        {
            Warning("Audio_Error", message);
            Debug($"Audio_Error:\n{message}");
        }
        #endregion

        #region Button Clicks
        //  disable BackButton
        protected override bool OnBackButtonPressed()
        {
            return true;
        }

        //  Attendance - Show
        private void Attendance_Clicked(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (!Sign_In_Sent)
                {
                    Attendance_Content.Text = "點擊麥克風開始錄製簽到語音";
                    Attendance_Mic_Image.Source = "Mic_cion.png";
                    Attendance_Completed_Grid.IsVisible = false;
                }
                else
                {
                    Attendance_Content.Text = "簽到完成!\n音檔已傳送至主席";
                    Attendance_Mic_Image.Source = "Mic_Completed_cion.png";
                    Attendance_Completed_Grid.IsVisible = true;
                }
                //  Show attendance window
                Attendance_Layout.IsVisible = true;
            });
        }
        //  Attendance - Mic
        private void Attendance_Mic_Clicked(object sender, EventArgs e)
        {
            if (!Sign_In_Sent)
            {
                //  animation
                ButtonAnimation();

                Device.BeginInvokeOnMainThread(() =>
                {        
                    if (!Sign_In_Recording)
                    {   //  Start Recording
                        Sign_In_Recording = true;

                        ///
                        /// Start Recording
                        Attendance_Mic_Button.BorderColor = Color.FromHex("#A5FF00");
                        Attendance_Content.Text = "錄音中...再次點擊麥克風以結束錄音";
                        Attendance_Mic_Image.Source = "Mic_cion.png";
                    }
                    else
                    {   //  Stop Recording
                        Sign_In_Sent = true;
                        ///  main
                        Attendant.Sign(new byte[] {0});
                        Attendance_Mic_Button.BorderColor = Color.FromHex("#66A5FF00");
                        Attendance_Content.Text = "簽到完成!\n音檔已傳送至主席";
                        Attendance_Mic_Image.Source = "Mic_Completed_cion.png";
                        Attendance_Completed_Grid.IsVisible = true;
                        Attendance_Button.Source = "Attendance_Completed_icon.png";
                    }
                });
            }
            else
            {
                /// Attendance had been sent. No need to record again...
            }
        }
        //  Attendance - Button animation
        private async void ButtonAnimation()
        {
            await Attendance_Mic_Image.ScaleTo(1.5, 200, Easing.CubicOut);
            await Attendance_Mic_Image.ScaleTo(1.0, 400, Easing.SpringOut);
        }
        //  Attendance - Dismiss
        private void Attendance_Dismiss_Clicked(object sender, EventArgs e)
        {
            if (Sign_In_Sent)
            {
                //  Close window
                Device.BeginInvokeOnMainThread(() =>
                {
                    Attendance_Layout.IsVisible = false;
                });
            }
        }

        private void Exit_Clicked(object sender, EventArgs e)
        {
            //  close room
            Do_Update_WiFi = false;
            //  Exit Room
            app.user.ExitRoom();
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
            else if (app.user.RoomConfig.ContainsAsker(app.user.Config.Name))
            {
                /// 原本是 app.user.RoomConfig.AskerList.Contains(app.user.Config.Name)
                /// 可替代為 app.user.RoomConfig.ContainsAsker(app.user.Config.Name);
                /// 但一般使用者的AskerList不會有東西，所以不會觸發。
                /// Disable Request
            }
            else
            {
                //  send request
                app.user.WantMic();
            }

            /// This may not work because the data iss updated by the host(room)
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
﻿using System;
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
        static App app = Application.Current as App;
        AttendenceSheet AttendenceSheet;

        //  Thread lock
        private static object UI_Lock = new object();

        private ObservableCollection<ianGuest> Guests;
        private ObservableCollection<ianAttendant> Attendants;

        bool Do_Update_WiFi = false;
        //  Button(switch) status
        bool isDoorOpen;
        bool isSoundOn;

        public HostPage()
        {
            InitializeComponent();

            //  init AttendancsSheet
            AttendenceSheet = new AttendenceSheet(app.myRoom.Config.Name);

            //  init list
            Guests = new ObservableCollection<ianGuest>();
            Attendants = new ObservableCollection<ianAttendant>();
            ListView_Guests.ItemsSource = Guests;
            Listview_Attendants.ItemsSource = Attendants;
            Debug_ListView.ItemsSource = app.DebugList;

            //  start update WiFi
            Update_WiFi();

            //  Button(switch) status
            isDoorOpen = true;
            isSoundOn = false;
            /// Audio - StopTrack
            Debug("(init - StopTrack)");
            if (app.audioControl.isTracking)
                app.audioControl.StopTrack();

            //  start broadcast
            app.myRoom.StartBroadcast(-1, TimeUnit.Hour);

            //  List Click Event
            ListView_Guests.ItemTapped += (sender, e) =>
            {
                //  get target guest
                ianGuest target = e.Item as ianGuest;

                ((ListView)sender).SelectedItem = null;

                //  give Mic to self
                ///
                Debug($"HostName: {app.user.Config.Name}");
                if (target.Name == app.user.Config.Name)
                    app.user.WantMic();
                //  give Mic to target
                else
                    app.user.AcceptAsker(target.Name);
                
                /// debug
                //UpdateList();
                //UpdateButton();
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
            lock(UI_Lock)
            {
                UpdateList();
                UpdateButton();
            }
            //  update WiFi
            Do_Update_WiFi = true;
            //  Hook Events - User
            app.user.RemoveAllEventHandlers();
            app.user.OnMicCapture += User_OnMicCapture;
            app.user.OnMicMissing += User_OnMicMissing;
            app.user.OnUserJoin += User_OnUserJoin;
            app.user.OnUserExit += User_OnUserExit;
            app.user.OnSpeakerChanged += User_OnSpeakerChanged;
            app.user.OnRequest += User_OnRequest;
            app.user.OnForbid += User_OnForbid;
            app.user.OnError += User_OnError;
            //  Hook Events - WiFi
            app.mWifiController.ClearEvents();
            WifiController.OnException += MWifiController_OnException;
            //  Hook Events - Room
            app.myRoom.OnAudioReceive += MyRoom_OnAudioReceive;
            //  Hook Events - Audio
            app.audioControl.ClearEvents();
            ///app.audioControl.OnSendAudio += AudioControl_OnSendAudio;
            AudioControl.OnException += AudioControl_OnException;
            
            //  Debug Screen
            Debug("HostPage OnAppearing");
            Switch_Debug(false);
        }

        /// 收音(from guest) -> 播出
        private void MyRoom_OnAudioReceive(object sender, UdpMessageEventArgs e)
        {
            //  Convert Bytes To Data
            short[] Sarray;
            int num = 0, Snum = 0;
            Sarray = new short[(e.Data.Length - 4) / 2];
            byte[] B32 = new byte[4] { e.Data[0], e.Data[1], e.Data[2], e.Data[3] };
            int result = BitConverter.ToInt32(B32, 0);
            num = 4;
            while (num < e.Data.Length)
            {
                byte[] B16 = new byte[2];
                B16[0] = e.Data[num];
                num++;
                B16[1] = e.Data[num];
                num++;
                Sarray[Snum] = BitConverter.ToInt16(B16, 0);
                Snum++;
            }
            app.audioControl.AudioWrite(Sarray, result);
        }

        private void UpdateList()
        {
            //  update list
            Device.BeginInvokeOnMainThread(() =>
            {
                ianGuest tempGuest;
                Guests.Clear();
                foreach (string guest in app.user.RoomConfig.UserList)
                {
                    tempGuest = new ianGuest(guest);
                    if (guest == app.user.Config.Name)
                        tempGuest.BeHost();
                    if (app.user.RoomConfig.AskerList.Contains(guest))
                        tempGuest.BeRequested();
                    if (guest == app.user.RoomConfig.Speaker.Trim())
                        tempGuest.BeSpeaker();
                    Guests.Add(tempGuest);
                }
            });
        }
        private void UpdateButton()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                //  UR Speaking
                if (app.user.RoomConfig.Speaker.Trim() == app.user.Config.Name)
                {
                    //  Show "Give Mic to the Requsted Guest"
                    if (app.user.RoomConfig.AskerList.Length != 0)
                    {
                        Action_Button.Text = $"[{app.user.RoomConfig.AskerList[0]}]";
                        Action_Button.BackgroundColor = Color.FromHex("#88FFA922");
                        Action_BoxView_L.BackgroundColor = Color.FromHex("#44FF44");
                        Action_BoxView_R.BackgroundColor = Color.FromHex("#44FF44");
                        Action_Button.IsEnabled = true;
                    }
                    //  Show "UR speaking"
                    else
                    {
                        Action_Button.Text = "發言中...";
                        Action_Button.BackgroundColor = Color.FromHex("#88FFA922");
                        Action_BoxView_L.BackgroundColor = Color.FromHex("#FFA500");
                        Action_BoxView_R.BackgroundColor = Color.FromHex("#FFA500");
                        Action_Button.IsEnabled = true;
                    }
                }
                //  Show "Click to retrieve Mic"
                else
                {
                    Action_Button.Text = "取回發言權";
                    Action_Button.BackgroundColor = Color.FromHex("#555555");
                    Action_BoxView_L.BackgroundColor = Color.FromHex("#808080");
                    Action_BoxView_R.BackgroundColor = Color.FromHex("#808080");
                    Action_Button.IsEnabled = true;
                }


                ////  Guest Speaking
                //if (!app.user.Config.HaveMic)
                //{
                //    Action_Button.Text = "取回發言權";
                //    Action_Button.BackgroundColor = Color.FromHex("#555555");
                //    Action_BoxView_L.BackgroundColor = Color.FromHex("#808080");
                //    Action_BoxView_R.BackgroundColor = Color.FromHex("#808080");
                //    Action_Button.IsEnabled = true;
                //}
                ////  Guest Requested
                //else if (app.user.RoomConfig.AskerList.Length != 0)
                //{
                //    Action_Button.Text = $"[{app.user.RoomConfig.AskerList[0]}]";
                //    Action_Button.BackgroundColor = Color.FromHex("#88FFA922");
                //    Action_BoxView_L.BackgroundColor = Color.FromHex("#44FF44");
                //    Action_BoxView_R.BackgroundColor = Color.FromHex("#44FF44");
                //    Action_Button.IsEnabled = true;
                //}
                ////  No Guest Requested
                //else
                //{
                //    Action_Button.Text = "發言中...";
                //    Action_Button.BackgroundColor = Color.FromHex("#88FFA922");
                //    Action_BoxView_L.BackgroundColor = Color.FromHex("#FFA500");
                //    Action_BoxView_R.BackgroundColor = Color.FromHex("#FFA500");
                //    Action_Button.IsEnabled = true;
                //}
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
        private void User_OnUserExit(object sender, UserEventArgs e)
        {
            lock (UI_Lock)
            {
                UpdateList();
                UpdateButton();
            }
            app.user.UpdateUserList();
            Debug($"user [{e.Name}] exited.");
        }
        private void User_OnUserJoin(object sender, UserEventArgs e)
        {
            lock (UI_Lock)
            {
                UpdateList();
                UpdateButton();
            }
            app.user.UpdateUserList();
            Debug($"user [{e.Name}] joined.");
        }
        private void User_OnMicMissing(object sender, EventArgs e)
        {
            lock (UI_Lock)
            {
                UpdateList();
                UpdateButton();
            }
            app.user.UpdateUserList();
            /// Stop record audio
            Debug("Stop recording...");
            if (app.audioControl.isRecording)
                app.audioControl.StopRecord();
        }
        private void User_OnMicCapture(object sender, EventArgs e)
        {
            lock (UI_Lock)
            {
                UpdateList();
                UpdateButton();
            }
            app.user.UpdateUserList();
            /// Start record audio
            Debug("Start recording...");
            if (app.audioControl.isRecording)
                app.audioControl.StopRecord();
            app.audioControl.StartRecord();
        }
        private void User_OnSpeakerChanged(object sender, UserEventArgs e)
        {
            lock (UI_Lock)
            {
                UpdateList();
                UpdateButton();
            }
            app.user.UpdateUserList();
        }
        private void User_OnRequest(object sender, UserEventArgs e)
        {
            lock (UI_Lock)
            {
                UpdateList();
                UpdateButton();
            }
            app.user.UpdateUserList();
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
            Debug("Error: Host should not send out audio (call ian)");
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
                //  Update Attendants list
                ianAttendant tempAttendant;
                Attendants.Clear();
                foreach (Attendant att in AttendenceSheet.signedList)
                {
                    tempAttendant = new ianAttendant(att.name);
                    tempAttendant.Signed();
                    Attendants.Add(tempAttendant);
                }

                //  Show attendance window
                Attendance_Layout.IsVisible = true;
            });
        }
        //  Attendance - Dismiss
        private void Attendance_Dismiss_Clicked(object sender, EventArgs e)
        {
            //  Close window
            Device.BeginInvokeOnMainThread(() =>
            {
                Attendance_Layout.IsVisible = false;
            });
        }
        
        private void Door_Clicked(object sender, EventArgs e)
        {
            if (isDoorOpen)
            {
                //  close door (stop broadcast)
                isDoorOpen = false;
                Device.BeginInvokeOnMainThread(() => Door_BoxView.BackgroundColor = Color.FromHex("FF4444"));
                app.myRoom.StopBroadcast();
            }
            else
            {
                //  open door (start broadcast)
                isDoorOpen = true;
                Device.BeginInvokeOnMainThread(() => Door_BoxView.BackgroundColor = Color.FromHex("44FF44"));
                app.myRoom.StartBroadcast(10, TimeUnit.Minute);
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
                /// Audio - StopTrack
                Debug("(StopTrack)");
                if (app.audioControl.isTracking)
                    app.audioControl.StopTrack();
            }
            else
            {
                //  open mic
                isSoundOn = true;
                Device.BeginInvokeOnMainThread(() => Sound_BoxView.BackgroundColor = Color.FromHex("44FF44"));
                app.myRoom.OpenMic();
                /// Audio - StartTrack
                Debug("(StartTrack)");
                if (!(app.audioControl.isTracking))
                    app.audioControl.StartTrack();
            }
        }
        private void Exit_Clicked(object sender, EventArgs e)
        {
            //  close room
            Do_Update_WiFi = false;
            app.myRoom.Close();
            //  Exit page
            Navigation.PopModalAsync();
        }
        private void Action_Clicked(object sender, EventArgs e)
        {
            //  U R Speaking
            if(app.user.RoomConfig.Speaker.Trim() == app.user.Config.Name)
            {
                //  Give Mic to the Requsted Guest
                if(app.user.RoomConfig.AskerList.Length != 0)
                {
                    //  Give Mic to the First name in the AskList
                    app.user.AcceptAsker(app.user.RoomConfig.AskerList[0]);
                }
                /// Drop Mic
                else
                {
                    Debug("No guest Requested");
                }
            }
            //  Others or Nobody Speaking
            else
            {
                //  Take Mic
                app.user.WantMic();
                Debug("(action-take mic)");
                /// 直接修改 UI info source...
                lock(UI_Lock)
                {
                    app.user.RoomConfig.Speaker = app.user.Config.Name.Trim();
                }
            }
            
            lock(UI_Lock)
            {
                UpdateButton();
                UpdateList();
            }
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

    public class ianGuest : BindableObject
    {
        public string Name { get; set; }
        public string Color { get; set; }
        public string SideColor { get; set; }
        public string Icon { get; set; }
        public ianGuest(string name)
        {
            Name = name;
            Icon = "";
            SideColor = "#707070";
            Color = "#444444";
        }
        public void BeSpeaker()
        {
            SideColor = "#FFA500";
            Color = "#88FFA922";
            Icon = "\uf0a1";
        }
        public void BeHost()
        {
            Icon = "\uf521";
        }
        public void BeRequested()
        {
            SideColor = "#FFA500";
            Icon = "\uf256";
        }
    }
    public class ianAttendant : BindableObject
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public ianAttendant(string name)
        {
            Name = name;
            Status = "";
        }
        public void Signed()
        {
            Status = "已簽到";
        }
        public void UnSigned()
        {
            Status = "";
        }
    }
}
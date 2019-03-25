using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Controller.Component;

//  [SOLID]
//  Crown:  521 *
//  Hand:   256 *
//  info:   129
//  bell:   0f3
//  volume: 028
//  bullhorn:   0a1 *
//  comment:    075
//  lightbulb:  0eb
//  comment-alt:    27a
//  exclamation-circle: 06a

namespace MeetingHelper
{
	public partial class MainPage : ContentPage
	{
        App app = Application.Current as App;
        
        public MainPage()
		{
			InitializeComponent();
            
            /// Test fa
            //Label2.FontFamily = "fa-solid-900.ttf#Font Awesome 5 Free Regular";
            //Label3.FontFamily = "fa-solid-900.ttf#Font Awesome 5 Free Regular";
            //Label5.FontFamily = "fa-solid-900.ttf#Font Awesome 5 Free Regular";
            //Label2.Text = "\uf521";
            //Label3.Text = "\uf256";
            //Label5.Text = "\uf521 \uf256 \uf129 \uf0f3 \uf028 \uf0a1 \uf075 \uf0eb \uf27a \uf06a";
            ///

            Label_Debug.Text = $"LastSleep: {app.LastSleep}";
            Warning_Layout.IsVisible = false;

            if (app.UserName == "Unnamed")
                NameEntry.Text = "";
            else
                NameEntry.Text = app.UserName;
		}

        //  Enter clicked
        public void OnClicked1(object sender, EventArgs args)
        {
            if (NameEntry.Text.Contains("(") || NameEntry.Text.Contains(")"))
                Label_Debug.Text = "The name can't include '(' or ')'.";
            else if (NameEntry.Text == "")
                Label_Debug.Text = "Username cannot be Unnamed.";
            else if (NameEntry.Text == "Unnamed")
                Label_Debug.Text = "Username cannot be \"Unnamed\" ;)";
            else if (NameEntry.Text.Length > 9)
                Label_Debug.Text = "Name length must less than 10.";
            else
            {
                app.UserName = NameEntry.Text;
                // Next page
                Navigation.PushModalAsync(new SearchRoomPage(), false);
            }
        }

        public void Mode_Clicked(object sender, EventArgs args)
        {
            app.TrackerType = (app.TrackerType + 1) % 4;
            Device.BeginInvokeOnMainThread(() =>
            {
                Mode_Button.Text = Enum.GetName(typeof(TrackerType), app.TrackerType);
            });
        }


        protected override bool OnBackButtonPressed()
        {
            Warning("Quit?", "There's no way out.");
            return true;
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
}

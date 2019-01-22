using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace MeetingHelper
{
	public partial class MainPage : ContentPage
	{
        App app = Application.Current as App;
        
        public MainPage()
		{
			InitializeComponent();

            Label_Debug.Text = $"LastSleep: {app.LastSleep}";

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
            else if (NameEntry.Text.Length > 12)
                Label_Debug.Text = "Name length must less than 12.";
            else
            {
                app.UserName = NameEntry.Text;
                // Next page
                Navigation.PushModalAsync(new SearchRoomPage());
            }
        }

    }
}

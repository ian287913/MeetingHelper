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

            if(app.UserName == "Unnamed")
            {
                Label1.Text = "UserName:";
                NameEntry.Text = "";
            }
            else
            {
                Label1.Text = "Welcome,";
                NameEntry.Text = app.UserName;
            }
		}

        //  Botton1 clicked
        public void OnClicked1(object sender, EventArgs args)
        {
            if (NameEntry.Text.Contains("(") || NameEntry.Text.Contains(")"))
                Label1.Text = "The name can't include '(' or ')'.";
            else if (NameEntry.Text == "")
                Label1.Text = "Username cannot be unnamed.";
            else if (NameEntry.Text == "Unnamed")
                Label1.Text = "Username cannot be \"Unnamed\" ;)";
            else
            {
                app.UserName = NameEntry.Text;
                /// Next page
            }
        }

    }
}

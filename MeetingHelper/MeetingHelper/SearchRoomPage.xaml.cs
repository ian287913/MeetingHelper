using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using System.Collections.ObjectModel;

namespace MeetingHelper
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SearchRoomPage : ContentPage
	{
        ObservableCollection<Room> Rooms;

        public SearchRoomPage ()
		{
			InitializeComponent ();

            //  init Room ItemSource
            Rooms = new ObservableCollection<Room>();
            Rooms.Add(new Room("Alpha Room", "Ian287913", "2019/1/21 19:35"));
            Rooms.Add(new Room("Bravo", "Founder", "2018/1/1 19:08"));
            Rooms.Add(new Room("Charlie", "someone", "1998/12/16 07:32"));

            ListView_Rooms.ItemsSource = Rooms;

            //  click event
            ListView_Rooms.ItemTapped += (sender, e) =>
            {
                //  get tapped room
                Room target = e.Item as Room;
                ((ListView)sender).SelectedItem = null;

                ///  do something
                Warning("ROOM CLICKED!!", $"Name:{target.Name}\nFounder:{target.Founder}");
            };
        }
        
        //  Create Room
        private void OnClicked_Create(object sender, EventArgs e)
        {
            Warning("Create", "Show Create-Room UI...");
        }

        //  Enter Password
        private void Password_Clicked(object sender, EventArgs e)
        {
            if (Password_Entry.Text == "1234")
            {
                Warning("Success", "Entering room\nwaiting...\n123.");
                Password_Label_Wrong.IsVisible = false;
            }
            else
            {
                Password_Label_Wrong.IsVisible = true;
            }
        }


        ///  these method may need to be "exe-by-main-thread"
        private void Warning(string title, string message)
        {
            //  show warning and disable main layout
            Main_Layout.IsEnabled = false;
            Warning_Layout.IsEnabled = true;

            Warning_Title.Text = title;
            Warning_Content.Text = message;

            Warning_Layout.IsVisible = true;
        }
        private void Warning_Clicked(object sender, EventArgs e)
        {
            //  hide warning and enable main layout
            Warning_Layout.IsEnabled = false;
            Warning_Layout.IsVisible = false;
            Main_Layout.IsEnabled = true;
        }

    }

    public class Room : BindableObject
    {
        public string Name { get; set; }
        public string Founder { get; set; }
        public string Found_Time { get; set; }
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
        
        public Room(string name, string founder, string foundTime)
        {
            Name = name;
            Founder = founder;
            Found_Time = foundTime;
            _isSelected = false;
        }
    }
}
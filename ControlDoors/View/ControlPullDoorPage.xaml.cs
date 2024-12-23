using ControlDoors.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ControlDoors.View
{
    /// <summary>
    /// ControlPullDoorPage.xaml 的交互逻辑
    /// </summary>
    public partial class ControlPullDoorPage : UserControl
    {
        public ControlPullDoorPage()
        {
            InitializeComponent();
        }

        private void openPullDoorButton_Click(object sender, RoutedEventArgs e)
        {
            DoorClass door = new DoorClass()
            {
                taskType = 0
            };
            Http http = new Http();
            http.PostJson("http://192.168.20.110:12581/ControlDoors/ControlPullDoor", JsonConvert.SerializeObject(door));
        }

        private void closePullDoorButton_Click(object sender, RoutedEventArgs e)
        {
            DoorClass door = new DoorClass()
            {
                taskType = 1
            };
            Http http = new Http();
            http.PostJson("http://192.168.20.110:12581/ControlDoors/ControlPullDoor", JsonConvert.SerializeObject(door));
        }
    }
}
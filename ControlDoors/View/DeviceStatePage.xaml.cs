using ControlDoors.Common;
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
    /// DeviceStatePage.xaml 的交互逻辑
    /// </summary>
    public partial class DeviceStatePage : UserControl
    {
        public DeviceStatePage()
        {
            InitializeComponent();
        }

        private void ConnectPullDoor_Click(object sender, RoutedEventArgs e)
        {
            ClickRequest request = new ClickRequest();
            request.status = 211;
            if (request.status == 200)
            {
                SolidColorBrush brush1 = new SolidColorBrush();
                SolidColorBrush brush2 = new SolidColorBrush();
                brush1.Color = Colors.LightGreen;
                PullDoorOnlineCircle.Fill = brush1;
                brush2.Color = Colors.Gray;
                PullDoorOfflineCircle.Fill = brush2;
                ConnectPullDoor.Content = "断 开";
            }
            else
            {
                SolidColorBrush brush1 = new SolidColorBrush();
                SolidColorBrush brush2 = new SolidColorBrush();
                brush1.Color = Colors.Gray;
                PullDoorOnlineCircle.Fill = brush1;
                brush2.Color = Colors.Red;
                PullDoorOfflineCircle.Fill = brush2;
                ConnectPullDoor.Content = "连 接";
            }
        }

        private void ConnectShutterDoor_Click(object sender, RoutedEventArgs e)
        {
            ClickRequest request = new ClickRequest();
            if (request.status == 200)
            {
                SolidColorBrush brush1 = new SolidColorBrush();
                SolidColorBrush brush2 = new SolidColorBrush();
                brush1.Color = Colors.LightGreen;
                ShutterDoorOnlineCircle.Fill = brush1;
                brush2.Color = Colors.Gray;
                ShutterDoorOfflineCircle.Fill = brush2;
                ConnectShutterDoor.Content = "断 开";
            }
            else
            {
                SolidColorBrush brush1 = new SolidColorBrush();
                SolidColorBrush brush2 = new SolidColorBrush();
                brush1.Color = Colors.Gray;
                ShutterDoorOnlineCircle.Fill = brush1;
                brush2.Color = Colors.Red;
                ShutterDoorOfflineCircle.Fill = brush2;
                ConnectShutterDoor.Content = "连 接";
            }
        }
    }
}

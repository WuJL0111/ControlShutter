using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ControlDoor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        //private void StartFadeIn_Click(object sender, RoutedEventArgs e)
        //{
        //    // 启动渐入动画
        //    Storyboard fadeInStoryboard = (Storyboard)this.Resources["FadeInStoryboard"];
        //    fadeInStoryboard.Begin();
        //}

        //private void StartFadeOut_Click(object sender, RoutedEventArgs e)
        //{
        //    // 启动渐出动画
        //    Storyboard fadeOutStoryboard = (Storyboard)this.Resources["FadeOutStoryboard"];
        //    fadeOutStoryboard.Begin();
        //}
    }
}
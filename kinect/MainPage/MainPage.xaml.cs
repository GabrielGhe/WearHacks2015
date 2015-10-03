using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Windows.Navigation;
using Kinect2Sample;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MainPage
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void StartApplication_Click(object sender, RoutedEventArgs e)
        {
            NavigationService nav = new NavigationService();
            nav.Navigate(new Uri("/Kinect2Smample;component/MainPage.xaml", UriKind.Relative));
            //Uri test = new Uri("/Kinect2Smample;component/MainPage.xaml", UriKind.RelativeOrAbsolute);
            //NavigationService.Navigate(test);
        }
    }
}

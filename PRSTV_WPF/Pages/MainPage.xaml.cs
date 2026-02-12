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

namespace PRSTV_WPF.Pages
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void CountMenu_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new CountMenu());
        }

        private void CreateElection_Click(object sender, RoutedEventArgs e)
        {
            // NavigationService?.Navigate(new CreateElectionPage());
        }
    }
}

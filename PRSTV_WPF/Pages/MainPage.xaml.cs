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

            // Optional: if you still want the VM for properties later, keep it:
            // DataContext = new PRST_XAML.ViewModels.Pages.HomePageViewModel();
        }

        private void StartNewElection_Click(object sender, RoutedEventArgs e)
        {
            // Replace NewElectionPage with your actual page type
            NavigationService?.Navigate(new NewElectionPage());
        }

        private void ResumeElection_Click(object sender, RoutedEventArgs e)
        {
            // Replace ChooseElectionPage with your actual page type
            NavigationService?.Navigate(new ChooseElectionPage());
        }
    }
}


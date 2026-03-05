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
using PRSTV_WPF_v2.Infrastructure;
using PRSTV_WPF_v2.ViewModel.Pages;

namespace PRSTV_WPF_v2.Pages
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : Page
    {
        public HomePage()
        {
            InitializeComponent();
        }

        private HomePageVM Vm => (HomePageVM)DataContext;

        private void StartNewElection_Click(object sender, RoutedEventArgs e)
            => Vm.NavigateToNewElection(); // this calls _nav internally

        private void ChooseElection_Click(object sender, RoutedEventArgs e)
            => Vm.NavigateToChooseElection();
    }
}

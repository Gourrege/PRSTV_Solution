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
    /// Interaction logic for CountMenu.xaml
    /// </summary>
    public partial class CountMenu : Page
    {

        private BallotPaperService? _ballotService;

        public CountMenu()
        {
            InitializeComponent();
            Loaded += CountMenuPage_Loaded;
        }

        private async void CountMenuPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var client = await SupabaseClientFactory.GetClientAsync();
            _ballotService = new BallotPaperService(client);

            // Example call
            // var ballots = await _ballotService.GetAllAsync();
        }

        private void ContinueCount_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Continue selected election
        }

        private void StartElection_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Navigate to Create Election page
        }

        private void ResolveDoubtful_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ListDoubtful());
        }
    }
}

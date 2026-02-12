using PRSTV_WPF.Models;
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
    /// Interaction logic for ListDoubtful.xaml
    /// </summary>
    public partial class ListDoubtful : Page
    {
        private BallotPaperService _ballotService = null!;
        private List<BallotPaper> _ballots = new();

        public ListDoubtful()
        {
            InitializeComponent();
            Loaded += BallotListPage_Loaded;

        }
        private async void BallotListPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var client = await SupabaseClientFactory.GetClientAsync();
            _ballotService = new BallotPaperService(client);

            _ballots = await _ballotService.GetAllAsync();
            BallotGrid.ItemsSource = _ballots;
        }

        private void BallotGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (BallotGrid.SelectedItem is not BallotPaper selected)
                return;

            // Navigate to detail page
            NavigationService?.Navigate(new EditDoubtful(selected));
        }
    }
}

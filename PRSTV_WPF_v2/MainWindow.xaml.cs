using PRSTV_WPF_v2.Infrastructure;
using PRSTV_WPF_v2.ViewModel;
using PRSTV_WPF_v2.ViewModel.Pages;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PRSTV_WPF_v2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _core;
        private readonly FrameNavigationService _nav;

        public MainWindow()
        {
            InitializeComponent();

            // Core counting VM (existing logic + DB behavior)
            _core = new MainViewModel();

            // Navigation service with factories (keeps pages lightweight)
            _nav = new FrameNavigationService(
                frame: MainFrame,
                homeVm: () => new HomePageVM(_nav),
                newElectionVm: () => new NewElectionPageVM(_nav, _core),
                chooseElectionVm: () => new ChooseElectionPageVM(_nav, _core),
                countingVm: () => new CountingPageVM(_nav, _core),
                adviceVm: () => new AdvicePageVM(_nav, _core),
                surplusVm: () => new SurplusDistributionPageVM(_nav, _core),
                preCountReviewVm: () => new PreCountBallotReviewPageVM(_nav, _core),
                editBallotVm: () => new EditBallotPageVM(_nav, _core),
                resumeHubVm: () => new ElectionResumeHubPageVM(_nav, _core),
                stateOfPollVm: () => new StateOfPollReportPageVM(_nav, _core),
                countResultVm: () => new CountResultReportPageVM(_nav, _core)
                );

            // Start on Home
            _nav.Navigate(AppPage.Home);
        }
    }
}
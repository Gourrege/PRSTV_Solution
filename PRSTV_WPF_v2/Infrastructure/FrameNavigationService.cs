using PRSTV_WPF_v2.Pages;
using PRSTV_WPF_v2.ViewModel.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PRSTV_WPF_v2.Infrastructure
{
    public class FrameNavigationService
    {
        private readonly Frame _frame;
        private readonly Dictionary<AppPage, Func<Page>> _pageFactories;

        public FrameNavigationService(
            Frame frame,
            Func<object> homeVm,
            Func<object> newElectionVm,
            Func<object> chooseElectionVm,
            Func<object> countingVm,
            Func<object> adviceVm,
            Func<object> surplusVm,
            Func<object> preCountReviewVm,
            Func<object> editBallotVm,
            Func<object> resumeHubVm,
            Func<object> stateOfPollVm,
            Func<object> countResultVm)
        {
            _frame = frame;

            // Map AppPage => "build a Page + VM"
            _pageFactories = new Dictionary<AppPage, Func<Page>>
            {
                [AppPage.Home] = () => new HomePage { DataContext = homeVm() },
                [AppPage.NewElection] = () => new NewElectionPage { DataContext = newElectionVm() },
                [AppPage.ChooseElection] = () => new ChooseElectionPage { DataContext = chooseElectionVm() },
                [AppPage.Counting] = () => new CountingPage { DataContext = countingVm() },
                [AppPage.Advice] = () => new AdvicePage { DataContext = adviceVm() },
                [AppPage.SurplusDistribution] = () => new SurplusDistributionPage { DataContext = surplusVm() },
                [AppPage.PreCountReview] = () => new PreCountBallotReviewPage { DataContext = preCountReviewVm() },
                [AppPage.EditBallot] = () => new EditBallotPage { DataContext = editBallotVm() },
                [AppPage.ResumeHub] = () => new ElectionResumeHubPage { DataContext = resumeHubVm() },
                [AppPage.StateOfPoll] = () => new StateOfPollReportPage { DataContext = stateOfPollVm() },
                [AppPage.CountResult] = () => new CountingResultReportPage { DataContext = countResultVm() }
            };
        }

        public void Navigate(AppPage page)
        {
            if (!_pageFactories.TryGetValue(page, out var factory))
                throw new InvalidOperationException($"No factory registered for {page}");

            _frame.Navigate(factory());
        }

        public void GoBack()
        {
            if (_frame.CanGoBack)
                _frame.GoBack();
        }
    }
}

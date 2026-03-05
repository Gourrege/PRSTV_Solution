using PRSTV_WPF_v2.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRSTV_WPF_v2.ViewModel.Pages
{
    class HomePageVM
    {
        private readonly FrameNavigationService _nav;

        public HomePageVM(FrameNavigationService nav)
        {
            _nav = nav;
        }

        public void NavigateToNewElection() => _nav.Navigate(AppPage.NewElection);
        public void NavigateToChooseElection() => _nav.Navigate(AppPage.ChooseElection);
    }
}

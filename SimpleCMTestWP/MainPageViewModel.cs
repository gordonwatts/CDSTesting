using Caliburn.Micro;

namespace SimpleCMTestWP
{
    /// <summary>
    /// simple main page
    /// </summary>
    public class MainPageViewModel : Screen
    {
        public INavigationService _nav;
        public MainPageViewModel(INavigationService nav)
        {
            _nav = nav;
        }

        public void SecondPage()
        {
            _nav.UriFor<SecondPageViewModel>()
                .Navigate();
        }
    }
}
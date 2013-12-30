using Caliburn.Micro;

namespace SimpleCMTestWP
{
    public class SecondPageViewModel : PropertyChangedBase
    {
        private readonly INavigationService _nav;

        public SecondPageViewModel(INavigationService nav)
        {
            _nav = nav;
        }

        public void FirstPage()
        {
            _nav.UriFor<MainPageViewModel>()
                .Navigate();
        }
    }
}

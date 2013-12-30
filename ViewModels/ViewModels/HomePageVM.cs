using ReactiveUI;

namespace ViewModels.ViewModels
{
    /// <summary>
    /// The home page in this test does nothing but have a menu item we can use to switch pages.
    /// </summary>
    public class HomePageVM : ReactiveObject, IRoutableViewModel
    {
        public HomePageVM(IScreen parent)
        {
            HostScreen = parent;
        }

        public IScreen HostScreen { get; private set; }

        public string UrlPathSegment
        {
            get { return "Home"; }
        }
    }
}

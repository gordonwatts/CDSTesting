using Caliburn.Micro;
using Caliburn.Micro.Portable;

namespace ViewModels.ViewModels
{
    /// <summary>
    /// The home page, with a simple button.
    /// </summary>
    public class HomeCMViewModel : Screen
    {
        /// <summary>
        /// Cache for moving around later on.
        /// </summary>
        private readonly INavService _nav;

        /// <summary>
        /// Setup.
        /// </summary>
        /// <param name="nav"></param>
        public HomeCMViewModel(INavService nav)
        {
            _nav = nav;
        }

        public void CmdAdd()
        {
            _nav.NavigateToViewModel<AddCMViewModel>();
        }
    }
}

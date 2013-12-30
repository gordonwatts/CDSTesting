using MVVMTestWS.Views;
using ReactiveUI;
using ViewModels.ViewModels;
using ViewModels.Web;

namespace MVVMTestWS
{
    public class AppBootstrapper : ReactiveObject, IScreen
    {
        public IRoutingState Router { get; private set; }

        public AppBootstrapper(IMutableDependencyResolver dependencyResolver = null, IRoutingState testRouter = null)
        {
            Router = testRouter ?? new RoutingState();
            dependencyResolver = dependencyResolver ?? RxApp.MutableResolver;

            // Bind 
            RegisterParts(dependencyResolver);

            // TODO: This is a good place to set up any other app 
            // startup tasks, like setting the logging level
            LogHost.Default.Level = LogLevel.Debug;

            // Navigate to the opening page of the application
            Router.Navigate.Execute(new HomePageVM(this));
        }

        /// <summary>
        /// The actual CDS searcher.
        /// </summary>
        ICDSSearch gSearcher = new RealCDSSearch();

        private void RegisterParts(IMutableDependencyResolver dependencyResolver)
        {
            dependencyResolver.RegisterConstant(this, typeof(IScreen));

            dependencyResolver.Register(() => new HomePageView(), typeof(IViewFor<HomePageVM>));
            dependencyResolver.Register(() => new AddCDSView(), typeof(IViewFor<AddPageVM>));
        }
    }
}

using Caliburn.Micro;
using System;
using System.Collections.Generic;
using ViewModels.ViewModels;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml.Controls;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227

namespace CMWSAddCDSDoc
{
    public sealed partial class App
    {
        private WinRTContainer container;
        private INavigationService navigationService;

        public App()
        {
            InitializeComponent();
        }

        protected override void Configure()
        {
            LogManager.GetLog = t => new DebugLog(t);

            container = new WinRTContainer();
            container.RegisterWinRTServices();

            container
                .PerRequest<HomeCMViewModel>();

            // We want to use the Frame in OnLaunched so set it up here
            PrepareViewFirst();

            // We are crossing projects for views and view models. Set that up for the
            // type resolver.
            ViewLocator.AddNamespaceMapping("ViewModels.ViewModels", "CMWSAddCDSDoc.Views");
        }

        protected override object GetInstance(Type service, string key)
        {
            var instance = container.GetInstance(service, key);
            if (instance != null)
                return instance;

            throw new Exception("Could not locate any instances.");
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance)
        {
            container.BuildUp(instance);
        }

        /// <summary>
        /// Setup first view, remember the PCL version of the nav service.
        /// </summary>
        /// <param name="rootFrame"></param>
        protected override void PrepareViewFirst(Frame rootFrame)
        {
            navigationService = container.RegisterNavigationService(rootFrame);
            Caliburn.Micro.Portable.WS.NavService.RegisterINavService(navigationService, container);
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Initialize();

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            var resumed = false;

            if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
            {
                resumed = navigationService.ResumeState();
            }

            if (!resumed)
                DisplayRootViewFor<HomeCMViewModel>();
        }
    }
}

using Caliburn.Micro;
using System;
using System.Collections.Generic;
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

#if false
            container
                .PerRequest<ReactiveViewModel>()
                .PerRequest<MyViewDudeViewModel>();
#endif
            // We want to use the Frame in OnLaunched so set it up here

            PrepareViewFirst();
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

        protected override void PrepareViewFirst(Frame rootFrame)
        {
            navigationService = container.RegisterNavigationService(rootFrame);
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

#if false
            if (!resumed)
                DisplayRootView<ReactiveView>();
#endif
        }
    }
}

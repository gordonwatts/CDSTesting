namespace CMWPAddCDSDoc
{
    using Caliburn.Micro;
    using Microsoft.Phone.Controls;
    using System;
    using System.Collections.Generic;
    using System.Windows.Controls;
    using ViewModels.ViewModels;
    using ViewModels.Web;

    public class AppBootstrapper : PhoneBootstrapperBase
    {
        PhoneContainer container;

        public AppBootstrapper()
        {
            Start();
        }

        protected override void Configure()
        {
            LogManager.GetLog = t => new DebugLog(t);
            container = new PhoneContainer();
            if (!Execute.InDesignMode)
                container.RegisterPhoneServices(RootFrame);

            container
                .PerRequest<HomeCMViewModel>()
                .PerRequest<AddCMViewModel>();

            // Some of our global services
            container.RegisterInstance(typeof(ICDSSearch), null, new RealCDSSearch());

            // Register our custom navigation service.
            var nav = container.GetInstance<INavigationService>();
            Caliburn.Micro.Portable.WP8.NavService.RegisterINavService(container);

            // We are crossing projects for views and view models. Set that up for the
            // type resolver. Also make sure that it loads the types we are interested in!
            AssemblySource.Instance.Add(typeof(HomeCMViewModel).Assembly);
            ViewLocator.AddNamespaceMapping("ViewModels.ViewModels", "CMWPAddCDSDoc.Views");
            ViewModelLocator.AddNamespaceMapping("CMWPAddCDSDoc.Views", "ViewModels.ViewModels");

            // And conventions for... whatever.
            AddCustomConventions();
        }

        protected override object GetInstance(Type service, string key)
        {
            var instance = container.GetInstance(service, key);
            if (instance != null)
                return instance;

            throw new InvalidOperationException("Could not locate any instances.");
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance)
        {
            container.BuildUp(instance);
        }

        static void AddCustomConventions()
        {
            ConventionManager.AddElementConvention<Pivot>(Pivot.ItemsSourceProperty, "SelectedItem", "SelectionChanged").ApplyBinding =
                (viewModelType, path, property, element, convention) =>
                {
                    if (ConventionManager
                        .GetElementConvention(typeof(ItemsControl))
                        .ApplyBinding(viewModelType, path, property, element, convention))
                    {
                        ConventionManager
                            .ConfigureSelectedItem(element, Pivot.SelectedItemProperty, viewModelType, path);
                        ConventionManager
                            .ApplyHeaderTemplate(element, Pivot.HeaderTemplateProperty, null, viewModelType);
                        return true;
                    }

                    return false;
                };

            ConventionManager.AddElementConvention<Panorama>(Panorama.ItemsSourceProperty, "SelectedItem", "SelectionChanged").ApplyBinding =
                (viewModelType, path, property, element, convention) =>
                {
                    if (ConventionManager
                        .GetElementConvention(typeof(ItemsControl))
                        .ApplyBinding(viewModelType, path, property, element, convention))
                    {
                        ConventionManager
                            .ConfigureSelectedItem(element, Panorama.SelectedItemProperty, viewModelType, path);
                        ConventionManager
                            .ApplyHeaderTemplate(element, Panorama.HeaderTemplateProperty, null, viewModelType);
                        return true;
                    }

                    return false;
                };
        }
    }
}
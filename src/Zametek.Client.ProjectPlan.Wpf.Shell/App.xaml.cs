using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Prism.Unity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using Xceed.Wpf.AvalonDock;
using Zametek.Access.ProjectPlan;
using Zametek.Contract.ProjectPlan;
using Zametek.Engine.ProjectPlan;
using Zametek.Manager.ProjectPlan;
using Zametek.PrismEx.AvalonDock;

namespace Zametek.Client.ProjectPlan.Wpf.Shell
{
    public partial class App
        : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainView>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<IDateTimeCalculator, DateTimeCalculator>();
            containerRegistry.Register<IFileDialogService, FileDialogService>();
            containerRegistry.Register<IProjectSettingService, ProjectSettingService>();

            containerRegistry.RegisterSingleton<IGraphProcessingEngine, GraphProcessingEngine>();
            containerRegistry.RegisterSingleton<IAssessingEngine, AssessingEngine>();
            containerRegistry.RegisterSingleton<IProjectManager, ProjectManager>();
            containerRegistry.RegisterSingleton<ISettingResourceAccess, SettingResourceAccess>();
            containerRegistry.RegisterSingleton<ISettingManager, SettingManager>();

            containerRegistry.RegisterSingleton<ICoreViewModel, CoreViewModel>();
            containerRegistry.RegisterSingleton<IEarnedValueChartManagerViewModel, EarnedValueChartManagerViewModel>();
            containerRegistry.RegisterSingleton<IResourceChartManagerViewModel, ResourceChartManagerViewModel>();
            containerRegistry.RegisterSingleton<IMetricsManagerViewModel, MetricsManagerViewModel>();
            containerRegistry.RegisterSingleton<IArrowGraphManagerViewModel, ArrowGraphManagerViewModel>();
            containerRegistry.RegisterSingleton<IGanttChartManagerViewModel, GanttChartManagerViewModel>();
            containerRegistry.Register<IActivitiesManagerViewModel, ActivitiesManagerViewModel>();
            containerRegistry.RegisterSingleton<IMainViewModel, MainViewModel>();

            containerRegistry.RegisterSingleton<IApplicationCommands, ApplicationCommands>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<ClientWpfModule>();
        }

        protected override void ConfigureRegionAdapterMappings(RegionAdapterMappings regionAdapterMappings)
        {
            regionAdapterMappings.RegisterMapping(typeof(DockingManager), Container.Resolve<DockingManagerRegionAdapter>());
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Set the display format to the local settings.
            FrameworkElement.LanguageProperty.OverrideMetadata(
              typeof(FrameworkElement),
              new FrameworkPropertyMetadata(
                  XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            Current.MainWindow.Activate();

            // Check if started with filename parameter.
            string[] args = e.Args;
            if (args.Any()
                && File.Exists(args[0])
                && string.CompareOrdinal(Path.GetExtension(args[0]), Wpf.Properties.Resources.Filter_OpenProjectPlanFileExtension) == 0)
            {
                var mainView = Container.Resolve<IMainViewModel>();
                mainView.DoOpenProjectPlanFileAsync(args[0]);
            }
        }
    }
}

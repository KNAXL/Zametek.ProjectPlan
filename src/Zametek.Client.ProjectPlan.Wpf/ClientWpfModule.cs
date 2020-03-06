﻿using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Zametek.Common.ProjectPlan;

namespace Zametek.Client.ProjectPlan.Wpf
{
    public class ClientWpfModule
        : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = containerProvider.Resolve<IRegionManager>();

            {
                IRegion projectPlanActivitiesRegion = regionManager.Regions["ContentRegion"];
                var activitiesManagerView = containerProvider.Resolve<ActivitiesManagerView>();
                projectPlanActivitiesRegion.Add(activitiesManagerView);
            }
            {
                IRegion projectPlanMetricsRegion = regionManager.Regions[RegionNames.ProjectPlanMetricsRegion];
                var metricsManagerView = containerProvider.Resolve<MetricsManagerView>();
                projectPlanMetricsRegion.Add(metricsManagerView);
            }
            {
                IRegion projectPlanGanttChartRegion = regionManager.Regions["ContentRegion"];
                var ganttChartManagerView = containerProvider.Resolve<GanttChartManagerView>();
                projectPlanGanttChartRegion.Add(ganttChartManagerView);
            }

            //regionManager.RegisterViewWithRegion(RegionNames.ProjectPlanActivitiesRegion, typeof(ActivitiesManagerView));
            //regionManager.RegisterViewWithRegion(RegionNames.ProjectPlanMetricsRegion, typeof(MetricsManagerView));
            //regionManager.RegisterViewWithRegion(RegionNames.ProjectPlanGanttChartRegion, typeof(GanttChartManagerView));
            //regionManager.RegisterViewWithRegion(RegionNames.ProjectPlanArrowGraphRegion, typeof(ArrowGraphManagerView));
            //regionManager.RegisterViewWithRegion(RegionNames.ProjectPlanResourceChartRegion, typeof(ResourceChartManagerView));
            //regionManager.RegisterViewWithRegion(RegionNames.ProjectPlanEarnedValueChartRegion, typeof(EarnedValueChartManagerView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }
    }
}

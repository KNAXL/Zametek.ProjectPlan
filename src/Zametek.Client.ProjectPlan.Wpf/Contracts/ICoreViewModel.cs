﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Zametek.Common.Project;
using Zametek.Common.ProjectPlan;
using Zametek.Maths.Graphs;

namespace Zametek.Client.ProjectPlan.Wpf
{
    public interface ICoreViewModel
        : IPropertyChangedPubSubViewModel
    {
        bool IsBusy { get; set; }

        DateTime ProjectStart { get; set; }

        bool IsProjectUpdated { get; set; }

        bool ShowDates { get; set; }

        bool UseBusinessDays { get; set; }

        bool HasStaleOutputs { get; set; }

        bool AutoCompile { get; set; }

        bool HasCompilationErrors { get; set; }

        GraphCompilation<int, IDependentActivity<int>> GraphCompilation { get; set; }

        string CompilationOutput { get; set; }

        Common.Project.v0_1_0.ArrowGraphDto ArrowGraphDto { get; set; }

        ObservableCollection<ManagedActivityViewModel> Activities { get; }

        IList<ResourceSeriesDto> ResourceSeriesSet { get; }

        Common.Project.v0_1_0.ArrowGraphSettingsDto ArrowGraphSettingsDto { get; set; }

        Common.Project.v0_1_0.ResourceSettingsDto ResourceSettingsDto { get; set; }

        int? CyclomaticComplexity { get; set; }

        int? Duration { get; set; }

        double? DirectCost { get; set; }

        double? IndirectCost { get; set; }

        double? OtherCost { get; set; }

        double? TotalCost { get; set; }

        IDependentActivity<int> AddManagedActivity();

        HashSet<IDependentActivity<int>> AddManagedActivities(HashSet<IDependentActivity<int>> dependentActivities);

        HashSet<IDependentActivity<int>> RemoveManagedActivities(HashSet<int> dependentActivityIds);

        void ClearManagedActivities();

        void UpdateActivitiesTargetResources();

        void UpdateActivitiesTargetResourceDependencies();

        void UpdateActivitiesAllocatedToResources();

        void UpdateActivitiesProjectStart();

        void UpdateActivitiesUseBusinessDays();

        int RunCalculateResourcedCyclomaticComplexity();

        void RunCompile();

        void RunAutoCompile();

        void RunTransitiveReduction();

        void SetCompilationOutput();

        void CalculateCosts();

        void ClearCosts();

        void ClearSettings();
    }
}

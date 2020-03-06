﻿using Prism;
using Prism.Regions;
using System;

namespace Zametek.Client.ProjectPlan.Wpf
{
    public partial class ActivitiesManagerView
        : IActiveAware, INavigationAware
    {


        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }



        #region Fields

        private bool m_IsActive;

        #endregion

        #region Ctors

        public ActivitiesManagerView(IActivitiesManagerViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        #endregion

        #region Properties

        public IActivitiesManagerViewModel ViewModel
        {
            get
            {
                return DataContext as IActivitiesManagerViewModel;
            }
            set
            {
                DataContext = value;
            }
        }

        #endregion

        #region IActiveAware Members

        public event EventHandler IsActiveChanged;

        public bool IsActive
        {
            get
            {
                return m_IsActive;
            }
            set
            {
                if (m_IsActive != value)
                {
                    m_IsActive = value;
                    IsActiveChanged?.Invoke(this, new EventArgs());
                }
            }
        }

        #endregion
    }
}

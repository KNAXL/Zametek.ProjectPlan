using Prism;
using Prism.Commands;
using Prism.Events;
using Prism.Interactivity.InteractionRequest;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Zametek.Maths.Graphs;

namespace Zametek.Client.ProjectPlan.Wpf
{
    public class ActivitiesManagerViewModel
        : PropertyChangedPubSubViewModel, IActivitiesManagerViewModel, IActiveAware
    {
        #region Fields

        private readonly object m_Lock;

        private const int c_MaxUndoRedoStackSize = 10;
        private readonly LimitedSizeStack<UndoRedoCommandPair> m_UndoStack;
        private readonly LimitedSizeStack<UndoRedoCommandPair> m_RedoStack;

        private readonly ICoreViewModel m_CoreViewModel;
        private readonly IEventAggregator m_EventService;

        private readonly InteractionRequest<Notification> m_NotificationInteractionRequest;

        private SubscriptionToken m_ManagedActivityUpdatedSubscriptionToken;
        private SubscriptionToken m_ProjectStartUpdatedSubscriptionToken;
        private SubscriptionToken m_UseBusinessDaysUpdatedSubscriptionToken;
        private SubscriptionToken m_ShowDatesUpdatedSubscriptionToken;

        private bool m_IsActive;

        #endregion

        #region Ctors

        public ActivitiesManagerViewModel(
            ICoreViewModel coreViewModel,
            IApplicationCommands applicationCommands,
            IEventAggregator eventService)
            : base(eventService)
        {
            m_Lock = new object();
            m_UndoStack = new LimitedSizeStack<UndoRedoCommandPair>(c_MaxUndoRedoStackSize);
            m_RedoStack = new LimitedSizeStack<UndoRedoCommandPair>(c_MaxUndoRedoStackSize);
            m_CoreViewModel = coreViewModel ?? throw new ArgumentNullException(nameof(coreViewModel));
            ApplicationCommands = applicationCommands ?? throw new ArgumentNullException(nameof(applicationCommands));
            m_EventService = eventService ?? throw new ArgumentNullException(nameof(eventService));

            SelectedActivities = new ObservableCollection<ManagedActivityViewModel>();

            m_NotificationInteractionRequest = new InteractionRequest<Notification>();

            InitializeCommands();
            SubscribeToEvents();

            SubscribePropertyChanged(m_CoreViewModel, nameof(m_CoreViewModel.IsBusy), nameof(IsBusy), ThreadOption.BackgroundThread);
            SubscribePropertyChanged(m_CoreViewModel, nameof(m_CoreViewModel.HasStaleOutputs), nameof(HasStaleOutputs), ThreadOption.BackgroundThread);
            SubscribePropertyChanged(m_CoreViewModel, nameof(m_CoreViewModel.HasCompilationErrors), nameof(HasCompilationErrors), ThreadOption.BackgroundThread);
            SubscribePropertyChanged(m_CoreViewModel, nameof(m_CoreViewModel.CompilationOutput), nameof(CompilationOutput), ThreadOption.BackgroundThread);
            SubscribePropertyChanged(m_CoreViewModel, nameof(m_CoreViewModel.ShowDates), nameof(ShowDates), ThreadOption.BackgroundThread);
            SubscribePropertyChanged(m_CoreViewModel, nameof(m_CoreViewModel.ShowDates), nameof(ShowDays), ThreadOption.BackgroundThread);
        }

        #endregion

        #region Properties

        private bool IsProjectUpdated
        {
            set
            {
                m_CoreViewModel.IsProjectUpdated = value;
            }
        }

        private bool UseBusinessDays => m_CoreViewModel.UseBusinessDays;

        private DateTime ProjectStart => m_CoreViewModel.ProjectStart;

        #endregion

        #region Commands

        public DelegateCommandBase InternalSetSelectedManagedActivitiesCommand
        {
            get;
            private set;
        }

        private void SetSelectedManagedActivities(SelectionChangedEventArgs args)
        {
            if (args?.AddedItems != null)
            {
                SelectedActivities.AddRange(args?.AddedItems.OfType<ManagedActivityViewModel>());
            }
            if (args?.RemovedItems != null)
            {
                foreach (var managedActivityViewModel in args?.RemovedItems.OfType<ManagedActivityViewModel>())
                {
                    SelectedActivities.Remove(managedActivityViewModel);
                }
            }
            RaisePropertyChanged(nameof(SelectedActivity));
            RaiseCanExecuteChangedAllCommands();
        }

        private DelegateCommandBase InternalAddManagedActivityCommand
        {
            get;
            set;
        }

        private async void AddManagedActivity()
        {
            await DoAddManagedActivityAsync();
        }

        private bool CanAddManagedActivity()
        {
            return true;
        }

        private async void AddManagedActivity(IEnumerable<IDependentActivity<int>> dependentActivities)
        {
            await DoAddManagedActivityAsync(dependentActivities);
        }

        private bool CanAddManagedActivity(IEnumerable<IDependentActivity<int>> dependentActivities)
        {
            return !SelectedActivities.Intersect(dependentActivities).Any();
        }

        private DelegateCommandBase InternalRemoveManagedActivityCommand
        {
            get;
            set;
        }

        private async void RemoveManagedActivity()
        {
            await DoRemoveManagedActivityAsync();
        }

        private bool CanRemoveManagedActivity()
        {
            return SelectedActivities.Any();
        }

        private async void RemoveManagedActivity(IEnumerable<IDependentActivity<int>> selectedActivities)
        {
            await DoRemoveManagedActivityAsync(selectedActivities);
        }

        private bool CanRemoveManagedActivity(IEnumerable<IDependentActivity<int>> selectedActivities)
        {
            return selectedActivities?.Any() ?? false;
        }

        private DelegateCommand UndoCommand
        {
            get;
            set;
        }

        private void Undo()
        {
            lock (m_Lock)
            {
                UndoRedoCommandPair undoRedoCommandPair = m_UndoStack.Pop();
                undoRedoCommandPair.UndoCommand.Execute(undoRedoCommandPair.UndoParameter);
            }
        }

        private bool CanUndo()
        {
            return m_UndoStack.Any();
        }

        #endregion

        #region Private Methods

        private void InitializeCommands()
        {
            SetSelectedManagedActivitiesCommand =
                InternalSetSelectedManagedActivitiesCommand =
                    new DelegateCommand<SelectionChangedEventArgs>(SetSelectedManagedActivities);
            AddManagedActivityCommand =
                InternalAddManagedActivityCommand =
                    new DelegateCommand(AddManagedActivity, CanAddManagedActivity);
            RemoveManagedActivityCommand =
                InternalRemoveManagedActivityCommand =
                    new DelegateCommand(RemoveManagedActivity, CanRemoveManagedActivity);
            UndoCommand = new DelegateCommand(Undo, CanUndo);

            ApplicationCommands.UndoCommand.RegisterCommand(UndoCommand);
        }

        private void RaiseCanExecuteChangedAllCommands()
        {
            InternalSetSelectedManagedActivitiesCommand.RaiseCanExecuteChanged();
            InternalAddManagedActivityCommand.RaiseCanExecuteChanged();
            InternalRemoveManagedActivityCommand.RaiseCanExecuteChanged();
            UndoCommand.RaiseCanExecuteChanged();
        }

        private void SubscribeToEvents()
        {
            m_ManagedActivityUpdatedSubscriptionToken =
                m_EventService.GetEvent<PubSubEvent<ManagedActivityUpdatedPayload>>()
                    .Subscribe(async payload =>
                    {
                        IsProjectUpdated = true;
                        await UpdateActivitiesTargetResourceDependenciesAsync();
                        await DoAutoCompileAsync();
                    }, ThreadOption.BackgroundThread);
            m_ProjectStartUpdatedSubscriptionToken =
                m_EventService.GetEvent<PubSubEvent<ProjectStartUpdatedPayload>>()
                    .Subscribe(async payload =>
                    {
                        IsProjectUpdated = true;
                        await UpdateActivitiesProjectStartAsync();
                        await DoAutoCompileAsync();
                    }, ThreadOption.BackgroundThread);
            m_UseBusinessDaysUpdatedSubscriptionToken =
                m_EventService.GetEvent<PubSubEvent<UseBusinessDaysUpdatedPayload>>()
                    .Subscribe(async payload =>
                    {
                        IsProjectUpdated = true;
                        await UpdateActivitiesUseBusinessDaysAsync();
                        await DoAutoCompileAsync();
                    }, ThreadOption.BackgroundThread);
            m_ShowDatesUpdatedSubscriptionToken =
                m_EventService.GetEvent<PubSubEvent<ShowDatesUpdatedPayload>>()
                    .Subscribe(async payload =>
                    {
                        await SetCompilationOutputAsync();
                        PublishGraphCompilationUpdatedPayload();
                    }, ThreadOption.BackgroundThread);
        }

        private void UnsubscribeFromEvents()
        {
            m_EventService.GetEvent<PubSubEvent<ManagedActivityUpdatedPayload>>()
                .Unsubscribe(m_ManagedActivityUpdatedSubscriptionToken);
            m_EventService.GetEvent<PubSubEvent<ProjectStartUpdatedPayload>>()
                .Unsubscribe(m_ProjectStartUpdatedSubscriptionToken);
            m_EventService.GetEvent<PubSubEvent<UseBusinessDaysUpdatedPayload>>()
                .Unsubscribe(m_UseBusinessDaysUpdatedSubscriptionToken);
            m_EventService.GetEvent<PubSubEvent<ShowDatesUpdatedPayload>>()
                .Unsubscribe(m_ShowDatesUpdatedSubscriptionToken);
        }

        private void PublishGraphCompilationUpdatedPayload()
        {
            m_EventService.GetEvent<PubSubEvent<GraphCompilationUpdatedPayload>>()
                .Publish(new GraphCompilationUpdatedPayload());
        }

        private async Task UpdateActivitiesTargetResourceDependenciesAsync()
        {
            await Task.Run(() => m_CoreViewModel.UpdateActivitiesTargetResourceDependencies());
        }

        private async Task UpdateActivitiesProjectStartAsync()
        {
            await Task.Run(() => m_CoreViewModel.UpdateActivitiesProjectStart());
        }

        private async Task UpdateActivitiesUseBusinessDaysAsync()
        {
            await Task.Run(() => m_CoreViewModel.UpdateActivitiesUseBusinessDays());
        }

        private async Task RunAutoCompileAsync()
        {
            await Task.Run(() => m_CoreViewModel.RunAutoCompile());
        }

        private async Task SetCompilationOutputAsync()
        {
            await Task.Run(() => m_CoreViewModel.SetCompilationOutput());
        }

        private void DispatchNotification(string title, object content)
        {
            m_NotificationInteractionRequest.Raise(
                new Notification
                {
                    Title = title,
                    Content = content
                });
        }

        #endregion

        #region Public Methods

        public async Task DoAutoCompileAsync()
        {
            try
            {
                IsBusy = true;
                HasStaleOutputs = true;
                IsProjectUpdated = true;
                await RunAutoCompileAsync();
            }
            catch (Exception ex)
            {
                DispatchNotification(
                    Properties.Resources.Title_Error,
                    ex.Message);
            }
            finally
            {
                IsBusy = false;
                RaiseCanExecuteChangedAllCommands();
            }
        }

        public async Task DoAddManagedActivityAsync()
        {
            try
            {
                IsBusy = true;

                lock (m_Lock)
                {
                    m_CoreViewModel.AddManagedActivity();
                }

                HasStaleOutputs = true;
                IsProjectUpdated = true;

                await RunAutoCompileAsync();
            }
            catch (Exception ex)
            {
                DispatchNotification(
                    Properties.Resources.Title_Error,
                    ex.Message);
            }
            finally
            {
                IsBusy = false;
                RaiseCanExecuteChangedAllCommands();
            }
        }

        public async Task DoAddManagedActivityAsync(IEnumerable<IDependentActivity<int>> selectedActivities)
        {
            if (selectedActivities == null)
            {
                throw new ArgumentNullException(nameof(selectedActivities));
            }
            try
            {
                IsBusy = true;

                lock (m_Lock)
                {
                    IEnumerable<IDependentActivity<int>> dependentActivities = selectedActivities.ToList();
                    m_CoreViewModel.AddManagedActivities(new HashSet<IDependentActivity<int>>(dependentActivities));
                }

                HasStaleOutputs = true;
                IsProjectUpdated = true;

                await RunAutoCompileAsync();
            }
            catch (Exception ex)
            {
                DispatchNotification(
                    Properties.Resources.Title_Error,
                    ex.Message);
            }
            finally
            {
                IsBusy = false;
                RaiseCanExecuteChangedAllCommands();
            }
        }

        public async Task DoRemoveManagedActivityAsync()
        {
            try
            {
                IsBusy = true;

                lock (m_Lock)
                {
                    IEnumerable<IDependentActivity<int>> dependentActivities = SelectedActivities.ToList();
                    var activityIds = new HashSet<int>(dependentActivities.Select(x => x.Id));
                    if (!activityIds.Any())
                    {
                        return;
                    }
                    m_CoreViewModel.RemoveManagedActivities(activityIds);
                    m_UndoStack.Push(new UndoRedoCommandPair(
                        new DelegateCommand<IEnumerable<IDependentActivity<int>>>(AddManagedActivity, CanAddManagedActivity),
                        dependentActivities,
                        new DelegateCommand<IEnumerable<IDependentActivity<int>>>(RemoveManagedActivity, CanRemoveManagedActivity),
                        dependentActivities));
                }

                HasStaleOutputs = true;
                IsProjectUpdated = true;

                await RunAutoCompileAsync();
            }
            catch (Exception ex)
            {
                DispatchNotification(
                    Properties.Resources.Title_Error,
                    ex.Message);
            }
            finally
            {
                SelectedActivities.Clear();
                RaisePropertyChanged(nameof(Activities));
                RaisePropertyChanged(nameof(SelectedActivities));
                IsBusy = false;
                RaiseCanExecuteChangedAllCommands();
            }
        }

        public async Task DoRemoveManagedActivityAsync(IEnumerable<IDependentActivity<int>> selectedActivities)
        {
            if (selectedActivities == null)
            {
                throw new ArgumentNullException(nameof(selectedActivities));
            }
            try
            {
                IsBusy = true;

                lock (m_Lock)
                {
                    IEnumerable<IDependentActivity<int>> dependentActivities = selectedActivities.ToList();
                    var activityIds = new HashSet<int>(dependentActivities.Select(x => x.Id));
                    if (!activityIds.Any())
                    {
                        return;
                    }
                    m_CoreViewModel.RemoveManagedActivities(activityIds);
                }

                HasStaleOutputs = true;
                IsProjectUpdated = true;

                await RunAutoCompileAsync();
            }
            catch (Exception ex)
            {
                DispatchNotification(
                    Properties.Resources.Title_Error,
                    ex.Message);
            }
            finally
            {
                SelectedActivities.Clear();
                RaisePropertyChanged(nameof(Activities));
                RaisePropertyChanged(nameof(SelectedActivities));
                IsBusy = false;
                RaiseCanExecuteChangedAllCommands();
            }
        }

        #endregion

        #region IActivityManagerViewModel Members

        public IInteractionRequest NotificationInteractionRequest => m_NotificationInteractionRequest;

        public bool IsBusy
        {
            get
            {
                return m_CoreViewModel.IsBusy;
            }
            private set
            {
                lock (m_Lock)
                {
                    m_CoreViewModel.IsBusy = value;
                }
                RaisePropertyChanged();
            }
        }

        public bool HasStaleOutputs
        {
            get
            {
                return m_CoreViewModel.HasStaleOutputs;
            }
            private set
            {
                lock (m_Lock)
                {
                    m_CoreViewModel.HasStaleOutputs = value;
                }
                RaisePropertyChanged();
            }
        }

        public bool ShowDates
        {
            get
            {
                return m_CoreViewModel.ShowDates;
            }
        }

        public bool ShowDays
        {
            get
            {
                return !ShowDates;
            }
        }

        public bool HasCompilationErrors
        {
            get
            {
                return m_CoreViewModel.HasCompilationErrors;
            }
            private set
            {
                lock (m_Lock)
                {
                    m_CoreViewModel.HasCompilationErrors = value;
                }
                RaisePropertyChanged();
            }
        }

        public string CompilationOutput
        {
            get
            {
                return m_CoreViewModel.CompilationOutput;
            }
        }

        public ObservableCollection<ManagedActivityViewModel> Activities => m_CoreViewModel.Activities;

        public ObservableCollection<ManagedActivityViewModel> SelectedActivities
        {
            get;
        }

        public ManagedActivityViewModel SelectedActivity
        {
            get
            {
                if (SelectedActivities.Count == 1)
                {
                    return SelectedActivities.FirstOrDefault();
                }
                return null;
            }
        }

        public IApplicationCommands ApplicationCommands
        {
            get;
        }

        public ICommand SetSelectedManagedActivitiesCommand
        {
            get;
            private set;
        }

        public ICommand AddManagedActivityCommand
        {
            get;
            private set;
        }

        public ICommand RemoveManagedActivityCommand
        {
            get;
            private set;
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

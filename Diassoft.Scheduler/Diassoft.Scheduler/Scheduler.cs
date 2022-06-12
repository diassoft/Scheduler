using Diassoft.Scheduler.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Diassoft.Scheduler
{
    /// <summary>
    /// A class that defines a scheduler for events.
    /// </summary>
    /// <typeparam name="T">The type of event to be triggered</typeparam>
    public class Scheduler<T>
    {
        #region Constants
        
        /// <summary>
        /// The default description of an exception
        /// </summary>
        protected const string DEFAULT_EXCEPTION_MESSAGE = "An error has occurred";
        /// <summary>
        /// The date format for the event key
        /// </summary>
        protected const string EVENT_KEY_DATE_FORMAT = "yyyyMMddHHmmss";
        /// <summary>
        /// The default date format to be used for display purposes
        /// </summary>
        protected const string DISPLAY_DATE_FORMAT = "yyyy-MM-dd HH:mm:ss";
        /// <summary>
        /// The number of days to build the schedule for. 
        /// </summary>
        /// <remarks>Do not use zero or too many. Usually 1 or 2 is enough.</remarks>
        protected const int SCHEDULE_BUILD_DAYS = 1;
        /// <summary>
        /// The time the service will wait to check the schedule again (in milliseconds)
        /// </summary>
        /// <remarks>Be careful with this configuration</remarks>
        public const int WAITING_TIME_CHECKING = 5000;
        /// <summary>
        /// The maximum time any method would wait for completion (in milliseconds)
        /// </summary>
        public const int WAIT_FOR_COMPLETION_TIMEOUT = 60000;

        // Internal Event Names
        /// <summary>
        /// An internal event called to tell the system to recalculate the schedule
        /// </summary>
        protected const string INTERNAL_EVENT_CALCULATE_SCHEDULE = "$CALCULATESCHEDULE";

        #endregion Constants

        #region Properties

        /// <summary>
        /// The Logger to be used for tracing or error logging
        /// </summary>
        protected readonly ILogger Logger = null;

        /// <summary>
        /// A lock to be used when interacting with objects that require exclusive access
        /// </summary>
        /// <remarks>The following objects require exclusive access:
        /// <list type="bullet">
        /// <item><see cref="EventMaster"/></item>
        /// </list></remarks>
        protected readonly object SchedulerLock = new object();

        /// <summary>
        /// A dictionary containing the master data for the event, keyed by its name
        /// </summary>
        protected Dictionary<string, ScheduledEventInfo> EventMaster = new Dictionary<string, ScheduledEventInfo>();
        /// <summary>
        /// A sorted list keyed by the date and time of the next execution, containing the name of the event to be triggered
        /// </summary>
        protected SortedList<string, ScheduledEventExecutionInfo> ScheduledEvents = new SortedList<string, ScheduledEventExecutionInfo>();
        /// <summary>
        /// Defines whether the events will be triggered asynchronously or synchronously
        /// </summary>
        /// <remarks>By default, all events should be triggered on its own Task. It is not recommended to trigger events synchronously.</remarks>
        public bool TriggerEventsAsynchronously { get; set; } = true;

        #endregion Properties

        #region Events

        /// <summary>
        /// An event triggered when the scheduler service starts
        /// </summary>
        public event EventHandler<EventArgs> SchedulerStarted;
        /// <summary>
        /// Method called prior to raising the <see cref="SchedulerStarted"/> event
        /// </summary>
        /// <param name="e"></param>
        /// <remarks>This method should change the status</remarks>
        protected virtual void OnSchedulerStarted(EventArgs e)
        {
            // Logging
            Logger?.LogTrace($"[{nameof(OnSchedulerStarted)}] Entering function...");

            Status = SchedulerStatus.Active;
            SchedulerStarted?.Invoke(this, e);

            // Logging
            Logger?.LogTrace($"[{nameof(OnSchedulerStarted)}] Exiting function...");
        }

        /// <summary>
        /// An event triggered when the scheduler service is pausing
        /// </summary>
        public event EventHandler<EventArgs> SchedulerPausing;
        /// <summary>
        /// Method called prior to raising the <see cref="SchedulerPausing"/> event
        /// </summary>
        /// <param name="e"></param>
        /// <remarks>This method should change the status</remarks>
        protected virtual void OnSchedulerPausing(EventArgs e)
        {
            // Logging
            Logger?.LogTrace($"[{nameof(OnSchedulerPausing)}] Entering function...");

            Status = SchedulerStatus.Pausing;
            SchedulerPausing?.Invoke(this, e);

            // Logging
            Logger?.LogTrace($"[{nameof(OnSchedulerPausing)}] Exiting function...");
        }
        /// <summary>
        /// An event triggered when the scheduler service is paused
        /// </summary>
        public event EventHandler<EventArgs> SchedulerPaused;
        /// <summary>
        /// Method called prior to raising the <see cref="SchedulerPaused"/> event
        /// </summary>
        /// <param name="e"></param>
        /// <remarks>This method should change the status</remarks>
        protected virtual void OnSchedulerPaused(EventArgs e)
        {
            // Logging
            Logger?.LogTrace($"[{nameof(OnSchedulerPaused)}] Entering function...");

            Status = SchedulerStatus.Paused;
            SchedulerPaused?.Invoke(this, e);

            // Logging
            Logger?.LogTrace($"[{nameof(OnSchedulerPaused)}] Exiting function...");
        }
        /// <summary>
        /// An event triggered when the scheduler service is resuming from a paused state
        /// </summary>
        public event EventHandler<EventArgs> SchedulerResuming;
        /// <summary>
        /// Method called prior to raising the <see cref="SchedulerPaused"/> event
        /// </summary>
        /// <param name="e"></param>
        /// <remarks>This method should change the status</remarks>
        protected virtual void OnSchedulerResuming(EventArgs e)
        {
            // Logging
            Logger?.LogTrace($"[{nameof(OnSchedulerResuming)}] Entering function...");

            Status = SchedulerStatus.Resuming;
            SchedulerResuming?.Invoke(this, e);

            // Logging
            Logger?.LogTrace($"[{nameof(OnSchedulerResuming)}] Exiting function...");
        }
        /// <summary>
        /// An event triggered when the scheduler service is resumed after a paused state
        /// </summary>
        /// <remarks>This method should change the status</remarks>
        public event EventHandler<EventArgs> SchedulerResumed;
        /// <summary>
        /// Method called prior to raising the <see cref="SchedulerPaused"/> event
        /// </summary>
        /// <param name="e"></param>
        /// <remarks>This method should change the status</remarks>
        protected virtual void OnSchedulerResumed(EventArgs e)
        {
            // Logging
            Logger?.LogTrace($"[{nameof(OnSchedulerResumed)}] Entering function...");

            Status = SchedulerStatus.Active;
            SchedulerResumed?.Invoke(this, e);

            // Logging
            Logger?.LogTrace($"[{nameof(OnSchedulerResumed)}] Exiting function...");
        }

        /// <summary>
        /// An event triggered when a request to stop the scheduler is called
        /// </summary>
        public event EventHandler<EventArgs> SchedulerStopping;
        /// <summary>
        /// Method caleld prior to raising the <see cref="SchedulerStopping"/> event
        /// </summary>
        /// <param name="e"></param>
        /// <remarks>This method should change the status</remarks>
        protected virtual void OnSchedulerStopping(EventArgs e)
        {
            // Logging
            Logger?.LogTrace($"[{nameof(OnSchedulerStopping)}] Entering function...");

            Status = SchedulerStatus.Stopping;
            SchedulerStopping?.Invoke(this, e);

            // Logging
            Logger?.LogTrace($"[{nameof(OnSchedulerStopping)}] Exiting function...");

        }

        /// <summary>
        /// An event triggered when the scheduler service fully stops
        /// </summary>
        public event EventHandler<EventArgs> SchedulerStopped;
        /// <summary>
        /// Method called prior to raising the <see cref="SchedulerStopped"/> event
        /// </summary>
        /// <param name="e"></param>
        /// <remarks>This method should change the status</remarks>
        protected virtual void OnSchedulerStopped(EventArgs e)
        {
            // Logging
            Logger?.LogTrace($"[{nameof(OnSchedulerStopped)}] Entering function...");

            Status = SchedulerStatus.Stopped;
            SchedulerStopped?.Invoke(this, e);

            // Logging
            Logger?.LogTrace($"[{nameof(OnSchedulerStopped)}] Exiting function...");
        }

        /// <summary>
        /// An event triggered when an event should be executed
        /// </summary>
        public event EventHandler<ScheduleEventArgs> EventTimeReached;

        /// <summary>
        /// Method called prior to raising the <see cref="EventTimeReached"/> event
        /// </summary>
        /// <param name="e">The arguments for the <see cref="EventTimeReached"/> event</param>
        protected virtual void OnEventTimeReached(ScheduleEventArgs e)
        {
            // Logging
            Logger?.LogTrace($"[{nameof(OnEventTimeReached)}] Event '{e.EventInfo.Name}' time reached.");

            EventTimeReached?.Invoke(this, e);
        }

        /// <summary>
        /// Method called once an internal event is triggered.
        /// </summary>
        /// <remarks>An internal event does not trigger the <see cref="EventTimeReached"/> event. Instead, they must be handled by this method.</remarks>
        /// <param name="e">The arguments for the internal event</param>
        protected virtual void OnInternalEventTimeReached(ScheduleEventArgs e)
        {
            // Logging
            Logger?.LogTrace($"[{nameof(OnInternalEventTimeReached)}] Event '{e.EventInfo.Name}' time reached.");

            // Internal Events do not trigger the regular 'EventTimeReached'. Instead, they must be handled by this method.
            
            // Process the "$CALCULATE_SCHEDULE" event
            if (e.EventInfo.Name == INTERNAL_EVENT_CALCULATE_SCHEDULE)
            {
                // This event needs to lock the Scheduler
                lock (SchedulerLock)
                {
                    // Look for events that needs their schedule to be rebuild. A complete rebuild is triggered.
                    foreach (var eventInfo in EventMaster)
                    {
                        if (eventInfo.Value.LastBuiltDate.Date < DateTime.Today)
                            CalculateSchedule(eventInfo.Key, true, DateTime.Today, DateTime.Today.AddDays(SCHEDULE_BUILD_DAYS), eventInfo.Value.ScheduleConfiguration);
                    }

                }
            }
        }

        /// <summary>
        /// An event triggered when a record is added to the schedule
        /// </summary>
        public event EventHandler<ScheduleEventArgs> RecordAddedToSchedule;

        /// <summary>
        /// Method called prior to raising the <see cref="RecordAddedToSchedule"/> event
        /// </summary>
        /// <param name="e">The arguments for the <see cref="RecordAddedToSchedule"/> event</param>
        protected virtual void OnRecordAddedToSchedule(ScheduleExecutionEventArgs e)
        {
            // Logging
            Logger?.LogTrace($"[{nameof(OnRecordAddedToSchedule)}] Event '{e.EventInfo.Name}' has been added to the schedule. It will be executed at '{e.ExecutionInfo.ExecutionDateTime:DISPLAY_DATE_FORMAT}'");

            RecordAddedToSchedule?.Invoke(this, e);
        }

        /// <summary>
        /// An event triggered when a record is removed from the schedule
        /// </summary>
        public event EventHandler<ScheduleEventArgs> RecordRemovedFromSchedule;

        /// <summary>
        /// Method called prior to raising the <see cref="OnRecordRemovedFromSchedule"/> event
        /// </summary>
        /// <param name="e">The arguments for the <see cref="RecordRemovedFromSchedule"/> event</param>
        protected virtual void OnRecordRemovedFromSchedule(ScheduleExecutionEventArgs e)
        {
            // Logging
            Logger?.LogTrace($"[{nameof(OnRecordRemovedFromSchedule)}] Event '{e.EventInfo.Name}' at '{e.ExecutionInfo.ExecutionDateTime:DISPLAY_DATE_FORMAT}' has been removed from schedule.");

            RecordRemovedFromSchedule?.Invoke(this, e);
        }

        #endregion Events

        #region Status Management

        /// <summary>
        /// A lock for the <see cref="Status"/> property
        /// </summary>
        private readonly object statusLock = new object();

        /// <summary>
        /// The internal variable to store the <see cref="Status"/>
        /// </summary>
        private SchedulerStatus _status = SchedulerStatus.Stopped;

        /// <summary>
        /// The Scheduler Current Status
        /// </summary>
        public SchedulerStatus Status
        {
            get { return _status; }
            set
            {
                lock (statusLock)
                {
                    _status = value;
                }
            }
        }

        #endregion Status Management

        #region Scheduler Thread Management

        /// <summary>
        /// Cancellation Token to be used to cancel the scheduler thread (the one submitting the jobs)
        /// </summary>
        protected CancellationTokenSource ctsSchedulerThread = null;

        /// <summary>
        /// The Thread that submits jobs based on their schedule
        /// </summary>
        protected readonly Thread SchedulerThread = null;

        #endregion Scheduler Thread Management

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Scheduler{T}"/> class
        /// </summary>
        public Scheduler(): this(null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Scheduler{T}"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public Scheduler(ILogger logger)
        {
            this.Logger = logger;

            SchedulerThread = new Thread(ProcessSchedule)
            {
                Name = "Scheduler",
                IsBackground = true
            };
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Returns a list containing all events currently scheduled
        /// </summary>
        /// <remarks>Internal events are ignored by this method</remarks>
        public List<ScheduledEventExecutionInfo> GetCurrentSchedule()
        {
            return GetCurrentSchedule(false);
        }

        /// <summary>
        /// Returns a list containing all events currently scheduled
        /// </summary>
        /// <param name="includeInternalEvents">A flag to define whether to display internal events or not</param>
        public List<ScheduledEventExecutionInfo> GetCurrentSchedule(bool includeInternalEvents)
        {
            var returnList = new List<ScheduledEventExecutionInfo>();

            // Retrieve all events that are to be executed
            foreach (var item in ScheduledEvents)
            {
                if (item.Value.EventGroup == EventGroups.Regular || (item.Value.EventGroup == EventGroups.Internal && includeInternalEvents))
                    returnList.Add(item.Value);
            }
                

            return returnList;
        }

        /// <summary>
        /// Returns a list containing all events currently scheduled
        /// </summary>
        /// <param name="recordCount">The number of records to retrieve</param>
        public List<ScheduledEventExecutionInfo> GetCurrentSchedule(int recordCount)
        {
            return GetCurrentSchedule(recordCount, false);
        }

        /// <summary>
        /// Returns a list containing all events currently scheduled
        /// </summary>
        /// <param name="recordCount">The number of records to retrieve</param>
        /// <param name="includeInternalEvents">A flag to define whether to display internal events or not</param>
        public List<ScheduledEventExecutionInfo> GetCurrentSchedule(int recordCount, bool includeInternalEvents)
        {
            var returnList = new List<ScheduledEventExecutionInfo>();

            // Retrieve all events that are to be executed
            foreach (var item in ScheduledEvents)
            {
                if (item.Value.EventGroup == EventGroups.Regular || (item.Value.EventGroup == EventGroups.Internal && includeInternalEvents))
                    returnList.Add(item.Value);

                if (returnList.Count >= recordCount) break;
            }

            return returnList;
        }

        /// <summary>
        /// Returns a list containing events currently scheduled for a given event unique name
        /// </summary>
        /// <param name="eventUniqueName">The name of the event to search for</param>
        public List<ScheduledEventExecutionInfo> GetCurrentSchedule(string eventUniqueName)
        {
            var returnList = new List<ScheduledEventExecutionInfo>();

            // Retrieve all events that are to be executed
            foreach (var item in ScheduledEvents)
            {
                if (item.Value.Name == eventUniqueName.ToUpper())
                    returnList.Add(item.Value);
            }

            return returnList;
        }

        /// <summary>
        /// Returns a list containing events currently scheduled for a given event unique name
        /// </summary>
        /// <param name="eventUniqueName">The name of the event to search for</param>
        /// <param name="recordCount">The number of records to retrieve</param>
        public List<ScheduledEventExecutionInfo> GetCurrentSchedule(string eventUniqueName, int recordCount)
        {
            var returnList = new List<ScheduledEventExecutionInfo>();

            // Retrieve all events that are to be executed
            foreach (var item in ScheduledEvents)
            {
                if (item.Value.Name == eventUniqueName.ToUpper())
                {
                    returnList.Add(item.Value);
                    if (returnList.Count >= recordCount) break;
                }
            }

            return returnList;
        }

        /// <summary>
        /// Calculate the schedule for the events based on a input date and add to the list
        /// </summary>
        /// <param name="uniqueName">The unique name representing the event</param>
        /// <param name="destroyCurrentSchedule">Defines whether to destroy the existing schedule or keep it</param>
        /// <param name="scheduleBeginDate">The begin date to build the schedule at</param>
        /// <param name="scheduleEndDate">The end date to build the schedule</param>
        /// <param name="scheduleConfiguration">The configuration of the event execution date and time</param>
        /// <remarks>This function does not lock the <see cref="ScheduledEvents"/> list. Make sure it is locked before calling it.</remarks>
        protected void CalculateSchedule(string uniqueName, bool destroyCurrentSchedule, DateTime scheduleBeginDate, DateTime scheduleEndDate, ScheduleConfiguration scheduleConfiguration)
        {
            // Logging
            Logger?.LogTrace($"[{nameof(CalculateSchedule)}] Entering Function...");

            try
            {
                var uniqueNameUpper = uniqueName.ToUpper();

                if (!EventMaster.ContainsKey(uniqueNameUpper))
                    throw new ArgumentOutOfRangeException(nameof(uniqueName), $"Unable to find event '{uniqueNameUpper}'");

                // Retrieve Event
                var scheduledEvent = EventMaster[uniqueNameUpper];

                // Destroy current schedule if applicable
                if (destroyCurrentSchedule)
                    RemoveEventFromSchedule(uniqueName);

                var currentDateTime = DateTime.Now;
                var schedule = scheduleConfiguration.GetExecutionDates(scheduleBeginDate.Date, scheduleEndDate.Date);

                foreach (var executionTime in schedule)
                {
                    // Add the schedule to the real schedule
                    if (executionTime >= currentDateTime)
                    {
                        // Set the key (202201010830-EVENT for example)
                        var eventKey = executionTime.ToString(EVENT_KEY_DATE_FORMAT) + "-" + uniqueNameUpper;

                        if (!ScheduledEvents.ContainsKey(eventKey))
                        {
                            var executionInfo = new ScheduledEventExecutionInfo(scheduledEvent.Name, scheduledEvent.Description, executionTime, scheduledEvent.EventGroup);
                            ScheduledEvents.Add(eventKey, executionInfo);
                            OnRecordAddedToSchedule(new ScheduleExecutionEventArgs(EventMaster[uniqueNameUpper], executionInfo));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Logging
                Logger?.LogError(e, $"[{nameof(CalculateSchedule)}] {DEFAULT_EXCEPTION_MESSAGE}");
                throw e;
            }
            finally
            {
                // Logging
                Logger?.LogTrace($"[{nameof(CalculateSchedule)}] Entering Function...");
            }


        }

        /// <summary>
        /// Schedule an event
        /// </summary>
        /// <param name="uniqueName">The unique name representing the event</param>
        /// <param name="obj">The information regarding the event to be scheduled</param>
        /// <param name="scheduleConfiguration">The configuration of the event execution date and time</param>
        public void AddEventToSchedule(string uniqueName, T obj, ScheduleConfiguration scheduleConfiguration)
        {
            // Check the valid Event Names
            if (uniqueName.StartsWith("$"))
                throw new ArgumentException("Events should not start with the '$' character. This character is reserved for Internal events only.", nameof(uniqueName));

            // Check the valid Event Names
            if (uniqueName.StartsWith("@"))
                throw new ArgumentException("Events should not start with the '@' character", nameof(uniqueName));

            var newScheduleInfo = new ScheduledEventInfo
            {
                Name = uniqueName.ToUpper(),
                Description = uniqueName,
                Event = obj,
                ScheduleConfiguration = scheduleConfiguration
            };

            AddEventToSchedule(newScheduleInfo);
        }

        /// <summary>
        /// Internal method to add an event to the schedule
        /// </summary>
        /// <param name="eventInfo">The event information</param>
        internal void AddEventToSchedule(ScheduledEventInfo eventInfo)
        {
            // Logging
            Logger?.LogTrace($"[{nameof(AddEventToSchedule)}] Entering Function...");
            
            try
            {
                lock (SchedulerLock)
                {
                    // Check for Unique Name. If duplicated, don't let the system continue.
                    if (EventMaster.ContainsKey(eventInfo.Name))
                        throw new Exception($"There is already a schedule for '{eventInfo.Name}'");

                    EventMaster.Add(eventInfo.Name, eventInfo);

                    // Build the schedule for N days
                    var currentDate = DateTime.Today;
                    CalculateSchedule(eventInfo.Name, false, currentDate, currentDate.AddDays(SCHEDULE_BUILD_DAYS), eventInfo.ScheduleConfiguration);

                    // Update the Built Schedule
                    EventMaster[eventInfo.Name].LastBuiltDate = currentDate;
                }

            }
            catch (Exception e)
            {
                // Logging
                Logger?.LogError(e, $"[{nameof(AddEventToSchedule)}] {DEFAULT_EXCEPTION_MESSAGE}");

                throw e;
            }
            finally
            {
                // Logging
                Logger?.LogTrace($"[{nameof(AddEventToSchedule)}] Exiting Function...");
            }

        }

        /// <summary>
        /// Removes an event from the schedule
        /// </summary>
        /// <param name="uniqueName">The unique name representing the event</param>
        public void RemoveEventFromSchedule(string uniqueName)
        {
            // Logging
            Logger?.LogTrace($"[{nameof(RemoveEventFromSchedule)}] Entering Function...");

            try
            {
                lock (SchedulerLock)
                {
                    // Check for Unique Name. If duplicated, don't let the system continue.
                    var uniqueNameUpper = uniqueName.ToUpper();
                    if (!EventMaster.ContainsKey(uniqueNameUpper))
                        throw new Exception($"There is no event with the unique name '{uniqueNameUpper}' to be removed");

                    // Look for all events and remove the events based on the unique name
                    foreach (var item in ScheduledEvents)
                    {
                        if (item.Value.Name == uniqueNameUpper)
                        {
                            ScheduledEvents.Remove(item.Key);
                            OnRecordRemovedFromSchedule(new ScheduleExecutionEventArgs(EventMaster[uniqueNameUpper], item.Value));
                        }
                    }

                    // Remove from Event Master
                    EventMaster.Remove(uniqueNameUpper);
                }
            }
            catch (Exception e)
            {
                // Logging
                Logger?.LogError(e, $"[{nameof(RemoveEventFromSchedule)}] {DEFAULT_EXCEPTION_MESSAGE}");

                throw e;
            }
            finally
            {
                // Logging
                Logger?.LogTrace($"[{nameof(RemoveEventFromSchedule)}] Exiting Function...");
            }
        }

        /// <summary>
        /// Updates the Schedule with a new one
        /// </summary>
        /// <param name="uniqueName">The unique name representing the event</param>
        /// <param name="obj">The information regarding the event to be scheduled</param>
        /// <param name="scheduleEntry">The configuration of the event execution date and time</param>
        /// <remarks>This function will basically call the <see cref="RemoveEventFromSchedule(string)"/> and the <see cref="AddEventToSchedule(string, T, ScheduleConfiguration)"/></remarks>
        public void ReplaceEventSchedule(string uniqueName, T obj, ScheduleConfiguration scheduleEntry)
        {
            // Logging
            Logger?.LogTrace($"[{nameof(ReplaceEventSchedule)}] Entering Function...");

            try
            {
                // Remove the existing schedule
                RemoveEventFromSchedule(uniqueName);

                // Add the new schedule
                AddEventToSchedule(uniqueName, obj, scheduleEntry);

            }
            catch (Exception e)
            {
                // Logging
                Logger?.LogError(e, $"[{nameof(ReplaceEventSchedule)}] {DEFAULT_EXCEPTION_MESSAGE}");
                throw e;
            }
            finally
            {
                // Logging
                Logger?.LogTrace($"[{nameof(ReplaceEventSchedule)}] Exiting Function...");
            }
        }

        #endregion Methods

        #region Thread Processing

        /// <summary>
        /// Starts the Scheduler
        /// </summary>
        public void Start()
        {
            // Logging
            Logger?.LogTrace($"[{nameof(Start)}] Entering Function...");

            try
            {
                // Initialize the scheduler
                if (Status == SchedulerStatus.Stopped)
                {
                    Status = SchedulerStatus.Starting;

                    OnCreateInternalEvents();

                    ctsSchedulerThread = new CancellationTokenSource();
                    SchedulerThread.Start(ctsSchedulerThread.Token);
                }
            }
            catch (Exception e)
            {
                // Logging
                Logger?.LogError(e, $"[{nameof(Start)}] Error while starting the Scheduler");

                OnSchedulerStopped(new EventArgs());
                throw e;
            }
            finally
            {
                // Logging
                Logger?.LogTrace($"[{nameof(Start)}] Exiting Function...");
            }
        }

        /// <summary>
        /// Method called to add internal events to the scheduler
        /// </summary>
        /// <remarks>When overriding this method, make sure to call the base method</remarks>
        protected virtual void OnCreateInternalEvents()
        {
            // Add the "Calculate Schedule" internal event. This event will trigger a recalculation of the schedule multiple times a day.
            var calculateScheduleEvent = new ScheduledEventInfo(EventGroups.Internal)
            {
                Name = INTERNAL_EVENT_CALCULATE_SCHEDULE,
                Description = INTERNAL_EVENT_CALCULATE_SCHEDULE,
                Event = null,
                ScheduleConfiguration = new MultiScheduleConfiguration("12:00 AM|06:00 AM|12:00 PM|06:00 PM", "YYYYYYY")
            };

            AddEventToSchedule(calculateScheduleEvent);
        }

        /// <summary>
        /// Requests the Scheduler to stop
        /// </summary>
        /// <remarks>This method will request a stop and exit irrespective of the scheduler status. Check the <see cref="SchedulerStopped"/> event to ensure the service is stopped.</remarks>
        public void Stop()
        {
            // Logging
            Logger?.LogTrace($"[{nameof(Stop)}] Entering Function...");

            try
            {
                // Attempt to cancel the execution of the scheduler thread
                if (Status == SchedulerStatus.Stopped ||
                    Status == SchedulerStatus.StopRequested ||
                    Status == SchedulerStatus.Stopping)
                {
                    // Logging
                    Logger?.LogTrace($"[{nameof(Stop)}] Scheduler is already being stopped, no action needed");
                    return;
                }
                
                // Initializes the Stop Procedure (which will force the loop at "ProcessSchedule" to end)
                Status = SchedulerStatus.StopRequested;

                if (ctsSchedulerThread != null)
                {
                    // There is a thread running, cancel it

                    OnSchedulerStopping(new EventArgs());
                    ctsSchedulerThread.Cancel();

                    // Awake thread if it is sleeping
                    if (SchedulerThread.ThreadState.HasFlag(ThreadState.WaitSleepJoin)) SchedulerThread.Interrupt();
                }
                else
                {
                    // There is no thread running, just call the event then
                    OnSchedulerStopping(new EventArgs());
                    OnSchedulerStopped(new EventArgs());
                }
            }
            catch (Exception e)
            {
                // Logging
                Logger?.LogError(e, $"[{nameof(Stop)}] Error while stopping the Scheduler");

                throw e;
            }
            finally
            {
                // Logging
                Logger?.LogTrace($"[{nameof(Stop)}] Exiting Function...");
            }
        }

        /// <summary>
        /// Pause the Scheduler Services
        /// </summary>
        /// <remarks>The system will put the <see cref="SchedulerThread"/> into a sleep state, which has to be awake using the <see cref="Resume(bool)"/> method</remarks>
        public void Pause()
        {
            Pause(false);
        }

        /// <summary>
        /// Pause the Scheduler Services
        /// </summary>
        /// <param name="waitForCompletion">Defines whether the method will return only after the pause has been completed</param>
        /// <remarks>The system will put the <see cref="SchedulerThread"/> into a sleep state, which has to be awake using the <see cref="Resume(bool)"/> method</remarks>
        public void Pause(bool waitForCompletion)
        {
            // Logging
            Logger?.LogTrace($"[{nameof(Pause)}] Entering Function...");

            try
            {
                // Attempt to pause the execution of the scheduler thread
                if (Status == SchedulerStatus.Paused ||
                    Status == SchedulerStatus.PauseRequested ||
                    Status == SchedulerStatus.Pausing)
                {
                    // Logging
                    Logger?.LogTrace($"[{nameof(Pause)}] Scheduler is already paused or being paused, no action needed");
                    return;
                }

                // Initializes the Pause Procedure (which will force the loop at "ProcessSchedule" to stop )
                Status = SchedulerStatus.PauseRequested;

                if (ctsSchedulerThread == null)
                {
                    // There is no thread running, just call the events then
                    OnSchedulerPausing(new EventArgs());
                    OnSchedulerPaused(new EventArgs());
                }

                // Awake thread if it is sleeping
                if (SchedulerThread.ThreadState.HasFlag(ThreadState.WaitSleepJoin)) SchedulerThread.Interrupt();

                // Check Wait For Completion Parameter
                if (waitForCompletion)
                {
                    var startTime = DateTime.Now;
                    SpinWait spin = new SpinWait();

                    while (Status != SchedulerStatus.Paused)
                    {
                        spin.SpinOnce();

                        var timeElapsed = DateTime.Now - startTime;
                        if (timeElapsed.TotalMilliseconds > WAIT_FOR_COMPLETION_TIMEOUT)
                        {
                            // Time is up, throw an exception then
                            throw new TimeoutException($"The method could not complete in less than {WAIT_FOR_COMPLETION_TIMEOUT} milliseconds");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Logging
                Logger?.LogError(e, $"[{nameof(Pause)}] Error while pausing the Scheduler");

                throw e;
            }
            finally
            {
                // Logging
                Logger?.LogTrace($"[{nameof(Pause)}] Exiting Function...");
            }
        }

        /// <summary>
        /// Resumes the scheduling services
        /// </summary>
        /// <remarks>The system will call an interrupt on the <see cref="SchedulerThread"/>. If the thread is not at a valid state, it will throw a <see cref="ThreadStateException"/>.</remarks>
        /// <exception cref="ThreadStateException">Thrown when the <see cref="SchedulerThread"/> is not at <see cref="ThreadState.WaitSleepJoin"/> state</exception>
        public void Resume()
        {
            Resume(false);
        }

        /// <summary>
        /// Resumes the scheduling services
        /// </summary>
        /// <param name="waitForCompletion">Defines whether the method will return only after the pause has been completed</param>
        /// <remarks>The system will call an interrupt on the <see cref="SchedulerThread"/>. If the thread is not at a valid state, it will throw a <see cref="ThreadStateException"/>.</remarks>
        /// <exception cref="ThreadStateException">Thrown when the <see cref="SchedulerThread"/> is not at <see cref="ThreadState.WaitSleepJoin"/> state</exception>
        public void Resume(bool waitForCompletion)
        {
            // Logging
            Logger?.LogTrace($"[{nameof(Resume)}] Entering Function...");

            try
            {
                // Attempt to pause the execution of the scheduler thread
                if (Status == SchedulerStatus.Active ||
                    Status == SchedulerStatus.ResumeRequested ||
                    Status == SchedulerStatus.Resuming)
                {
                    // Logging
                    Logger?.LogTrace($"[{nameof(Resume)}] Scheduler is already active or being resumed, no action needed");
                    return;
                }

                // Initializes the Resume Procedure (which will force the loop at "ProcessSchedule" to continue )
                Status = SchedulerStatus.ResumeRequested;

                if (ctsSchedulerThread == null)
                {
                    // There is no thread running, just call the events then
                    OnSchedulerResuming(new EventArgs());
                    OnSchedulerResumed(new EventArgs());
                }
                else
                {
                    // Wake up sleeping thread
                    if (SchedulerThread.ThreadState.HasFlag(ThreadState.WaitSleepJoin))
                        SchedulerThread.Interrupt();
                    else
                        // Something is missing and the thread cannot be resumed
                        throw new ThreadStateException($"The {nameof(SchedulerThread)} is not at the {nameof(ThreadState.WaitSleepJoin)} state, therefore, it cannot be awoke");
                }

                // Check Wait For Completion Parameter
                if (waitForCompletion)
                {
                    var startTime = DateTime.Now;
                    SpinWait spin = new SpinWait();

                    while (Status != SchedulerStatus.Active)
                    {
                        spin.SpinOnce();

                        var timeElapsed = DateTime.Now - startTime;
                        if (timeElapsed.TotalMilliseconds > WAIT_FOR_COMPLETION_TIMEOUT)
                        {
                            // Time is up, throw an exception then
                            throw new TimeoutException($"The method could not complete in less than {WAIT_FOR_COMPLETION_TIMEOUT} milliseconds");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Logging
                Logger?.LogError(e, $"[{nameof(Resume)}] Error while resuming the Scheduler");

                throw e;
            }
            finally
            {
                // Logging
                Logger?.LogTrace($"[{nameof(Resume)}] Exiting Function...");
            }
        }

        /// <summary>
        /// Method to be executed by the Scheduler Thread, which is in charge of calling the events when a task has to be submitted.
        /// </summary>
        /// <param name="obj">The Cancellation Token to be used to cancel the Scheduler Thread</param>
        protected void ProcessSchedule(object obj)
        {
            // Logging
            Logger?.LogTrace($"[{nameof(ProcessSchedule)}] Entering Function...");

            try
            {
                // Reference to the Cancellation Token
                var cancellationToken_SchedulerThread = (CancellationToken)obj;

                // Ensure to notify that the service is active. Everything else will happen inside the loop now.
                OnSchedulerStarted(new EventArgs());

                // Keep running until a cancellation is requested
                while (!cancellationToken_SchedulerThread.IsCancellationRequested)
                {
                    // Check for Pause State
                    if (Status == SchedulerStatus.PauseRequested)
                    {
                        OnSchedulerPausing(new EventArgs());
                        OnSchedulerPaused(new EventArgs());

                        try
                        {
                            // Put thread to sleep until it is hit by an interrupt
                            Thread.Sleep(Timeout.Infinite);
                        }
                        catch (ThreadInterruptedException)
                        {
                            OnSchedulerResuming(new EventArgs());
                            OnSchedulerResumed(new EventArgs());
                        }
                    }

                    // Cache the Execution Date/Time
                    var currentDateTime = DateTime.Now;

                    lock (SchedulerLock)
                    {
                        // Keep checking events to trigger
                        while (true)
                        {
                            // Logging
                            Logger?.LogTrace($"[{nameof(ProcessSchedule)}] Checking for events to trigger...");

                            // No jobs in queue, nothing to do
                            if (ScheduledEvents.Count == 0) break;

                            // Logging
                            Logger?.LogTrace($"[{nameof(ProcessSchedule)}] Retrieving event info...");

                            // Get the next job to execute
                            var nextScheduledEventKey = ScheduledEvents.Keys[0];
                            var nextScheduledEventExecutionInfo = ScheduledEvents[nextScheduledEventKey];
                            var nextScheduledEventName = ScheduledEvents[nextScheduledEventKey].Name;
                            var nextScheduledEventInfo = EventMaster[nextScheduledEventName];

                            // Logging
                            Logger?.LogTrace($"[{nameof(ProcessSchedule)}] Event Key is: {nextScheduledEventKey}, to be executed at '{nextScheduledEventExecutionInfo.ExecutionDateTime:DISPLAY_DATE_FORMAT}'.");

                            if (nextScheduledEventExecutionInfo.ExecutionDateTime <= currentDateTime)
                            {
                                // Check the proper event group
                                if (nextScheduledEventInfo.EventGroup == EventGroups.Internal)
                                {
                                    // Internal Events only call the internal function
                                    OnInternalEventTimeReached(new ScheduleEventArgs(nextScheduledEventInfo));
                                }
                                else
                                {
                                    
                                    if (TriggerEventsAsynchronously)
                                    {
                                        // Call the event asynchronously
                                        Task.Run(() => OnEventTimeReached(new ScheduleEventArgs(nextScheduledEventInfo)));
                                    }
                                    else
                                    {
                                        // Call the event synchronously
                                        OnEventTimeReached(new ScheduleEventArgs(nextScheduledEventInfo));
                                    }
                                }

                                // Remove event from the list
                                ScheduledEvents.RemoveAt(0);
                            }
                            else
                            {
                                // Logging
                                Logger?.LogTrace($"[{nameof(ProcessSchedule)}] Event should not be executed just yet.");

                                // Exit the infinite loop
                                break;
                            }
                        }
                    }

                    // Wait for next attempt
                    try
                    {
                        Thread.Sleep(WAITING_TIME_CHECKING);
                    }
                    catch (ThreadInterruptedException)
                    {
                        // Nothing to do, it was just awoke
                    }
                    catch (Exception)
                    {
                        // An error happened, throw exception forward
                        throw;
                    }
                }
            }
            catch (Exception e)
            {
                // Logging
                Logger?.LogError(e, $"[{nameof(ProcessSchedule)}] A fatal error occurred while processing the scheduler. This will cause the Scheduler to Stop.");

                throw e;
            }
            finally
            {
                // Scheduler Stopped
                OnSchedulerStopped(new EventArgs());

                // Logging
                Logger?.LogTrace($"[{nameof(ProcessSchedule)}] Exiting Function...");
            }
        }

        #endregion Thread Processing
    }

    /// <summary>
    /// Specifies the Scheduler States
    /// </summary>
    public enum SchedulerStatus:int
    {
        /// <summary>
        /// The Scheduler is Stopped
        /// </summary>
        Stopped = 0,
        /// <summary>
        /// The Scheduler is going to stop scheduling events
        /// </summary>
        StopRequested = 1,
        /// <summary>
        /// The Scheduler is processing a Stop Request, and it will be stopped once all scheduling events are deleted
        /// </summary>
        Stopping = 2,
        /// <summary>
        /// The Scheduler is active and processing events
        /// </summary>
        Active = 10,
        /// <summary>
        /// The Scheduler is Starting
        /// </summary>
        Starting = 11,
        /// <summary>
        /// The Scheduler is Paused
        /// </summary>
        Paused = 20,
        /// <summary>
        /// The Scheduler is going to pause scheduling events
        /// </summary>
        PauseRequested = 21,
        /// <summary>
        /// The Scheduler is processing a Pause Request
        /// </summary>
        Pausing = 22,
        /// <summary>
        /// The Scheduler is going to resume scheduling events
        /// </summary>
        ResumeRequested = 31,
        /// <summary>
        /// The Scheduler is resuming from a Paused state
        /// </summary>
        Resuming = 32
    }
    
}

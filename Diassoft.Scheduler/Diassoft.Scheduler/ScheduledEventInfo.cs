using Diassoft.Scheduler.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Diassoft.Scheduler
{
    /// <summary>
    /// Represent information regarding an event being scheduled
    /// </summary>
    public class ScheduledEventInfo
    {
        /// <summary>
        /// A unique name for the event
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// A description for the event
        /// </summary>
        public string Description { get; set; } = "";
        /// <summary>
        /// An object containing event data to be used for the event submission
        /// </summary>
        public object Event { get; set; } = null;
        /// <summary>
        /// The schedule configuration for the event
        /// </summary>
        public ScheduleConfiguration ScheduleConfiguration { get; set; } = null;
        /// <summary>
        /// The time the schedule for the event has been built for the last time
        /// </summary>
        public DateTime LastBuiltDate { get; set; } = DateTime.MinValue;
        /// <summary>
        /// Defines the group of the event
        /// </summary>
        /// <remarks>See the enumeration <see cref="EventGroups"/> for more information</remarks>
        public EventGroups EventGroup { get; internal set; } = EventGroups.Regular;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledEventInfo"/> class
        /// </summary>
        public ScheduledEventInfo(): this(EventGroups.Regular)
        {

        }

        /// <summary>
        /// Internal method that initializes a new instance of the <see cref="ScheduledEventInfo"/> class
        /// </summary>
        /// <param name="eventGroup">The event group</param>
        internal ScheduledEventInfo(EventGroups eventGroup)
        {
            EventGroup = eventGroup;
        }
    }

    /// <summary>
    /// A list of valid groups of events
    /// </summary>
    public enum EventGroups:byte
    {
        /// <summary>
        /// This is a regular event.
        /// </summary>
        Regular = 0,
        /// <summary>
        /// This is an internal event, only important for the <see cref="EventScheduler{T}"/> class.
        /// </summary>
        /// <remarks>Internal Events do not trigger the <see cref="EventScheduler{T}.EventTimeReached"/>. Instead, they call an internal event of the <see cref="EventScheduler{T}"/> class.</remarks>
        Internal = 1
    }

}

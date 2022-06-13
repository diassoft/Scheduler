using System;
using System.Collections.Generic;
using System.Text;

namespace Diassoft.Scheduler
{
    /// <summary>
    /// Represent information regarding the execution of an event
    /// </summary>
    public sealed class ScheduledEventExecutionInfo
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
        /// The time the event will be triggered
        /// </summary>
        public DateTime ExecutionDateTime { get; set; } = DateTime.MinValue;
        /// <summary>
        /// Defines the group of the event
        /// </summary>
        /// <remarks>See the enumeration <see cref="EventGroups"/> for more information</remarks>
        public EventGroups EventGroup { get; internal set; } = EventGroups.Regular;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledEventExecutionInfo"/> class
        /// </summary>
        /// <param name="name">The unique name of the event</param>
        /// <param name="description">The description of the event</param>
        /// <param name="executionDateTime">The date and time the event will be triggered</param>
        public ScheduledEventExecutionInfo(string name, string description, DateTime executionDateTime): this(name, description, executionDateTime, EventGroups.Regular) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledEventExecutionInfo"/> class
        /// </summary>
        /// <param name="name">The unique name of the event</param>
        /// <param name="description">The description of the event</param>
        /// <param name="executionDateTime">The date and time the event will be triggered</param>
        /// <param name="eventGroup">The event group (for internal purposes only)</param>
        internal ScheduledEventExecutionInfo(string name, string description, DateTime executionDateTime, EventGroups eventGroup)
        {
            Name = name;
            Description = description;
            ExecutionDateTime = executionDateTime;
            EventGroup = eventGroup;
        }

    }
}

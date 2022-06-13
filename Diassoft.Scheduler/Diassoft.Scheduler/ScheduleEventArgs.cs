using System;
using System.Collections.Generic;
using System.Text;

namespace Diassoft.Scheduler
{
    /// <summary>
    /// Provides data for the schedule event
    /// </summary>
    public class ScheduleEventArgs : EventArgs
    {
        /// <summary>
        /// Defines event information
        /// </summary>
        public ScheduledEventInfo EventInfo { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleEventArgs"/> class
        /// </summary>
        /// <param name="eventInfo">Information regarding the event being triggered</param>
        public ScheduleEventArgs(ScheduledEventInfo eventInfo)
        {
            this.EventInfo = eventInfo;
        }

    }
}

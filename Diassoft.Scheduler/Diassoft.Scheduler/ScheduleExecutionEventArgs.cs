using System;
using System.Collections.Generic;
using System.Text;

namespace Diassoft.Scheduler
{
    /// <summary>
    /// Provides data for the schedule execution event
    /// </summary>
    public class ScheduleExecutionEventArgs: ScheduleEventArgs
    {
        /// <summary>
        /// The information regarding the event execution
        /// </summary>
        public ScheduledEventExecutionInfo ExecutionInfo { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleExecutionEventArgs"/> class
        /// </summary>
        /// <param name="eventInfo">Information regarding the event being executed</param>
        /// <param name="executionInfo">Information regarding the event execution</param>
        public ScheduleExecutionEventArgs(ScheduledEventInfo eventInfo, ScheduledEventExecutionInfo executionInfo): base(eventInfo)
        {
            ExecutionInfo = executionInfo;
        }

    }
}

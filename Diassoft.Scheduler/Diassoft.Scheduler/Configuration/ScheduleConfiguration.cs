using System;
using System.Collections.Generic;
using System.Text;

namespace Diassoft.Scheduler.Configuration
{
    /// <summary>
    /// Represents the base class of a schedule entry
    /// </summary>
    public abstract class ScheduleConfiguration
    {
        /// <summary>
        /// An array containing the accepted time formats
        /// </summary>
        public readonly string[] AcceptedTimeFormats = new string[]
        {
            "hh:mm tt",             // 02:30 AM     (12-hour clock)
            "h:mm tt",              // 2:30 AM      (12-hour clock)
            "H:mm",                 // 7:30         (24-hour clock)
            "HH:mm"                 // 07:30        (24-hour clock)
        };

        /// <summary>
        /// The date when the task should start to be executed
        /// </summary>
        public DateTime StartDate { get; set; } = System.DateTime.MinValue;
        /// <summary>
        /// The date when the task should stop being executed
        /// </summary>
        public DateTime EndDate { get; set; } = System.DateTime.MaxValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleConfiguration"/>
        /// </summary>
        public ScheduleConfiguration() { }

        /// <summary>
        /// A function to calculate the schedule based on the parameters
        /// </summary>
        /// <param name="scheduleBeginDate">The begin date to build the schedule at</param>
        /// <param name="scheduleEndDate">The end date to build the schedule</param>
        /// <returns>A <see cref="List{T}"/> containing the execution dates and times</returns>
        public virtual List<DateTime> GetExecutionDates(DateTime scheduleBeginDate, DateTime scheduleEndDate)
        {
            // An empty list to return
            var schedule = new List<DateTime>();

            // Make sure date is valid
            if (scheduleBeginDate == DateTime.MinValue || scheduleBeginDate == DateTime.MaxValue)
                throw new ArgumentException("Invalid Begin Date for building the schedule", nameof(scheduleBeginDate));

            if (scheduleEndDate == DateTime.MinValue || scheduleEndDate == DateTime.MaxValue)
                throw new ArgumentException("Invalid End Date for building the schedule", nameof(scheduleEndDate));

            if (scheduleBeginDate > scheduleEndDate)
                throw new ArgumentException("Schedule Begin Date cannot be greater than Schedule End Date", nameof(scheduleBeginDate));

            return schedule;
        }
    }

}

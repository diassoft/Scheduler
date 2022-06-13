using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Diassoft.Scheduler.Configuration
{
    /// <summary>
    /// Represents a schedule entry with information to be executed multiple times during the day
    /// </summary>
    public class MultiScheduleConfiguration: RecurrentScheduleConfiguration
    {
        #region Properties

        /// <summary>
        /// An array containing the times to perform the scheduled task
        /// </summary>
        public DateTime[] TimesArray { get; private set; } = new DateTime[0];

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiScheduleConfiguration"/> class
        /// </summary>
        public MultiScheduleConfiguration(): this(String.Empty) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiScheduleConfiguration"/> class
        /// </summary>
        /// <param name="times">A string containing times separated by '|'</param>
        public MultiScheduleConfiguration(string times): this(times, String.Empty) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiScheduleConfiguration"/> class
        /// </summary>
        /// <param name="times">A string containing times separated by '|'</param>
        /// <param name="recurrence">A string containing the recurrence. See <see cref="Recurrence.Recurrence(string)"/> for more details.</param>
        public MultiScheduleConfiguration(string times, string recurrence): this(times, new Recurrence(recurrence)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiScheduleConfiguration"/> class
        /// </summary>
        /// <param name="times">A string containing times separated by '|'</param>
        /// <param name="recurrence">An object containing the recurrence</param>
        public MultiScheduleConfiguration(string times, Recurrence recurrence)
        {
            if (!string.IsNullOrEmpty(times)) UpdateScheduledTimes(times);
            Recurrence = recurrence;
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Populate the <see cref="TimesArray"/> with all times when the task execution must happen
        /// </summary>
        /// <param name="times">A string containing times separated by '|'</param>
        public void UpdateScheduledTimes(string times)
        {
            var tempTimesArray = times.Split('|');
            var tempListOfTimes = new List<DateTime>();

            foreach (var time in tempTimesArray)
            {
                if (!DateTime.TryParseExact(time, AcceptedTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime submitTime))
                    throw new Exception($"The time '{time}' is not in a valid format. Use one of the following formats: {String.Join(",", AcceptedTimeFormats)}");

                if (!tempListOfTimes.Contains(submitTime))
                    tempListOfTimes.Add(submitTime);
            }

            TimesArray = tempListOfTimes.ToArray();
        }

        /// <summary>
        /// Calculate the Schedule
        /// </summary>
        /// <param name="scheduleBeginDate">The begin date to build the schedule at</param>
        /// <param name="scheduleEndDate">The end date to build the schedule</param>
        /// <returns>A <see cref="List{T}"/> containing the execution dates and times</returns>
        public override List<DateTime> GetExecutionDates(DateTime scheduleBeginDate, DateTime scheduleEndDate)
        {
            // Perform basic validations and return an empty list to be used later
            var schedule = base.GetExecutionDates(scheduleBeginDate, scheduleEndDate);

            // Make sure times are configured
            if (TimesArray.Length == 0)
                throw new ArgumentException("Times for execution must be defined", nameof(TimesArray));

            // Process each day in the given range
            var currentDate = scheduleBeginDate.Date;

            while (currentDate <= scheduleEndDate)
            {
                // Check if current day of week is on the recurrence
                var currentDayOfWeek = (int)currentDate.DayOfWeek;

                if ((currentDayOfWeek == 0 && Recurrence.Sunday) ||
                    (currentDayOfWeek == 1 && Recurrence.Monday) ||
                    (currentDayOfWeek == 2 && Recurrence.Tuesday) ||
                    (currentDayOfWeek == 3 && Recurrence.Wednesday) ||
                    (currentDayOfWeek == 4 && Recurrence.Thursday) ||
                    (currentDayOfWeek == 5 && Recurrence.Friday) ||
                    (currentDayOfWeek == 6 && Recurrence.Saturday))
                {
                    // Day is valid for schedule generation

                    // Ensure a valid Start / End Dates
                    if (StartDate > EndDate)
                        throw new ArgumentException("Start Date cannot be greater than End Date", nameof(StartDate));

                    foreach (var time in TimesArray)
                    {
                        var currentScheduleDateTime = new DateTime(currentDate.Year, 
                                                                   currentDate.Month, 
                                                                   currentDate.Day, 
                                                                   time.Hour, 
                                                                   time.Minute, 
                                                                   0);

                        if (currentScheduleDateTime >= StartDate && currentScheduleDateTime <= EndDate)
                            schedule.Add(currentScheduleDateTime);
                    }
                }

                // Move to next day
                currentDate = currentDate.AddDays(1);
            }

            return schedule;
        }

        #endregion Methods
    }
}

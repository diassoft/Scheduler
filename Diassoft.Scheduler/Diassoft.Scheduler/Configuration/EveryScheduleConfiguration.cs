using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Diassoft.Scheduler.Configuration
{
    /// <summary>
    /// Represents a schedule entry with information to be executed every X seconds
    /// </summary>
    public class EveryScheduleConfiguration: RecurrentScheduleConfiguration
    {
        #region Properties

        /// <summary>
        /// The interval of execution in seconds
        /// </summary>
        /// <remarks>A negative less than zero means the task should never be executed</remarks>
        public int Interval { get; set; } = 0;
        /// <summary>
        /// The time to start executing the task
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.MinValue;
        /// <summary>
        /// The time to stop executing the task
        /// </summary>
        public DateTime EndTime { get; set; } = DateTime.MaxValue;

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EveryScheduleConfiguration"/> class
        /// </summary>
        public EveryScheduleConfiguration()
        {
            Interval = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EveryScheduleConfiguration"/> class
        /// </summary>
        /// <param name="interval">The interval of execution in seconds</param>
        /// <param name="startTime">A string containing the start time. See <see cref="ScheduleConfiguration.AcceptedTimeFormats"/> for a list of valid time formats.</param>
        /// <param name="endTime">A string containing the end time. See <see cref="ScheduleConfiguration.AcceptedTimeFormats"/> for a list of valid time formats.</param>
        public EveryScheduleConfiguration(int interval, string startTime, string endTime): this(interval, String.Empty, startTime, endTime) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EveryScheduleConfiguration"/> class
        /// </summary>
        /// <param name="interval">The interval of execution in seconds</param>
        /// <param name="recurrence">A string containing the recurrence. See <see cref="Recurrence.Recurrence(string)"/> for more details.</param>
        /// <param name="startTime">A string containing the start time. See <see cref="ScheduleConfiguration.AcceptedTimeFormats"/> for a list of valid time formats.</param>
        /// <param name="endTime">A string containing the end time. See <see cref="ScheduleConfiguration.AcceptedTimeFormats"/> for a list of valid time formats.</param>
        public EveryScheduleConfiguration(int interval, string recurrence, string startTime, string endTime): this(interval, new Recurrence(recurrence), startTime, endTime) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EveryScheduleConfiguration"/> class
        /// </summary>
        /// <param name="interval">The interval of execution in seconds</param>
        /// <param name="recurrence">An object containing the recurrence</param>
        /// <param name="startTime">A string containing the start time. See <see cref="ScheduleConfiguration.AcceptedTimeFormats"/> for a list of valid time formats.</param>
        /// <param name="endTime">A string containing the end time. See <see cref="ScheduleConfiguration.AcceptedTimeFormats"/> for a list of valid time formats.</param>
        public EveryScheduleConfiguration(int interval, Recurrence recurrence, string startTime, string endTime) : base(recurrence)
        {
            Interval = interval;

            // Attempt to Convert Start Time
            if (!DateTime.TryParseExact(startTime, AcceptedTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime newStartTime))
                throw new Exception($"The value of '{startTime}' (Start Time) is not in a valid format. Use one of the following formats: {String.Join(",", AcceptedTimeFormats)}");

            // Attempt to Convert End Time
            if (!DateTime.TryParseExact(endTime, AcceptedTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime newEndTime))
                throw new Exception($"The value of '{endTime}' (End Time) is not in valid format. Use one of the following formats: {String.Join(",", AcceptedTimeFormats)}");

            StartTime = newStartTime;
            EndTime = newEndTime;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="EveryScheduleConfiguration"/> class
        /// </summary>
        /// <param name="interval">The interval of execution in seconds</param>
        /// <param name="startTime">The execution start time</param>
        /// <param name="endTime">The execution end time</param>
        public EveryScheduleConfiguration(int interval, DateTime startTime, DateTime endTime): this(interval, String.Empty, startTime, endTime) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EveryScheduleConfiguration"/> class
        /// </summary>
        /// <param name="interval">The interval of execution in seconds</param>
        /// <param name="recurrence">A string string containing the recurrence. See <see cref="Recurrence.Recurrence(string)"/> for more details.</param>
        /// <param name="startTime">The execution start time</param>
        /// <param name="endTime">The execution end time</param>
        public EveryScheduleConfiguration(int interval, string recurrence, DateTime startTime, DateTime endTime): this(interval, new Recurrence(recurrence), startTime, endTime) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EveryScheduleConfiguration"/> class
        /// </summary>
        /// <param name="interval">The interval of execution in seconds</param>
        /// <param name="recurrence">An object string containing the recurrence</param>
        /// <param name="startTime">The execution start time</param>
        /// <param name="endTime">The execution end time</param>
        public EveryScheduleConfiguration(int interval, Recurrence recurrence, DateTime startTime, DateTime endTime) : base(recurrence)
        {
            Interval = interval;
            StartTime = startTime;
            EndTime = endTime;
        }

        #endregion Constructors

        #region Methods

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

            // Make sure interval is valid
            if (Interval < 0)
                throw new ArgumentException("Interval must be greater than 0", nameof(Interval));

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

                    // Define Start Time = when null, just assume 0:00 of the current day
                    var beginDateTime = (StartTime == DateTime.MinValue ?
                                         new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, 0, 0, 0) :
                                         new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, StartTime.Hour, StartTime.Minute, 0));

                    // Define End Time = when null, just assume 23:59 of the current day
                    var endDateTime = (EndTime == DateTime.MaxValue ?
                                       new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, 23, 59, 59) :
                                       new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, EndTime.Hour, EndTime.Minute, 0));

                    // Ensure a valid Start / End time
                    if (beginDateTime > endDateTime)
                        throw new ArgumentException("Start Time cannot be greater than End Time", nameof(StartTime));

                    // Start Storing Schedule Date / Times
                    var currentScheduleDateTime = beginDateTime;

                    while ((currentScheduleDateTime < endDateTime) &&
                           (currentScheduleDateTime >= StartDate) &&
                           (currentScheduleDateTime <= EndDate))
                    {
                        schedule.Add(currentScheduleDateTime);
                        currentScheduleDateTime = currentScheduleDateTime.AddSeconds(Interval);
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

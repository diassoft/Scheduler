using System;
using System.Collections.Generic;
using System.Text;

namespace Diassoft.Scheduler.Configuration
{
    /// <summary>
    /// Represents the base class of a recurrent schedule
    /// </summary>
    public abstract class RecurrentScheduleConfiguration: ScheduleConfiguration
    {
        /// <summary>
        /// The default recurrence
        /// </summary>
        protected const string DEFAULT_RECURRENCE = "NYYYYYN";

        /// <summary>
        /// The recurrence of the execution
        /// </summary>
        public Recurrence Recurrence { get; set; } = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecurrentScheduleConfiguration"/> class
        /// </summary>
        public RecurrentScheduleConfiguration(): this(DEFAULT_RECURRENCE) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecurrentScheduleConfiguration"/> class
        /// </summary>
        /// <param name="recurrence">A string containing the recurrence of the schedule. See <see cref="Recurrence.Recurrence(string)"/> for more details.</param>
        public RecurrentScheduleConfiguration(string recurrence): base()
        {
            if (String.IsNullOrEmpty(recurrence))
                Recurrence = new Recurrence(DEFAULT_RECURRENCE);
            else
                Recurrence = new Recurrence(recurrence);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecurrentScheduleConfiguration"/> class
        /// </summary>
        /// <param name="recurrence">An object containing the recurrence of the schedule</param>
        public RecurrentScheduleConfiguration(Recurrence recurrence) : base()
        {
            Recurrence = recurrence;
        }


    }
}

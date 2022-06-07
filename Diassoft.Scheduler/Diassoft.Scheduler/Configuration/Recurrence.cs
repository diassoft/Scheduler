using System;
using System.Collections.Generic;
using System.Text;

namespace Diassoft.Scheduler.Configuration
{
    /// <summary>
    /// Defines the Recurrence of an Schedule Entry
    /// </summary>
    public sealed class Recurrence
    {
        /// <summary>
        /// Schedule is active on Sunday
        /// </summary>
        public bool Sunday { get; set; } = false;
        /// <summary>
        /// Schedule is active on Monday
        /// </summary>
        public bool Monday { get; set; } = false;
        /// <summary>
        /// Schedule is active on Tuesday
        /// </summary>
        public bool Tuesday { get; set; } = false;
        /// <summary>
        /// Schedule is active on Wednesday
        /// </summary>
        public bool Wednesday { get; set; } = false;
        /// <summary>
        /// Schedule is active on Thursday
        /// </summary>
        public bool Thursday { get; set; } = false;
        /// <summary>
        /// Schedule is active on Friday
        /// </summary>
        public bool Friday { get; set; } = false;
        /// <summary>
        /// Schedule is active on Saturday
        /// </summary>
        public bool Saturday { get; set; } = false;

        /// <summary>
        /// Initialiez a new instance of the <see cref="Recurrence"/> class
        /// </summary>
        /// <param name="recurrence">A string containing the days of week separated by |</param>
        public Recurrence(string recurrence)
        {
            SetRecurrence(recurrence);
        }

        /// <summary>
        /// Set the Recurrence based on a string
        /// </summary>
        /// <param name="scheduleRecurrence">A string containing the days of week separated by |</param>
        private void SetRecurrence(string scheduleRecurrence)
        {
            // Make sure there are valid contents
            if (String.IsNullOrEmpty(scheduleRecurrence))
                throw new Exception("Invalid schedule reference. Valid types are SUN|MON|TUE|WED... or NYYYYYN (starting sunday).");

            bool formatNYYYYYN = false;

            // Check the types of contents
            if (scheduleRecurrence.Length == 7)
            {
                // Assume it is NYYYYYN
                formatNYYYYYN = true;

                // It could be "NYYYYYN"
                foreach (var c in scheduleRecurrence)
                {
                    if (c != 'Y' && c != 'y' &&
                        c != 'N' && c != 'n')
                    {
                        formatNYYYYYN = false;
                        break;
                    }
                }
            }

            // Parse accordingly to format
            if (formatNYYYYYN)
            {
                // Format NYYYYYYN
                if (scheduleRecurrence[0] == 'Y' || scheduleRecurrence[0] == 'y') Sunday = true;
                if (scheduleRecurrence[1] == 'Y' || scheduleRecurrence[1] == 'y') Monday = true;
                if (scheduleRecurrence[2] == 'Y' || scheduleRecurrence[2] == 'y') Tuesday = true;
                if (scheduleRecurrence[3] == 'Y' || scheduleRecurrence[3] == 'y') Wednesday = true;
                if (scheduleRecurrence[4] == 'Y' || scheduleRecurrence[4] == 'y') Thursday = true;
                if (scheduleRecurrence[5] == 'Y' || scheduleRecurrence[5] == 'y') Friday = true;
                if (scheduleRecurrence[6] == 'Y' || scheduleRecurrence[6] == 'y') Saturday = true;
            }
            else
            {
                // Format SUN|MON|TUE...
                var tempString = scheduleRecurrence.ToUpper();
                var recurrenceArray = tempString.Split('|');

                foreach (var item in recurrenceArray)
                {
                    if (item == "SUN") Sunday = true;
                    else if (item == "MON") Monday = true;
                    else if (item == "TUE") Tuesday = true;
                    else if (item == "WED") Wednesday = true;
                    else if (item == "THU") Thursday = true;
                    else if (item == "FRI") Friday = true;
                    else if (item == "SAT") Saturday = true;
                }
            }

        }

    }

}

using Diassoft.Scheduler.Configuration;
using System;
using System.Collections.Generic;

namespace Diassoft.Scheduler.NetCore.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var scheduler = new Scheduler.Scheduler<ColorConsoleEvent>();
            scheduler.SchedulerStarted += Scheduler_SchedulerStarted;
            scheduler.SchedulerStopping += Scheduler_SchedulerStopping;
            scheduler.SchedulerStopped += Scheduler_SchedulerStopped;
            //scheduler.SchedulerPaused += Scheduler_SchedulerPaused;
            //scheduler.SchedulerResumed += Scheduler_SchedulerResumed;
            scheduler.EventTimeReached += Scheduler_EventTimeReached;

            scheduler.TriggerEventsAsynchronously = false;

            var newEvent01 = new ColorConsoleEvent();
            var newEvent02 = new ColorConsoleEvent();

            var newEvent01Schedule = new EveryScheduleConfiguration(5, "YYYYYYY", new DateTime(1900, 1, 1, 7, 0, 0), new DateTime(1900, 1, 1, 23, 59, 59));
            var newEvent02Schedule = new MultiScheduleConfiguration("8:15 PM|22:00", "YYYYYYY");

            scheduler.AddEventToSchedule("EVENT01", newEvent01, newEvent01Schedule);
            scheduler.AddEventToSchedule("EVENT02", newEvent02, newEvent02Schedule);

            scheduler.Start();
            System.Threading.Thread.Sleep(1000);

            Console.WriteLine();

            while (true)
            {
                Console.WriteLine("======================================================================");
                Console.WriteLine("Scheduler Testing");
                Console.WriteLine("======================================================================");
                Console.WriteLine();
                Console.WriteLine("A = Display Current Schedule");
                Console.WriteLine("B = Display Current Schedule (only first 6 records)");
                Console.WriteLine("C = Display Current Schedule For Event 01");
                Console.WriteLine("D = Display Current Schedule For Event 02");
                Console.WriteLine("E = Display Current Schedule For Internal Events");
                Console.WriteLine("X = Stop Scheduler");

                Console.WriteLine();

                Console.Write("Enter your test and press ENTER => ");

                if (scheduler.Status == SchedulerStatus.Paused)
                    scheduler.Resume(true);

                var response = Console.ReadLine().ToUpper();

                scheduler.Pause(true);

                if (response == "A")
                {
                    DisplaySchedule(scheduler.GetCurrentSchedule());
                }
                else if (response == "B")
                {
                    DisplaySchedule(scheduler.GetCurrentSchedule(6));
                }
                else if (response == "C")
                {
                    DisplaySchedule(scheduler.GetCurrentSchedule("EVENT01"));
                }
                else if (response == "D")
                {
                    DisplaySchedule(scheduler.GetCurrentSchedule("EVENT02"));
                }
                else if (response == "E")
                {
                    var data = scheduler.GetCurrentSchedule(true);

                    for (int i = data.Count - 1; i >= 0; i--)
                    {
                        if (data[i].EventGroup != EventGroups.Internal) data.RemoveAt(i);
                    }

                    DisplaySchedule(data);
                }
                else if (response == "X")
                {
                    scheduler.Stop();
                    break;
                }
                else
                {
                    Console.WriteLine("ERROR: Invalid Option");
                    Console.WriteLine();
                }

            }

            Console.WriteLine("Wait events to inform that the test ended, and press ENTER");
            Console.ReadLine();

        }

        private static void Scheduler_SchedulerResumed(object sender, EventArgs e)
        {
            Console.WriteLine("Scheduler Resumed");
        }

        private static void Scheduler_SchedulerPaused(object sender, EventArgs e)
        {
            Console.WriteLine("Scheduler Paused");
        }

        private static void Scheduler_SchedulerStarted(object sender, EventArgs e)
        {
            Console.WriteLine("Scheduler Started");
        }

        private static void Scheduler_SchedulerStopping(object sender, EventArgs e)
        {
            Console.WriteLine("Scheduler Stopping...");
        }

        private static void Scheduler_SchedulerStopped(object sender, EventArgs e)
        {
            Console.WriteLine("Scheduler Stopped");
        }

        // Event called when the schedule is reached
        private static void Scheduler_EventTimeReached(object sender, Scheduler.ScheduleEventArgs e)
        {

            // Time to execute the event
            if (e.EventInfo.Event is ColorConsoleEvent colorEvent)
            {
                if (ColorConsoleEvent.CurrentSet == 1)
                {
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else if (ColorConsoleEvent.CurrentSet == 2)
                {
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else if (ColorConsoleEvent.CurrentSet == 3)
                {
                    Console.BackgroundColor = ConsoleColor.Gray;
                    Console.ForegroundColor = ConsoleColor.Black;
                }
                else if (ColorConsoleEvent.CurrentSet == 4)
                {
                    Console.BackgroundColor = ConsoleColor.DarkMagenta;
                    Console.ForegroundColor = ConsoleColor.White;
                }

                var originalCursorLeft = Console.CursorLeft;
                var originalCursorTop = Console.CursorTop;

                var contents = "Event Triggered: " + ColorConsoleEvent.CurrentSet.ToString().PadLeft(4, '0');
                Console.SetCursorPosition(Console.BufferWidth - contents.Length - 1, originalCursorTop);
                Console.Write(contents);

                ColorConsoleEvent.CurrentSet++;
                if (ColorConsoleEvent.CurrentSet > 4)
                    ColorConsoleEvent.CurrentSet = 1;

                Console.ResetColor();

                Console.SetCursorPosition(originalCursorLeft, originalCursorTop);
            }
        }

        private static void DisplaySchedule(List<ScheduledEventExecutionInfo> events)
        {
            Console.WriteLine();
            Console.WriteLine("Name                           Execution Date/Time");
            Console.WriteLine("------------------------------ -------------------");

            foreach (var item in events)
            {
                Console.WriteLine("{0} {1}", item.Name.PadRight(30, ' ').Substring(0, 30), item.ExecutionDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
            }

            Console.WriteLine();
        }

        // Temporary class to be used as an event for the scheduler
        private class ColorConsoleEvent
        {
            public ColorConsoleEvent()
            {

            }

            public static byte CurrentSet { get; set; } = 1;
        }
    }
}

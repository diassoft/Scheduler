# Scheduler - v1.3.0

[![nuget](https://img.shields.io/nuget/v/Diassoft.Scheduler.svg)](https://www.nuget.org/packages/Diassoft.Scheduler/) 
![GitHub release](https://img.shields.io/github/release/diassoft/Scheduler.svg)
![NuGet](https://img.shields.io/nuget/dt/Diassoft.Scheduler.svg)
![license](https://img.shields.io/github/license/diassoft/Scheduler.svg)

The Scheduler is a component that allows the configuration of specific dates times for events to be triggered.

An event could be anything. The Scheduler does not actually execute the event itself, instead, it just raises a notification to inform the consumers of the class that certain event should be processed.

## In this repository

* [Getting Started](#getting-started)
* [Concepts](#concepts)
    * [Understanding Events](#understanding-events)
    * [Understanding the Schedule Configuration](#understanding-the-schedule-configuration)
    * [Understanding the Schedule Recurrence](#understanding-the-schedule-recurrence)
* [Using the Scheduler](#using-the-scheduler)
* [Scheduler Documentation](https://diassoft.github.io/Scheduler_v1000)

### Recent Changes

This is a list containing the most relevant changes that the developers need to be aware when upgrading versions.

| Version | Notes |
| :-- | :-- |
| v1.3.0 | Corrected the Nuget Package References for the `Microsoft.Extensions.Logging.Abstractions` package. |
| v1.2.0 | Replaced the `Scheduler<T>` with the `EventScheduler<T>` class |

## Getting Started

The first thing you have to do is to define the event to be triggered. You can use any class for the event, including basic classes such as `System.String`. However, you may want to create a custom class to represent the event.

The code below represents an event that contains a number representing a color.

```cs
public class ColorEvent
{
    public ColorEvent(byte colorCode)
    {
        ColorCode = colorCode % 2;
    }

    public byte ColorCode { get; set; } = 0;

    // 0 = Black
    // 1 = White
}
```

Next, you have to define the dates and times the event should be scheduled.
This is done by setting a `ScheduleConfiguration`. For now, we will be using the `EveryScheduleConfiguration` class to define a schedule that runs every 10 seconds.

> The concept of the `ScheduleConfiguration` is discussed at the [Understanding the Schedule Configuration](#understanding-the-schedule-configuration) section.

```cs
// Creates a schedule that runs every 10 seconds, on all days of the week, from 7:00 AM thru 10:00 PM
var scheduleConfiguration = new EveryScheduleConfiguration(10, "YYYYYYY", new DateTime(1900, 1, 1, 7, 0, 0), new DateTime(1900, 1, 1, 21, 59, 59));
```

The last step is to create a scheduler, add the events, and listen to the `EventTimeReached` event.

Here is the full code example:

```cs
// Defines the scheduler
var scheduler = new EventScheduler<ColorEvent>();
scheduler.EventTimeReached += Scheduler_EventTimeReached;

// Create the proper events
var colorEventBlack = new ColorEvent(0);
var colorEventWhite = new ColorEvent(1);

// Create the schedule
var scheduleConfigurationBlack = new EveryScheduleConfiguration(10, "YYYYYYY", new DateTime(1900, 1, 1, 7, 0, 0), new DateTime(1900, 1, 1, 21, 59, 59));
var scheduleConfigurationWhite = new EveryScheduleConfiguration(60, "YYYYYYY", new DateTime(1900, 1, 1, 7, 0, 0), new DateTime(1900, 1, 1, 21, 59, 59));

// Add the events to the schedule
scheduler.AddEventToSchedule("COLOREVENTBLACK", colorEventBlack, scheduleConfigurationBlack);
scheduler.AddEventToSchedule("COLOREVENTWHITE", colorEventWhite, scheduleConfigurationWhite);

// Start the scheduler service (you can start the service and add the events later too)
scheduler.Start();

// Function called when an event time is reached
private static void Scheduler_EventTimeReached(object sender, Scheduler.ScheduleEventArgs e)
{
    // Time to execute the event
    if (e.EventInfo.Event is ColorEvent colorEvent)
    {
        // The event is raised, now this class will do whatever is necessary to do with the event
        if (colorEvent.Color == 0)
            Console.WriteLine("Black color at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        else if (colorEvent.Color == 1)
            Console.WriteLine("White color at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        else
            Console.WriteLine("Invalid Color");
    }
}
```

# Concepts

This section describes with more details the main objects of the component.
For a complete explanation of each class, refer to the [Scheduler Documentation](https://diassoft.github.io/Scheduler_v1000).

## Understanding Events

Events can be anything that has to be processed at a certain date and time. 
The `EventScheduler` component is responsible for triggering an alert that it's time to process an event. However, the responsibility of processing the event is on the consumer of the `EventScheduler` component. 

The easiest way to create an event is to use a `System.String` as the event, and have the consumer do what is necessary with the string.

A better approach is to have a custom class that not only hold values used by the event itself, but also has code to perform a specific activity. 

In the code below, we created an event that checks for files in a specific folder and, if there are files there, an email will be sent.

```cs
public class CheckFilesAndEmailEvent
{
    public string Path { get; set; }
    public string Email { get; set; }

    public CheckFilesAndEmailEvent(string path, string email) 
    {
        Path = path;
        Email = email;
    }

    public void ProcessEvent() 
    {
        // Check for files in the folder
        if (System.IO.Directory.GetFiles(Path).Length > 0 )
        {
            // There are files on the folder, send email
            
            ... logic to send email goes here
        }
    }
}
```

On the consumer side, you would only have to call the `ProcessEvent` method to process the event at the proper time.

Here is the full example:

```cs
// Defines the scheduler
var scheduler = new EventScheduler<CheckFilesAndEmailEvent>();
scheduler.EventTimeReached += Scheduler_EventTimeReached;

// Create the proper events
var checkErrorsFolderEvent = new CheckFilesAndEmailEvent(@"C:\Software\Errors", "admin@software.com");

// Create the schedule (run every 3600 seconds - 1 hour)
var scheduleConfigurationErrors = new EveryScheduleConfiguration(3600, "YYYYYYY", new DateTime(1900, 1, 1, 7, 0, 0), new DateTime(1900, 1, 1, 21, 59, 59));

// Add the evente to the schedule
scheduler.AddEventToSchedule("CHECKERRORS", checkErrorsFolderEvent, scheduleConfigurationErrors);

// Start the scheduler service (you can start the service and add the events later too)
scheduler.Start();

// Function called when an event time is reached
private static void Scheduler_EventTimeReached(object sender, Scheduler.ScheduleEventArgs e)
{
    // Time to execute the event
    if (e.EventInfo.Event is CheckFilesAndEmailEvent checkEvent)
    {
        checkEvent.ProcessEvent();
    }
}
```

## Understanding the Schedule Configuration

The Schedule Configuration dictates when the event should be triggered. 

The following base classes are available to be used.

| Base Class | Description |
| :-- | :-- |
| `ScheduleConfiguration` | A base class that must be inherited by all types of configuration. Only the very basic properties are available in this class. |
| `RecurrentScheduleConfiguration` | A base class that provides an instance of the `Recurrence` class to define which days of the week the schedule should be executed. |

The following implemented classes provide the basic needs of a scheduler.

| Class | Base | Description | 
| :-- | :-- | :-- |
| `EveryScheduleConfiguration` | `RecurrentScheduleConfiguration` | A class that provides information for an event to be executed every X seconds. |
| `MultiScheduleConfiguration` | `RecurrentScheduleConfiguration` | A class that provides information for an event to be executed multiple times during a day. |

> Other types of schedule configuration classes are currently in development and will be released soon. 

## Understanding the Schedule Recurrence

The `Recurrence` class defines the days of the week the event should be triggered.
We may have events that only need to be triggered during week days. Also, we may have events that only need to be triggered during weekends. 

All these scenarios can be configured thru the `Recurrence` class. The classes inheriting from `RecurrentScheduleConfiguration` already have built in functionality that defines the days of the week an event should be triggered.

The `Recurrence` class has a method named `SetRecurrence`, which accepts two different input formats.

| Format | Description |
| :-- | :-- |
| NYYYYYN | Each character represents one day of the week, starting on Sunday. Therefore, a value of `NYYYYYN` means that Sunday and Saturdays are disabled, but Monday thru Friday are enabled. |
| MON\|TUE\|WED\|THU\|FRI | Each element represents one day of the week. |

When inheriting from the `RecurrentScheduleConfiguration`, make sure to have a constructor that accepts the recurrence parameter. If no recurrence is informed, the system will use the constant `RecurrentScheduleConfiguration.DEFAULT_RECURRENCE` to configure the recurrence.

# Using the Scheduler

The main class of the Scheduler component is the `EventScheduler<T>` class.

This class provides a complete scheduler, which will run on its own thread and, at the proper date and time, will call a method to inform that it's time to process a certain event.

The main methods of the `EventScheduler` class are listed below:

| Method | Description |
| :-- | :-- |
| `Start` | Starts the Scheduling Services |
| `Stop` | Stops the Scheduling Services |
| `Pause` | Pauses the Scheduling Services |
| `Resume` | Resumes the Scheduling Services |
| `GetCurrentSchedule` | Returns a list with all the events to be executed |
| `AddEventToSchedule` | Adds an event to the schedule |
| `RemoveEventFromSchedule` | Removes an event from the schedule |
| `ReplaceEventSchedule` | Replaces the `ScheduleConfiguration` of an event with a new one |

Listen to the event `EventTimeReached` to know when it's time to process an event.

The following code demonstrates how to initialize the scheduler:

```cs
// Defines the scheduler
var scheduler = new EventScheduler<System.String>();
scheduler.EventTimeReached += Scheduler_EventTimeReached;

// Start the scheduler service (you can start the service and add the events later too)
scheduler.Start();

... add code here to create events to be triggered

// Function called when an event time is reached
private static void Scheduler_EventTimeReached(object sender, Scheduler.ScheduleEventArgs e)
{
    ... process the event
}
```

> A list of all the methods of the `EventScheduler` class can be found on the [Scheduler Documentation](https://diassoft.github.io/Scheduler_v1000).

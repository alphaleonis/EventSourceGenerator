# What is EventSource?

The [`EventSource`](https://msdn.microsoft.com/en-us/library/system.diagnostics.tracing.eventsource%28v=vs.110%29.aspx) class in either .NET or the NuGet package [Microsoft Event Source Library](https://www.nuget.org/packages/Microsoft.Diagnostics.Tracing.EventSource) offers a great way to utilize [ETW](https://msdn.microsoft.com/en-us/library/windows/desktop/bb968803%28v=vs.85%29.aspx) tracing from your .NET applications.  

For more information about how to use the System.Diagnostics.Tracing.EventSource (or Microsoft.Diagnostics.Tracing.EventSource)  classes to implement sematic logging utilizing ETW, see for example the following links:
* [MSDN](https://msdn.microsoft.com/en-us/library/system.diagnostics.tracing.eventsource%28v=vs.110%29.aspx)
* [Vance Morrison's Blog](http://blogs.msdn.com/b/vancem/archive/tags/eventsource/)
* [Kathleen Dollard's Blog](http://blogs.msmvps.com/kathleen/2013/08/16/summary-of-etw-support-in-net/)
* [Vance Morrison's video on Channel 9](https://channel9.msdn.com/Series/PerfView-Tutorial/PerfView-Tutorial-8-Generating-Your-Own-Events-with-EventSources)
* [Pluralsight course on ETW](http://www.pluralsight.com/courses/event-tracing-windows-etw-dotnet)

## Problems

There are however a few problems when it comes to writing implementations of the EventSource class; 

* You need to write the implementation for each event-method, and the event ID used in the method must match that in the [`[Event]`](https://msdn.microsoft.com/en-us/library/system.diagnostics.tracing.eventattribute%28v=vs.110%29.aspx) attribute on the method.  Implementations of these methods look identical, and these should preferrably be automatically generated.
* For the highest performance you can write overloads of the WriteEvent methods using unsafe code. This involves writing complicated code using pointers and some other usafe features not commonly used in normal .NET development.  It would be great if the neccessary overloads were automatically generated.   
* You cannot create a base class with common events to be used by multiple EventSources. Any base class must be abstract and contain no event-methods. 

The Alphaleonis C# Event Source generator attempts to overcome these problems by generating code based on an abstract class that you define, without having to manually write the implementation for any method.

# The Event Source Generator

The Alphaleonis C# Event Source Generator is a Visual Studio 2015 extension that provides a CustomTool that can be applied to a C#-file and then generates the actual implementation of an EventSource deriving from an abstract class defined by you, where all event methods are defined as abstract methods.  It will also generate any WriteEvent-overloads required, to avoid using the less performant generic overload in the base class.

The extension supports both the `System.Diagnostics.Tracing.EventSource` in .NET 4.5 and 4.6, as well as the NuGet package `Microsoft.Diagnostics.Tracing.EventSource`. The NuGet package is required for channel support in .NET 4.5. (.NET 4.6 supports this out of the box). 

## Template Attributes
Since we are not allowed to use the `[Event]` or `[EventSource]` attributes on the abstract class and its methods, the extension instead uses another set of attributes that are essentially clones of these attributes, but named `[TemplateEvent]` and `[TemplateEventSource]` instead.

These attributes should be identical to the corresponding attributes of the version of the EventSource that you are using, with the exception of the `TemplateEventSourceAttribute` which contains the addition of some properties that control the generation process. A NuGet package providing source code implementations of these attributes are available, and can be installed in the project where  you need it. The attributes may be defined in any namespace, the extension only looks at the actual name of the attribute, so if you want to move these to another namespace, feel free to do so.

### TemplateEventSourceAttribute
The following extra properties are added to the `TemplateEventSourceAttribute` when compared to the `EventSourceAttribute`:

Property|Description
--------|-----------
`TargetClassName` | Specifies the name of the class to generate. Defaults to the same name as the template class, with "Impl" appended, or if the template class name ends with "Base" or "Template", the default target class name is the name of the template class without the "Base" or "Template" suffix.
`SuppressSingletonGeneration` |  If set to `false` (the default) the generated class will include a static property called `Log` exposing a singleton instance of the EventSource class. If set to `true`, this property is not generated.  
`Net45EventSourceCompatibility` | Specifies whether to generate an implementation compatible with the .NET 4.5 EventSource. This prevents usage of some parameter types in any generated WriteEvent overloads. Set this to `true` if you are using the System.Diagnostics.Tracing.EventSource class in .NET 4.5.X. Set it to `false` if you are using the NuGet version of EventSource or the EventSource in .NET 4.6 or later. The default is `true`.

## Usage

The basic usage is probably best illustrated with a simple example:

### Simple Example

Create a new `.cs` file and set the *Custom Tool* property to `EventSourceGenerator`. Then create the following class inside it:

```C#
[TemplateEventSource(Name = "MyCompany-MyEventSource", Net45EventSourceCompatibility = false)]
public abstract class MyEventSourceTemplate : EventSource
{
    [TemplateEvent(10, Channel = EventChannel.Admin, Level = EventLevel.Informational, Message = "Some message: {0}")]
    public abstract void TestEvent1(string Message);
}
```

When this file is saved, a new `.cs` file is created as a child of this file with the `.g.cs` extension, containing the following implementation (somewhat simplified here for clarity):
```C#
[EventSource(Name = "MyCompany-MyEventSource")]
[System.CodeDom.Compiler.GeneratedCode("Alphaleonis EventSource Generator", "0.2.0.0")]
public sealed class MyEventSource : MyEventSourceTemplate
{
    private static readonly MyEventSource s_instance = new MyEventSource();

    public static MyEventSourceTemplate Log
    {
        get
        {
            return s_instance;
        }
    }

    [Event(10, Channel = EventChannel.Admin, Level = EventLevel.Informational, Message = "Some message: {0}")]
    public override void TestEvent1(string Message)
    {
        if (IsEnabled())
        {
            WriteEvent(10, Message);
        }
    }
}
```

Note that since we did not specify the `TargetClassName` property of the `TemplateEventSourceAttribute`, the class is automatically named based on the abstract class defining it.  

The `TemplateEventAttribute` used is simply copied over and renamed into the `EventAttribute` that the `EventSource` infrastructure expects, same thing with the `TemplateEventSourceAttribute`.

We can aslo see that the generated class is sealed, and it derives from our abstract base class, and it exposes the static `Log` property to access the singleton instance, as per the recommendations.

### Advanced Example

This is a more complicated example illustrating some more advanced features of the Event Source generator. 
First we define a class in a file that is *not* using the Custom Tool, since this is just a common base class and we do not want any code directly generated from it:
```C#
public abstract class CommonEventSourceBase : EventSource
{
    [TemplateEvent(65000, Channel = EventChannel.Admin, Keywords = CommonKeywords.CommonKeyword1, Level = EventLevel.Error, Message = "Common event: {0}", Opcode = EventOpcode.Start, Task = CommonTasks.CommonTask1)]
    public abstract void CommonEvent1(string Message);

    public class CommonKeywords
    {
        public const EventKeywords CommonKeyword1 = (EventKeywords)0x8000;
        public const EventKeywords CommonKeyword2 = (EventKeywords)0x8001;
    }

    public class CommonTasks
    {
        public const EventTask CommonTask1 = (EventTask)0x8000;
    }
}
``` 

Then we define our template class and assign it the `EventSourceGenerator` Custom Tool. This class derives from the previously defined abstract class, which will incorporate the events from that class as well in the generated code.

```C#
[TemplateEventSource(Name = "MyCompany-MyEventSource", Net45EventSourceCompatibility = false, TargetClassName = "MyRenamedEventSource")]
public abstract class MyEventSourceTemplate : CommonEventSourceBase
{
    [TemplateEvent(10, Channel = EventChannel.Admin, Level = EventLevel.Informational, Message = "Some message: {0}", Keywords = MyKeywords.MyKeyword, Task = MyTasks.MyTask, Opcode = MyOpcodes.MyOpcode)]
    public abstract void TestEvent1(string Message, DateTime time, TimeSpan timeSpan);

    public class MyKeywords
    {
        public const EventKeywords MyKeyword = (EventKeywords)0x1;
    }

    public class MyTasks
    {
        public const EventTask MyTask = (EventTask)88;
    }

    public class MyOpcodes
    {
        public const EventOpcode MyOpcode = (EventOpcode)88;
    }
}
```

This will generate the following code (somewhat simplified):

```C#
public sealed class MyRenamedEventSource : MyEventSourceTemplate
{
    private static readonly MyRenamedEventSource s_instance = new MyRenamedEventSource();

    public static MyEventSourceTemplate Log
    {
        get
        {
            return s_instance;
        }
    }

    [NonEvent]
    public override void TestEvent1(string Message, DateTime time, TimeSpan timeSpan)
    {
        if (IsEnabled())
        {
            TestEvent1(Message, time, timeSpan.ToString("c"));
        }
    }

    [Event(10, Channel = EventChannel.Admin, Level = EventLevel.Informational, Message = "Some message: {0}", Keywords = MyKeywords.MyKeyword, Task = MyTasks.MyTask, Opcode = MyOpcodes.MyOpcode)]
    private void TestEvent1(string Message, DateTime time, string timeSpan)
    {
        WriteEvent(10, Message, time, timeSpan);
    }

    [Event(65000, Channel = EventChannel.Admin, Keywords = CommonKeywords.CommonKeyword1, Level = EventLevel.Error, Message = "Common event: {0}", Opcode = EventOpcode.Start, Task = CommonTasks.CommonTask1)]
    public override void CommonEvent1(string Message)
    {
        if (IsEnabled())
        {
            WriteEvent(65000, Message);
        }
    }

    [NonEvent]
    private unsafe void WriteEvent(int eventId, string arg0, DateTime arg1, string arg2)
    {
        if (arg0 == null)
        {
            arg0 = string.Empty;
        }

        long fileTime1 = arg1.ToFileTimeUtc();
        if (arg2 == null)
        {
            arg2 = string.Empty;
        }

        EventData* descrs = stackalloc EventData[3];
        fixed (char* str2 = arg2)
        {
            fixed (char* str0 = arg0)
            {
                descrs[0].DataPointer = (IntPtr)str0;
                descrs[0].Size = (arg0.Length + 1) * 2;
                descrs[1].DataPointer = (IntPtr)(&fileTime1);
                descrs[1].Size = 8;
                descrs[2].DataPointer = (IntPtr)str2;
                descrs[2].Size = (arg2.Length + 1) * 2;
                WriteEventCore(eventId, 3, descrs);
            }
        }
    }

    public static class Keywords
    {
        public const EventKeywords MyKeyword = MyKeywords.MyKeyword;
        public const EventKeywords CommonKeyword1 = CommonKeywords.CommonKeyword1;
    }

    public static class Opcodes
    {
        public const EventOpcode MyOpcode = MyOpcodes.MyOpcode;
    }

    public static class Tasks
    {
        public const EventTask MyTask = MyTasks.MyTask;
        public const EventTask CommonTask1 = CommonTasks.CommonTask1;
    }
}
```

This example illustrates most of the features of the Event Source Generators. We are defining Keywords, Tasks and Opcodes, and we can see how this generates the corresponding static classes in the generated Event Source, which allows for proper manifest generation, for example with the *EventRegister* tool included in the Microsoft.Diagnostics.Tracing.EventSource NuGet package.

The template method `TestEvent1` that we created however, has been decorated with the `NonEvent` attribute, which may seem a bit strange. But we can also see that a private method has been created for this event, but with the `TimeSpan` parameter changed to `string`. The public method handles the translation of this parameter to a string. The `TimeSpan` type is currently the only type for which this is supported, and this is not recommended for high-volume events since the ToString() call will take a little time. But it may be handy on some occasions. 

We also see that an overload has been created for `WriteEvent`, matching the parameters of our event method, since such an overload did not exist in the base class. 
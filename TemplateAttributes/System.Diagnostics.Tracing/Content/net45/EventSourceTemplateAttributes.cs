using System;

namespace System.Diagnostics.Tracing
{
   /// <summary>
   /// Allows customizing defaults and specifying localization support for the event source 
   /// template class to which it is applied.
   /// </summary>
   [AttributeUsage(AttributeTargets.Class)]
   public sealed class TemplateEventSourceAttribute : Attribute
   {
      public TemplateEventSourceAttribute()
      {
         AllowUnsafeCode = true;
      }

      /// <summary>
      ///   Overrides the default (calculated) Guid of an EventSource type. Explicitly defining a GUID
      ///   is discouraged, except when upgrading existing ETW providers to using event sources.
      /// </summary>
      public string Guid { get; set; }

      /// <summary>
      ///   EventSources support localization of events. The names used for events, opcodes, tasks,
      ///   keywords and maps can be localized to several languages if desired. This works by creating
      ///   a ResX style string table (by simply adding a 'Resource File' to your project). This
      ///   resource file is given a name e.g. 'DefaultNameSpace.ResourceFileName' which can be passed
      ///   to the ResourceManager constructor to read the resources. This name is the value of the
      ///   LocalizationResources property. If LocalizationResources property is non-null, then
      ///   EventSource will look up the localized strings for events by using the following resource
      ///   naming scheme
      ///   * event_EVENTNAME
      ///   * task_TASKNAME
      ///   * keyword_KEYWORDNAME
      ///   * map_MAPNAME
      ///   where the capitalized name is the name of the event, task, keyword, or map value that
      ///   should be localized. Note that the localized string for an event corresponds to the Message
      ///   string, and can have {0} values which represent the payload values.
      /// </summary>
      public string LocalizationResources { get; set; }

      /// <summary>
      ///   Overrides the ETW name of the event source (which defaults to the class name)
      /// </summary>
      public string Name { get; set; }

      /// <summary>
      ///   Gets or sets the name of the class to generate. Defaults to the same name as the template
      ///   class, with "Impl" appended, or if the template class name ends with "Base" or "Template", the default
      ///   target class name is the name of the template class without the "Base" or "Template" suffix.
      /// </summary>
      public string TargetClassName { get; set; }

      /// <summary>
      ///   Gets or sets a value indicating whether to suppress generation of the static singleton
      ///   property "Log" which is normally generated.  
      /// </summary>
      public bool SuppressSingletonGeneration { get; set; }

      /// <summary>
      /// Gets or sets a value indicating whether unsafe will be generated for WriteEvent overloads.
      /// Allowing this increases performance of high-volume events for which a WriteEvent overload is
      /// not available in the base class, but it requires that your project is configured to allow
      /// unsafe code.  The default is <see langword="true"/>.
      /// </summary>
      /// <value>true if allow unsafe code, false if not.</value>
      public bool AllowUnsafeCode { get; set; }
   }

   [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
   public sealed class TemplateEventAttribute : Attribute
   {
      /// <summary>
      ///   Initializes a new instance of the the TemplateEventAttribute class with the specified event
      ///   identifier.
      /// </summary>
      /// <param name="eventId">The event identifier. This value should be between 0 and 65535.</param>
      public TemplateEventAttribute(int eventId)
      {
         EventId = eventId;
      }

      /// <summary>
      ///   Gets the identifier for the event.
      /// </summary>
      public int EventId { get; }

      /// <summary>Gets or sets the keywords for the event.</summary>
      /// <value>A bitwise combination of the enumeration values.</value>
      public EventKeywords Keywords { get; set; }

      /// <summary>Gets or sets the level for the event.</summary>
      /// <value>One of the enumeration values that specifies the level for the event.</value>
      public EventLevel Level { get; set; }

      /// <summary>Gets or sets the message for the event. The message for the event.</summary>
      /// <value>The message for the event.</value>
      public string Message { get; set; }

      /// <summary>Gets or sets the operation code for the event.</summary>
      /// <value>One of the enumeration values that specifies the operation code.</value>
      public EventOpcode Opcode { get; set; }

      /// <summary>Gets or sets the task for the event.</summary>
      /// <value>The task for the event.</value>
      public EventTask Task { get; set; }

      /// <summary>Gets or sets the version of the event.</summary>
      /// <value>The version of the event.</value>
      public byte Version { get; set; }
   }
}

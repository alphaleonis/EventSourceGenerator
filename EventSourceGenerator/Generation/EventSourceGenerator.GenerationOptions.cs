using System;
using System.Collections.Generic;
using System.Linq;

namespace Alphaleonis.EventSourceGenerator
{
   partial class EventSourceGenerator
   {
      private class GenerationOptions
      {
         public string TargetClassName { get; set; }
         public bool SuppressSingletonGeneration { get; set; }
         public bool Net45EventSourceCompatibility { get; set; }
      }
   }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Alphaleonis.EventSourceGenerator
{
   partial class EventSourceGenerator
   {
      private class GenerationOptions
      {
         public GenerationOptions(Version targetFrameworkVersion)
         {
            AllowUnsafeCode = true;
            TargetFrameworkVersion = targetFrameworkVersion;
         }

         public string TargetClassName { get; set; }
         public bool SuppressSingletonGeneration { get; set; }
         public bool AllowUnsafeCode { get; set; }
         public Version TargetFrameworkVersion { get; }
      }
   }
}

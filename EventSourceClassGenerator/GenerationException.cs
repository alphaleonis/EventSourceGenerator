using System;
using System.Collections.Generic;
using System.Linq;
using CodeElement = EnvDTE.CodeElement;
using TextPoint = EnvDTE.TextPoint;

namespace Alphaleonis.EventSourceClassGenerator
{
   [Serializable]
   public class GenerationException : Exception
   {
      public int Line { get; set; }
      public int Column { get; set; }
      public GenerationException()
      {
         Line = -1;
         Column = -1;
      }
      public GenerationException(string message, CodeElement location)
         : base(message)
      {
         TextPoint textPoint = null;
         if (location != null)
         {
            textPoint = location.StartPoint;
         }

         Line = textPoint == null ? -1 : textPoint.Line;
         Column = textPoint == null ? -1 : textPoint.DisplayColumn;
      }

      public GenerationException(string message) : base(message) { }
      public GenerationException(string message, Exception inner) : base(message, inner) { }
      protected GenerationException(
       System.Runtime.Serialization.SerializationInfo info,
       System.Runtime.Serialization.StreamingContext context)
         : base(info, context) { }
   }
}

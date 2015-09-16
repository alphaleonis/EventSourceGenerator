using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Alphaleonis.EventSourceGenerator
{
   partial class EventSourceGenerator
   {
      /// <summary>Information about the template event method.</summary>
      private class TemplateEventMethodInfo
      {
         public TemplateEventMethodInfo(IReadOnlyList<SyntaxNode> attributes, int eventId)
         {
            EventId = eventId;
            Attributes = attributes;
         }

         public int EventId { get; }

         public IReadOnlyList<SyntaxNode> Attributes { get; }
      }
   }
}

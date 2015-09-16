using Alphaleonis.Vsx;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Alphaleonis.EventSourceGenerator
{
   partial class EventSourceGenerator
   {
      private class EventSourceTypeInfo
      {
         /// <summary>
         /// Initializes a new instance of the <see cref="EventSourceTypeInfo"/> class.
         /// </summary>
         public EventSourceTypeInfo(SemanticModel semanticModel, INamedTypeSymbol eventSourceClass)
         {
            if (eventSourceClass == null)
               throw new ArgumentNullException("eventSourceClass", "eventSourceClass is null.");

            INamedTypeSymbol eventSourceAttributeType = eventSourceClass.ContainingNamespace.GetTypeMembers("EventSourceAttribute", 0).Single();
            INamedTypeSymbol eventAttributeType = eventSourceClass.ContainingNamespace.GetTypeMembers("EventAttribute", 0).Single();

            EventSourceClass = eventSourceClass;
            EventAttributeType = eventAttributeType;
            EventSourceAttributeType = eventSourceAttributeType;
            EventSourceNamespace = eventSourceClass.ContainingNamespace;
            EventKeywordsType = semanticModel.Compilation.GetTypeByMetadataName(EventSourceNamespace.GetFullName() + ".EventKeywords");
            EventOpcodeType = semanticModel.Compilation.GetTypeByMetadataName(EventSourceNamespace.GetFullName() + ".EventOpcode");
            EventTaskType = semanticModel.Compilation.GetTypeByMetadataName(EventSourceNamespace.GetFullName() + ".EventTask");

            WriteEventOverloads = eventSourceClass.GetAllMembers()
               .OfType<IMethodSymbol>()
               .Where(method => method.Name.Equals("WriteEvent"))
               // Only add methods that have at least one parameter, and the first parameter is of type Int32.
               .Where(method => method.Parameters.Length >= 1 && method.Parameters[0].Type.SpecialType == SpecialType.System_Int32)
               // Only add methods that do not contain 'object' parameters or params parameters.
               .Where(method => method.Parameters.All(parameter => !parameter.IsParams && parameter.Type.SpecialType != SpecialType.System_Object))
               // Skip the first parameter, which should be (int eventId)
               .Select(method => method.Parameters.Skip(1).ToImmutableArray())
               .ToImmutableArray();
         }

         public ImmutableArray<ImmutableArray<IParameterSymbol>> WriteEventOverloads { get; }
         public INamedTypeSymbol EventSourceClass { get; }
         public INamedTypeSymbol EventAttributeType { get; }
         public INamedTypeSymbol EventSourceAttributeType { get; }
         public INamespaceSymbol EventSourceNamespace { get; }
         public INamedTypeSymbol EventKeywordsType { get; }
         public INamedTypeSymbol EventOpcodeType { get; }
         public INamedTypeSymbol EventTaskType { get; }
      }
   }
}

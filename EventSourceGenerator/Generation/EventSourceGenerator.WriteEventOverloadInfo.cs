using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Alphaleonis.EventSourceClassGenerator
{
   partial class EventSourceGenerator
   {
      /// <summary>Information about the an overload of the WriteEvent method..</summary>
      private class WriteEventOverloadInfo
      {
         private readonly GenerationOptions m_options;
         private readonly IMethodSymbol m_sourceMethod;

         public WriteEventOverloadInfo(IMethodSymbol sourceMethod, GenerationOptions options, ParameterConverterCollection converters)
         {
            if (sourceMethod == null)
               throw new ArgumentNullException("sourceMethod", "sourceMethod is null.");

            if (options == null)
               throw new ArgumentNullException("options", "options is null.");

            if (converters == null)
               throw new ArgumentNullException("converters", "converters is null.");

            m_sourceMethod = sourceMethod;
            m_options = options;
            Parameters = m_sourceMethod.Parameters.Select(p => new EventParameterInfo(p, options, converters.TryGetConverter(p.Type))).ToImmutableArray();
         }

         public ImmutableArray<EventParameterInfo> Parameters { get; }
                  
         public bool NeedsConverter
         {
            get
            {
               return Parameters.Any(p => p.HasConverter) && Parameters.All(p => p.IsSupported);
            }
         }

         public IMethodSymbol SourceMethod
         {
            get
            {
               return m_sourceMethod;
            }
         }
      }
   }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Alphaleonis.EventSourceGenerator
{
   partial class EventSourceGenerator
   {
      private class ParameterConverterCollection : IEnumerable<IParameterConverter>
      {
         private readonly Dictionary<ITypeSymbol, IParameterConverter> m_converters = new Dictionary<ITypeSymbol, IParameterConverter>();

         public ParameterConverterCollection(SemanticModel semanticModel, SyntaxGenerator syntaxGenerator)
         {
            IParameterConverter timeSpanConverter = new TimeSpanParameterConverter(semanticModel, syntaxGenerator);
            m_converters.Add(timeSpanConverter.SourceType, timeSpanConverter);
         }


         public IParameterConverter TryGetConverter(ITypeSymbol type)
         {
            IParameterConverter converter;
            if (m_converters.TryGetValue(type, out converter))
               return converter;

            return null;
         }

         public IEnumerator<IParameterConverter> GetEnumerator()
         {
            return m_converters.Values.GetEnumerator();
         }

         IEnumerator IEnumerable.GetEnumerator()
         {
            return GetEnumerator();
         }
      }
   }
}

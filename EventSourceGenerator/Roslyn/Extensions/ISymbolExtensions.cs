using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Alphaleonis.Vsx
{
   public static class ISymbolExtensions
   {
      public static ISymbol OverriddenMember(this ISymbol symbol)
      {
         switch (symbol.Kind)
         {
            case SymbolKind.Event:
               return ((IEventSymbol)symbol).OverriddenEvent;

            case SymbolKind.Method:
               return ((IMethodSymbol)symbol).OverriddenMethod;

            case SymbolKind.Property:
               return ((IPropertySymbol)symbol).OverriddenProperty;
         }

         return null;
      }

      public static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, ITypeSymbol attributeType)
      {
         return symbol.GetAttributes().Where(attr => attr.AttributeClass.Equals(attributeType));
      }

      public static AttributeData GetAttribute(this ISymbol symbol, ITypeSymbol attributeType)
      {
         IEnumerable<AttributeData> attributes = symbol.GetAttributes(attributeType);
         if (attributes.Skip(1).Any())
            throw new InvalidOperationException($"Multiple attributes of type {attributeType.GetFullName()} were found on {symbol.Name}.");

         return attributes.FirstOrDefault();

      }
   }
}

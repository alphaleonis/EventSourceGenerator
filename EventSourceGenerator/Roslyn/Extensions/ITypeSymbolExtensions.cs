using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Alphaleonis.Vsx
{
   public static class ITypeSymbolExtensions
   {
      public static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol type)
      {
         var current = type;
         while (current != null)
         {
            yield return current;
            current = current.BaseType;
         }
      }

      public static IEnumerable<INamedTypeSymbol> GetBaseTypes(this ITypeSymbol type)
      {
         var current = type.BaseType;
         while (current != null)
         {
            yield return current;
            current = current.BaseType;
         }
      }

      public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol type)
      {
         return type.GetBaseTypesAndThis().SelectMany(t => t.GetMembers()).RemoveOverriddenSymbolsWithinSet();
      }

      public static bool IsNullable(this ITypeSymbol symbol)
      {
         return symbol?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
      }

      private static IEnumerable<T> RemoveOverriddenSymbolsWithinSet<T>(this IEnumerable<T> symbols) where T : ISymbol
      {
         HashSet<ISymbol> overriddenSymbols = new HashSet<ISymbol>();

         foreach (var symbol in symbols)
         {
            if (symbol.OverriddenMember() != null && !overriddenSymbols.Contains(symbol.OverriddenMember()))
            {
               overriddenSymbols.Add(symbol.OverriddenMember());
            }
         }

         return symbols.Where(s => !overriddenSymbols.Contains(s));
      }

      public static bool IsPrimitive(this ITypeSymbol symbol)
      {
         switch (symbol.SpecialType)
         {
            case SpecialType.System_Boolean:
            case SpecialType.System_Char:
            case SpecialType.System_SByte:
            case SpecialType.System_Byte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Decimal:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_String:
               return true;
            default:
               return false;               
         }
      }

      public static bool IsEnumType(this ITypeSymbol type)
      {
         return type.IsValueType && type.TypeKind == TypeKind.Enum;
      }

      public static bool IsGuid(this ITypeSymbol type)
      {         
         return type.IsValueType && type.GetFullName().Equals("System.Guid") && type.ContainingAssembly.Name == "mscorlib";            
      }

      public static bool IsByteArray(this ITypeSymbol type)
      {
         return type.TypeKind == TypeKind.Array && ((IArrayTypeSymbol)type).ElementType.SpecialType == SpecialType.System_Byte;
      }
   }
}

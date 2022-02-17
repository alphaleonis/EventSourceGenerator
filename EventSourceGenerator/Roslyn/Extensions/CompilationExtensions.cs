using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Alphaleonis.Vsx.Roslyn
{
   public static class CompilationExtensions
   {
      public static INamedTypeSymbol RequireTypeByMetadataName(this Compilation compilation, string fullyQualifiedMetadataName)
      {
         INamedTypeSymbol type = compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);
         if (type == null)
            throw new TypeNotFoundException($"Could not find the type named \"{fullyQualifiedMetadataName}\". Are you missing an assembly reference?");

         return type;
      }

      public static INamedTypeSymbol RequireType<T>(this Compilation compilation)
      {
         return RequireType(compilation, typeof(T));
      }

      public static INamedTypeSymbol RequireType(this Compilation compilation, Type type)
      {
         return RequireTypeByMetadataName(compilation, type.FullName);
      }
   }





}

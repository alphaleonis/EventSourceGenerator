using Alphaleonis.Vsx;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alphaleonis.EventSourceGenerator
{
   partial class EventSourceGenerator
   {
      private class EventParameterInfo
      {
         private readonly IParameterConverter m_converter;
         private readonly IParameterSymbol m_parameter;
         private readonly GenerationOptions m_options;

         public EventParameterInfo(IParameterSymbol parameter, GenerationOptions options, IParameterConverter converter)
         {
            m_converter = converter;
            m_options = options;
            m_parameter = parameter;
         }

         public IParameterSymbol Parameter
         {
            get
            {
               return m_parameter;
            }
         }

         public ITypeSymbol TargetType
         {
            get
            {
               return HasConverter ? Converter.TargetType : Parameter.Type;
            }
         }

         public bool IsNativelySupported
         {
            get
            {
               var type = m_parameter.Type;
               switch (type.SpecialType)
               {
                  // The following types are supported.
                  case SpecialType.System_Boolean:
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
                  case SpecialType.System_DateTime:
                     return true;

                  // The following types are supported in NuGet EventSource
                  case SpecialType.System_Char:
                  case SpecialType.System_IntPtr:
                     return m_options.TargetFrameworkVersion.CompareTo(new Version(4, 6)) >= 0;                        
               }

               if (type.GetFullName() == typeof(Guid).FullName && type.ContainingAssembly.Name == typeof(Guid).Assembly.GetName().Name)
                  return true;

               if (type.TypeKind == TypeKind.Enum)
                  return true;

               if (type.TypeKind == TypeKind.Array && ((IArrayTypeSymbol)type).ElementType.SpecialType == SpecialType.System_Byte)
                  return true;

               // byte* is a supported type in NuGet EventSource.
               if (m_options.TargetFrameworkVersion.CompareTo(new Version(4, 6)) >= 0 && type.TypeKind == TypeKind.Pointer && ((IPointerTypeSymbol)type).PointedAtType.SpecialType == SpecialType.System_Byte)
                  return true;

               return false;
            }
         }

         public bool IsSupported
         {
            get
            {
               return IsNativelySupported || HasConverter;
            }
         }

         public bool HasConverter
         {
            get
            {
               return Converter != null;
            }
         }

         public IParameterConverter Converter
         {
            get
            {
               return m_converter;
            }
         }
      }
   }
}

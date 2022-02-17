using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Alphaleonis.Vsx.Roslyn
{
   public static class AttributeParser
   {
      /// <summary>
      /// Creates instance of type <typeparamref name="T"/> from the attribute specified, attempting to
      /// match the constructor and then assigning its properties from the named arguments. If no matching constructor
      /// is found, or a property is missing or has a value of an incompatible type an exception is thrown.
      /// </summary>
      /// <exception cref="CodeGeneratorException">Thrown when a Text File Generator error condition
      /// occurs.</exception>
      /// <typeparam name="T">The type of the instance to create.</typeparam>
      /// <param name="attribute">The attribute data from which to get constructor arguments and property values.</param>
      /// <remarks>Arguments of type System.Type is converted to INamedTypeSymbol</remarks>
      /// <returns>A new instance of T.</returns>
      public static T CreateInstanceFromAttribute<T>(AttributeData attribute)
      {
         T result;
         try
         {
            result = (T)Activator.CreateInstance(typeof(T), attribute.ConstructorArguments.Select(arg => arg.Value).ToArray());
         }
         catch (MissingMethodException)
         {
            throw new CodeGeneratorException(attribute, $"The attribute {attribute.AttributeClass.Name} uses an unsupported constructor. Valid constructors are: " +
               GetValidConstructors<T>(attribute));
         }
         catch (AmbiguousMatchException)
         {
            var argTypes = attribute.ConstructorArguments.Select(a => ResolveAttributeArgumentType(a)).ToArray();
            if (argTypes.Any(t => t == null))
               throw new CodeGeneratorException(attribute, $"Could not resolve the arguments of the attribute {attribute.AttributeClass.Name}, since the constructor arguments are ambiguous. Valid constructors are: {GetValidConstructors<T>(attribute)}");

            var ctorInfo = typeof(T).GetConstructor(argTypes);
            if (ctorInfo == null)
               throw new CodeGeneratorException(attribute, $"Could not resolve the arguments of the attribute {attribute.AttributeClass.Name}, since the constructor arguments are ambiguous.  Valid constructors are: {GetValidConstructors<T>(attribute)}");

            result = (T)ctorInfo.Invoke(attribute.ConstructorArguments.Select(arg => arg.Value).ToArray());
         }

         foreach (var namedArg in attribute.NamedArguments)
         {
            PropertyInfo property = typeof(T).GetProperty(namedArg.Key);
            if (property == null)
            {
               throw new CodeGeneratorException(attribute, $"Unsupported named argument {namedArg.Key} of attribute {attribute.AttributeClass.Name}. Supported arguments are: " +
                  String.Join(", ", typeof(T).GetProperties().Where(p => p.CanWrite).Select(p => $"{(p.PropertyType.Equals(typeof(INamedTypeSymbol)) ? typeof(Type).Name : p.PropertyType.Name)} {p.Name}")));
            }

            if (!property.CanWrite)
            {
               throw new CodeGeneratorException(attribute, $"Unsupported named argument {namedArg.Key} of attribute {attribute.AttributeClass.Name}. Supported arguments are: " +
                  String.Join(", ", typeof(T).GetProperties().Where(p => p.CanWrite).Select(p => $"{(p.PropertyType.Equals(typeof(INamedTypeSymbol)) ? typeof(Type).Name : p.PropertyType.Name)} {p.Name}")));
            }

            if (namedArg.Value.IsNull && property.PropertyType.IsValueType)
            {
               throw new CodeGeneratorException(attribute, $"Unsupported value for named argument {namedArg.Key} of attribute {attribute.AttributeClass.Name}. The value cannot be null.");
            }


            object value;

            if (!property.PropertyType.IsAssignableFrom(namedArg.Value.Value.GetType()))
            {
               if (namedArg.Value.Type.TypeKind == TypeKind.Enum && property.PropertyType.IsEnum)
               {
                  string enumFieldName = GetEnumNameFromValue(namedArg.Value.Type, namedArg.Value.Value);
                  try
                  {
                     value = Enum.Parse(property.PropertyType, enumFieldName);
                  }
                  catch
                  {
                     throw new CodeGeneratorException(attribute, $"The enum value {namedArg.Value.Type.Name}.{enumFieldName} is not supported. Valid values for this enum are: {StringUtils.Join(Enum.GetNames(property.PropertyType), ", ", " and ", "'")}.");
                  }


               }
               else
               {
                  throw new CodeGeneratorException(attribute, $"Unsupported value for named argument {namedArg.Key} of attribute {attribute.AttributeClass.Name}. The value must be assignable to " +
                     (property.PropertyType.Equals(typeof(INamedTypeSymbol)) ? typeof(Type).Name : property.PropertyType.Name));
               }
            }
            else
            {
               value = namedArg.Value.Value;
            }

            property.SetValue(result, value);
         }

         PropertyInfo attributeNameProperty = result.GetType().GetProperty("AttributeName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
         if (attributeNameProperty != null && attributeNameProperty.CanWrite && attributeNameProperty.PropertyType.IsAssignableFrom(typeof(string)))
         {
            attributeNameProperty.SetValue(result, attribute.AttributeClass.Name);
         }

         return result;
      }

      private static string GetEnumNameFromValue(ITypeSymbol enumType, object value)
      {
         if (enumType.TypeKind != TypeKind.Enum)
            throw new ArgumentException($"{nameof(enumType)} is not an enum type.");

         var field = enumType.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(fld => fld.HasConstantValue && EqualityComparer<object>.Default.Equals(fld.ConstantValue, value));
         if (field == null)
            throw new InvalidOperationException($"Unable to get enum name from enum of type {enumType.Name} for value {value}");

         return field.Name;
      }

      private static Type ResolveAttributeArgumentType(TypedConstant typedConstant)
      {
         if (typedConstant.Value != null)
            return typedConstant.Value.GetType();

         if (typedConstant.Kind == TypedConstantKind.Type)
            return typeof(INamedTypeSymbol);


         switch (typedConstant.Type.SpecialType)
         {
            case SpecialType.System_Object:
               return typeof(object);

            case SpecialType.System_Boolean:
               return typeof(bool);

            case SpecialType.System_Char:
               return typeof(char);

            case SpecialType.System_SByte:
               return typeof(sbyte);

            case SpecialType.System_Byte:
               return typeof(byte);

            case SpecialType.System_Int16:
               return typeof(Int16);

            case SpecialType.System_UInt16:
               return typeof(UInt16);

            case SpecialType.System_Int32:
               return typeof(Int32);

            case SpecialType.System_UInt32:
               return typeof(UInt32);

            case SpecialType.System_Int64:
               return typeof(Int64);

            case SpecialType.System_UInt64:
               return typeof(UInt64);

            case SpecialType.System_Decimal:
               return typeof(Decimal);

            case SpecialType.System_Single:
               return typeof(Single);

            case SpecialType.System_Double:
               return typeof(Double);

            case SpecialType.System_String:
               return typeof(string);

            case SpecialType.System_IntPtr:
               return typeof(IntPtr);

            case SpecialType.System_UIntPtr:
               return typeof(UIntPtr);

            case SpecialType.System_DateTime:
               return typeof(DateTime);

            default:
               return null;
         }

      }

      private static string GetValidConstructors<T>(AttributeData attribute)
      {
         return StringUtils.Join(typeof(T).GetConstructors().Select(ctor => $"{attribute.AttributeClass.Name}({String.Join(", ", ctor.GetParameters().Select(p => $"{(p.ParameterType == typeof(INamedTypeSymbol) ? typeof(Type).Name : p.ParameterType.Name)} {p.Name}"))})"), 
            ", ", " and ");
      }
   }
}

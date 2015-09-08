using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Alphaleonis.EventSourceClassGenerator
{
   static class CodeModelExtensions
   {
      public static IEnumerable<CodeAttributeArgument> Arguments(this CodeAttribute2 attr)
      {
         return attr.Arguments.OfType<CodeAttributeArgument>();
      }

      public static CodeClass BaseClass(this CodeClass codeClass)
      {
         return codeClass.Bases.Classes().FirstOrDefault();
      }      

      public static IEnumerable<CodeImport> Imports(this CodeElements elements)
      {
         return elements.OfType<CodeImport>();
      }

      public static IEnumerable<CodeClass2> Classes(this CodeElements elements)
      {
         return elements.OfType<CodeClass2>();
      }

      public static IEnumerable<CodeInterface2> Interfaces(this CodeElements elements)
      {
         return elements.OfType<CodeInterface2>();
      }

      public static IEnumerable<CodeStruct2> Structs(this CodeElements elements)
      {
         return elements.OfType<CodeStruct2>();
      }
      
      public static IEnumerable<CodeAttribute2> Attributes(this CodeElements elements)
      {
         return elements.OfType<CodeAttribute2>();
      }

      public static IEnumerable<CodeEnum> Enums(this CodeElements elements)
      {
         return elements.OfType<CodeEnum>();
      }

      public static IEnumerable<CodeFunction2> Methods(this CodeElements elements)
      {
         return elements.OfType<CodeFunction2>();
      }

      public static IEnumerable<CodeNamespace> Namespaces(this CodeElements elements)
      {
         return elements.OfType<CodeNamespace>();
      }

      public static IEnumerable<CodeParameter2> Parameters(this CodeElements elements)
      {
         return elements.OfType<CodeParameter2>();
      }

      public static IEnumerable<CodeProperty2> Properties(this CodeElements elements)
      {
         return elements.OfType<CodeProperty2>();
      }

      public static IEnumerable<CodeDelegate2> Delegates(this CodeElements elements)
      {
         return elements.OfType<CodeDelegate2>();
      }

      public static string FullTypeName(this CodeTypeRef type)
      {
         switch (type.TypeKind)
         {
            case vsCMTypeRef.vsCMTypeRefArray:
               return type.ElementType.FullTypeName() + "[]";               
            case vsCMTypeRef.vsCMTypeRefVoid:
               return "void";
            case vsCMTypeRef.vsCMTypeRefPointer:
               return type.ElementType.FullTypeName() + "*";               
            case vsCMTypeRef.vsCMTypeRefString:
            case vsCMTypeRef.vsCMTypeRefObject:
            case vsCMTypeRef.vsCMTypeRefOther:
            case vsCMTypeRef.vsCMTypeRefCodeType:
            case vsCMTypeRef.vsCMTypeRefByte:
            case vsCMTypeRef.vsCMTypeRefChar:
            case vsCMTypeRef.vsCMTypeRefShort:
            case vsCMTypeRef.vsCMTypeRefInt:
            case vsCMTypeRef.vsCMTypeRefLong:
            case vsCMTypeRef.vsCMTypeRefFloat:
            case vsCMTypeRef.vsCMTypeRefDouble:
            case vsCMTypeRef.vsCMTypeRefDecimal:
            case vsCMTypeRef.vsCMTypeRefBool:
            case vsCMTypeRef.vsCMTypeRefVariant:
            default:
               return type.AsFullName;
         }
      }

      private static vsCMElement GetElementType<T>()
      {
         vsCMElement elementType;
         if (typeof(CodeClass).IsAssignableFrom(typeof(T)))
            elementType = vsCMElement.vsCMElementClass;
         else if (typeof(CodeInterface).IsAssignableFrom(typeof(T)))
            elementType = vsCMElement.vsCMElementInterface;
         else if (typeof(CodeEnum).IsAssignableFrom(typeof(T)))
            elementType = vsCMElement.vsCMElementEnum;
         else if (typeof(CodeAttribute).IsAssignableFrom(typeof(T)))
            elementType = vsCMElement.vsCMElementAttribute;
         else if (typeof(CodeDelegate).IsAssignableFrom(typeof(T)))
            elementType = vsCMElement.vsCMElementDelegate;
         else if (typeof(CodeFunction).IsAssignableFrom(typeof(T)))
            elementType = vsCMElement.vsCMElementFunction;
         else if (typeof(CodeNamespace).IsAssignableFrom(typeof(T)))
            elementType = vsCMElement.vsCMElementNamespace;
         else if (typeof(CodeParameter).IsAssignableFrom(typeof(T)))
            elementType = vsCMElement.vsCMElementParameter;
         else if (typeof(CodeProperty).IsAssignableFrom(typeof(T)))
            elementType = vsCMElement.vsCMElementProperty;
         else if (typeof(CodeStruct).IsAssignableFrom(typeof(T)))
            elementType = vsCMElement.vsCMElementStruct;
         else if (typeof(CodeImport).IsAssignableFrom(typeof(T)))
            elementType = vsCMElement.vsCMElementImportStmt;
         else
            throw new NotSupportedException("OfType cannot retrieve types of type " + typeof(T).Name);

         return elementType;
      }
      //private static IEnumerable<T> OfType<T>(this CodeElements elements)
      //{
      //   vsCMElement elementType = GetElementType<T>();
      //   return elements.Cast<CodeElement>().Where(e => e.Kind == elementType).Cast<T>();
      //}

      public static IEnumerable<T> FindRecursively<T>(this CodeElements elements, bool includeExternalTypes = false)
      {
         return GetAllCodeElementsOfType(elements, GetElementType<T>(), includeExternalTypes).Cast<T>();
      }

      /// <summary>
      /// Searches a given collection of CodeElements recursively for objects of the given elementType.
      /// </summary>
      /// <param name="elements">Collection of CodeElements to recursively search for matching objects in.</param>
      /// <param name="elementType">Objects of this CodeModelElement-Type will be returned.</param>
      /// <param name="includeExternalTypes">If set to true objects that are not part of this solution are retrieved, too. E.g. the INotifyPropertyChanged interface from the System.ComponentModel namespace.</param>
      /// <returns>A list of CodeElement objects matching the desired elementType.</returns>
      public static IEnumerable<CodeElement> GetAllCodeElementsOfType(this CodeElements elements, vsCMElement elementType, bool includeExternalTypes)
      {
         var ret = new List<CodeElement>();

         foreach (CodeElement elem in elements)
         {
            // iterate all namespaces (even if they are external)
            // > they might contain project code
            if (elem.Kind == vsCMElement.vsCMElementNamespace)
            {
               ret.AddRange(GetAllCodeElementsOfType(((CodeNamespace)elem).Members, elementType, includeExternalTypes));
            }
            // if its not a namespace but external
            // > ignore it
            else if (elem.InfoLocation == vsCMInfoLocation.vsCMInfoLocationExternal
                    && !includeExternalTypes)
               continue;
            // if its from the project
            // > check its members
            else if (elem.IsCodeType)
            {
               ret.AddRange(GetAllCodeElementsOfType(((CodeType)elem).Members, elementType, includeExternalTypes));
            }

            // if this item is of the desired type
            // > store it
            if (elem.Kind == elementType)
               ret.Add(elem);
         }

         return ret;
      }

      /// <summary>
      /// Gets all methods and functions directly implemented by a code class
      /// </summary>
      public static IEnumerable<CodeFunction2> GetMethods(this CodeClass codeClass)
      {
         return GetAllCodeElementsOfType(codeClass.Members, vsCMElement.vsCMElementFunction, true).OfType<CodeFunction2>();
      }

      /// <summary>
      /// Gets all methods and functions directly implemented by a code class
      /// </summary>
      public static IEnumerable<CodeFunction> GetMethods(this CodeInterface codeClass)
      {
         return GetAllCodeElementsOfType(codeClass.Members, vsCMElement.vsCMElementFunction, true).OfType<CodeFunction>();
      }
      
      /// <summary>
      /// Recursively gets all interfaces that a given CodeClass implements either itself, one of its base classes or as an inherited interface.
      /// Respects partial classes.
      /// </summary>
      public static IEnumerable<CodeInterface> GetAllImplementedInterfaces(this CodeClass codeClass)
      {
         var implInterfaces = new List<CodeInterface>();

         foreach (var partialClass in GetPartialClasses(codeClass))
         {
            foreach (CodeInterface ci in GetImplementedInterfaces(partialClass))
            {
               implInterfaces.Add(ci);
               implInterfaces.AddRange(GetAllBaseInterfaces(ci));
            }

            var baseClass = partialClass.BaseClass();
            if (baseClass != null)
               implInterfaces.AddRange(GetAllImplementedInterfaces(baseClass));
         }

         return implInterfaces.Distinct(new CodeInterfaceEqualityComparer());
      }

      /// <summary>
      /// Gets all interfaces a given CodeClass implements directly.
      /// </summary>
      public static IEnumerable<CodeInterface> GetImplementedInterfaces(this CodeClass codeClass)
      {
         return GetAllCodeElementsOfType(codeClass.ImplementedInterfaces, vsCMElement.vsCMElementInterface, true).OfType<CodeInterface>();
      }

      /// <summary>
      /// Recursively gets all interfaces a given CodeInterface implements/inherits from.
      /// </summary>
      public static IEnumerable<CodeInterface> GetAllBaseInterfaces(this CodeInterface codeInterface)
      {
         var ret = new List<CodeInterface>();

         var directBases = GetBaseInterfaces(codeInterface);
         ret.AddRange(directBases);

         foreach (var baseInterface in directBases)
            ret.AddRange(GetAllBaseInterfaces(baseInterface));

         return ret;
      }

      /// <summary>
      /// Returns a list of all base interfaces a given CodeInterface implements/inherits from.
      /// </summary>
      public static IEnumerable<CodeInterface> GetBaseInterfaces(this CodeInterface codeInterface)
      {
         return codeInterface.Bases.OfType<CodeInterface>();
      }

      /// <summary>
      /// Recursively gets all base classes of the given CodeClass respecting partial implementations.
      /// </summary>
      public static IEnumerable<CodeClass> GetAllBaseClasses(this CodeClass codeClass)
      {
         var ret = new List<CodeClass>();

         // iterate all partial implementations
         foreach (CodeClass partialClass in GetPartialClasses(codeClass))
         {
            // climb up the inheritance tree
            var cc = partialClass;
            while (cc != null)
            {
               cc = cc.BaseClass();
               if (cc != null) ret.Add(cc);
            }
         }

         return ret;
      }      

      /// <summary>
      /// Checks if the given CodeClass has partial implementations.
      /// </summary>
      /// <returns> A list of the partial CodeClasses that form the given class. If the class is not partial, the class itself is returned in the list.</returns>
      public static IEnumerable<CodeClass> GetPartialClasses(this CodeClass codeClass)
      {
         var classParts = new List<CodeClass>();

         // partial classes are a new feature and only available in the CodeClass2 interface
         // check if the given class is a CodeClass2
         if (codeClass is EnvDTE80.CodeClass2)
         {
            // yes, it is
            EnvDTE80.CodeClass2 cc2 = (EnvDTE80.CodeClass2)codeClass;
            // check if it consists of multiple partial classes
            if (cc2.ClassKind != EnvDTE80.vsCMClassKind.vsCMClassKindPartialClass)
               // no > only return the class itself
               classParts.Add(cc2);
            else
               // yes > add all partial classes
               classParts.AddRange(cc2.PartialClasses.OfType<CodeClass>());
         }
         else
            // this is no CodeClass2 > return itself
            classParts.Add(codeClass);

         return classParts;
      }

      #region Private classes

      private class CodeInterfaceEqualityComparer : IEqualityComparer<CodeInterface>
      {
         public bool Equals(CodeInterface x, CodeInterface y)
         {
            var n1 = x.FullName;
            var n2 = y.FullName;
            return n1 == n2;
         }

         public int GetHashCode(CodeInterface obj)
         {
            return obj.FullName.GetHashCode();
         }
      }
      private class CodeFunctionEqualityComparer : IEqualityComparer<CodeFunction>
      {
         public bool Equals(CodeFunction x, CodeFunction y)
         {
            var n1 = x.FullName;
            var n2 = y.FullName;
            return n1 == n2;
         }

         public int GetHashCode(CodeFunction obj)
         {
            return obj.FullName.GetHashCode();
         }
      }
      #endregion
   }
}

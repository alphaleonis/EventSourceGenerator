using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VSLangProj80;

using ECodeClass = EnvDTE80.CodeClass2;
using ECodeInterface = EnvDTE80.CodeInterface2;
using ECodeFunction = EnvDTE80.CodeFunction2;
using ECodeAttribute = EnvDTE80.CodeAttribute2;
using ECodeParameter = EnvDTE80.CodeParameter2;
using ECodeNamespace = EnvDTE.CodeNamespace;
using CodeElement = EnvDTE80.CodeElement2;
using CodeElements = EnvDTE.CodeElements;
using ProjectItem = EnvDTE.ProjectItem;
using TextPoint = EnvDTE.TextPoint;
using vsCMPart = EnvDTE.vsCMPart;
using EFileCodeModel = EnvDTE80.FileCodeModel2;
using ECodeAttributeArgument = EnvDTE80.CodeAttributeArgument;
using Microsoft.CSharp;
using System.Globalization;
using ECodeTypeRef = EnvDTE.CodeTypeRef;
using ECodeType = EnvDTE.CodeType;
using EnvDTE;

namespace Alphaleonis.EventSourceClassGenerator
{
   [ComVisible(true)]
   [Guid("C0B0D7EA-A1E9-46B7-B7B2-93EC5249A719")]
   [CodeGeneratorRegistration(typeof(EventSourceClassGenerator), "C# Event Source Class Generator", vsContextGuids.vsContextGuidVCSProject, GeneratesDesignTimeSource = true)]
   [ProvideObject(typeof(EventSourceClassGenerator))]
   public class EventSourceClassGenerator : BaseCodeGeneratorWithSite
   {
#pragma warning disable 0414
      //The name of this generator (use for 'Custom Tool' property of project item)
      internal static string name = "EventSourceClassGenerator";
#pragma warning restore 0414

      /// <summary>
      /// Initializes a new instance of the <see cref="EvtSrcGen"/> class.
      /// </summary>
      public EventSourceClassGenerator()
      {
      }

      private static bool IsDerivedFromEventSource(ECodeClass c)
      {
         return c.GetAllBaseClasses().Any(bc => bc.FullName.Equals("System.Diagnostics.Tracing.EventSource") ||
              bc.FullName.Equals("Microsoft.Diagnostics.Tracing.EventSource"));
      }

      private void GenerateCode(string inputFileContent, TextWriter writer)
      {
         ReportProgress(0, 100);

         EnvDTE80.DTE2 dte = GetService(typeof(EnvDTE80.DTE2)) as EnvDTE80.DTE2;
         ProjectItem projectItem = GetProjectItem();
         Project project = GetProject();
         VSProject2 vsProject = project.Object as VSProject2;


         List<Project> referencedProjects = new List<Project>();
         foreach (VSLangProj2.Reference2 reference in vsProject.References)
         {
            if (reference.SourceProject != null)
            {
               referencedProjects.Add(reference.SourceProject);
            }
         }

         var fileCodeModel = (EnvDTE80.FileCodeModel2)projectItem.FileCodeModel;


         var classes = fileCodeModel.CodeElements.FindRecursively<ECodeClass>().ToArray();
         if (classes.Length == 0)
         {
            ReportWarning("No classes were defined in the file.", 0, 0);
            return;
         }

         SourceCodeGenerator gen = new SourceCodeGenerator(writer);

         // Get any classes that should participate in generation. These are classes inheriting from EventSource.
         var sourceClasses = classes.Where(c => IsDerivedFromEventSource(c));

         gen.WriteGeneratedCodeWarningComment();

         // Emit global usings
         foreach (var import in fileCodeModel.CodeElements.Imports())
         {
            gen.WriteImport(import);
         }

         // Generate all source classes.
         foreach (var sourceClassEntry in sourceClasses.AsSmartEnumerable())
         {
            GenerateEventSourceClass(gen, sourceClassEntry.Value, referencedProjects);
         }
      }

      public static void GenerateEventSourceClass(SourceCodeGenerator gen, ECodeClass sourceClass, IReadOnlyList<Project> referencedProjects)
      {

         string targetClassName = GetTargetClassName(sourceClass);

         if (!sourceClass.IsAbstract)
         {
            throw new GenerationException(String.Format("The class {0} must be abstract to participate in EventSource generation.", sourceClass.FullName), (CodeElement)sourceClass);
         }

         var eventSourceBaseClass = GetEventSourceBase(sourceClass);
         var writeEventMethods = eventSourceBaseClass.GetMethods()
            .Where(method => method.Name.Equals("WriteEvent") && method.Parameters.Parameters()
               .All(p => p.Type.FullTypeName() != "System.Object[]"));

         List<ECodeTypeRef[]> requiredWriteEventOverloads = new List<ECodeTypeRef[]>();
         var preDefinedWriteEventOverloads = writeEventMethods.Select(m => m.Parameters.Parameters().Skip(1).Select(p => p.Type).ToArray()).ToArray();

         ConstantsCollection constants = new ConstantsCollection();

         // Create the namespace for the class.
         gen.WriteLine();
         using (gen.StartNamespace(sourceClass.Namespace.FullName))
         {
            // Add any usings in this namespace.
            var namespaceImports = sourceClass.Namespace.Members.Imports();
            foreach (var import in namespaceImports)
               gen.WriteImport(import);

            if (namespaceImports.Any())
               gen.WriteLine();

            // Emit class declaration
            // Emit class attributes
            gen.WriteGeneratedCodeWarningComment();
            WriteAttributes(gen, sourceClass, sourceClass.Attributes.Attributes(), constants);

            // Write class declaration header
            gen.WriteAccess(sourceClass.Access);
            gen.Write("sealed partial class ");
            gen.Write(targetClassName);
            gen.Write(" : ");
            gen.WriteLine(sourceClass.FullName);

            // Write class Contents
            using (gen.StartBlock())
            {
               GenerateSingletonDefinition(gen, targetClassName);

               // Write Event Methods
               using (gen.StartRegion("Event Methods"))
               {
                  // Loop through all abstract methods with a TemplateEvent attribute in the source class.
                  ECodeClass[] classes = new[] { sourceClass }.Concat(sourceClass.GetAllBaseClasses().Cast<ECodeClass>().Where(c => IsDerivedFromEventSource(c))).ToArray();
                  for (int i = 0; i < classes.Length; i++)
                  {
                     var clas = classes[i];

                     if (clas.InfoLocation == vsCMInfoLocation.vsCMInfoLocationExternal)
                     {
                        var externalClasses = referencedProjects.Select(p => p.CodeModel.CodeTypeFromFullName(clas.FullName) as ECodeClass)
                           .Where(c => c != null && c.InfoLocation != vsCMInfoLocation.vsCMInfoLocationExternal).ToArray();
                           //.SelectMany(p => p.CodeModel.CodeElements.FindRecursively<ECodeClass>(false).Where(c => c.FullName == clas.FullName)).ToArray();
                        if (externalClasses.Length == 0)
                           throw new GenerationException(String.Format("Unable to locate the base class {0}.", clas.FullName), (CodeElement)clas);

                        if (externalClasses.Length > 1)
                           throw new GenerationException(String.Format("Found multiple definitions of base class {0}.", clas.FullName), (CodeElement)clas);

                        classes[i] = externalClasses[0];
                     }                 
                  }

                  foreach (var methodEntry in classes.SelectMany(c => c.GetMethods().Where(m => m.Attributes.Attributes().Any(attr => attr.Name.Equals("TemplateEvent"))).AsSmartEnumerable()))
                  {
                     var method = methodEntry.Value;

                     // Check if this method needs a WriteEvent overload, and if so, add it to the collection.
                     var nonSupportedParameter = method.Parameters.Parameters().FirstOrDefault(p => !IsSupportedEventParameter(p.Type));
                     if (nonSupportedParameter != null)
                        throw new GenerationException(String.Format("The parameter type {0} is not supported for an event method.", nonSupportedParameter.Type.FullTypeName()), (CodeElement)nonSupportedParameter);

                     var signature = method.Parameters.Parameters().Select(p => p.Type).ToArray();
                     if (!preDefinedWriteEventOverloads.Any(ol => SignaturesEqual(ol, signature)) && !requiredWriteEventOverloads.Any(ol => SignaturesEqual(ol, signature)))
                     {
                        requiredWriteEventOverloads.Add(signature);
                     }

                     if (!methodEntry.IsFirst)
                        gen.WriteLine();

                     GenerateEventMethod(gen, sourceClass, method, false, constants);
                  }
               }

               GenerateConstants(gen, constants, GetEventSourceBase(sourceClass).Namespace.FullName);

               if (requiredWriteEventOverloads.Count > 0)
               {
                  using (gen.StartRegion("WriteEvent Overloads"))
                  {
                     foreach (var signature in requiredWriteEventOverloads)
                     {
                        GenerateWriteEventOverload(gen, GetEventSourceBase(sourceClass).Namespace, signature);
                     }
                  }
               }
            }
         }
      }

      private static void GenerateConstants(SourceCodeGenerator gen, ConstantsCollection constants, string eventSourceNamespace)
      {
         using (gen.StartRegion("Tasks"))
         {
            gen.WriteLine("public static class Tasks");
            using (gen.StartBlock())
            {
               foreach (var entry in constants.Tasks)
               {
                  gen.WriteLine("public const {0}.EventTask {1} = ({0}.EventTask){2};",
                     eventSourceNamespace, entry.Key, entry.Value);
               }
            }
         }
         gen.WriteLine();
         using (gen.StartRegion("Opcodes"))
         {
            gen.WriteLine("public static class Opcodes");
            using (gen.StartBlock())
            {
               foreach (var entry in constants.Opcodes)
               {
                  gen.WriteLine("public const {0}.EventOpcode {1} = ({0}.EventOpcode){2};",
                     eventSourceNamespace, entry.Key, entry.Value);
               }
            }
         }

         gen.WriteLine();
         using (gen.StartRegion("Keywords"))
         {
            gen.WriteLine("public static class Keywords");
            using (gen.StartBlock())
            {
               foreach (var entry in constants.Keywords)
               {
                  gen.WriteLine("public const {0}.EventKeywords {1} = ({0}.EventKeywords){2};",
                     eventSourceNamespace, entry.Key, entry.Value);
               }
            }
         }
      }

      internal static void GenerateEventMethod(SourceCodeGenerator gen, ECodeClass sourceClass, ECodeFunction method, bool asPrivateImpl, ConstantsCollection constants)
      {
         // Check that the method returns void.
         if (method.Type.TypeKind != EnvDTE.vsCMTypeRef.vsCMTypeRefVoid)
            throw new GenerationException(String.Format("The method {0} must return void to be an event source template method.", method.Name), (CodeElement)method);

         if (method.OverrideKind != EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
            throw new GenerationException(String.Format("The method {0} must be abstract to be an event source template method.", method.Name), (CodeElement)method);

         // See if we have parameters that aren't natively supported by EventSource. If so, we need to create a wrapper method, that performs
         // the conversion.
         bool needsPrivateImpl = !asPrivateImpl && method.Parameters.Parameters().Any(p => !IsNativelySupportedByEventSource(p.Type));

         gen.WriteGeneratedCodeWarningComment();

         // Copy the attributes
         var eventSourceBase = GetEventSourceBase(sourceClass);

         if (needsPrivateImpl)
         {
            gen.WriteLine("[{0}.NonEvent]", eventSourceBase.Namespace.FullName);
         }
         else
         {
            WriteAttributes(gen, sourceClass, method.Attributes.Attributes(), constants);
         }

         // Write the method header

         if (asPrivateImpl)
            gen.WriteAccess(vsCMAccess.vsCMAccessPrivate);
         else
            gen.WriteAccess(method.Access);

         var type = method.Type;

         if (!asPrivateImpl)
            gen.Write("override ");
         
         gen.Write("void ");
         gen.Write(method.Name);
         gen.Write("(");

         // Write the parameter list
         foreach (var parameter in method.Parameters.Parameters().AsSmartEnumerable())
         {
            if (!parameter.IsFirst)
               gen.Write(", ");

            WriteAttributes(gen, sourceClass, parameter.Value.Attributes.Attributes(), constants, true);

            if (asPrivateImpl)
            {
               gen.Write(GetTypeSubstitutionForWrapperMethod(parameter.Value.Type));
            }
            else
            {
               gen.Write(parameter.Value.Type.FullTypeName());
            }

            gen.Write(" ");
            gen.Write(parameter.Value.Name);
         }
         gen.WriteLine(")");

         // Start the method body
         using (gen.StartBlock())
         {
            // Get the event id
            int eventId = Int32.Parse(method.Attributes.Attributes().First(attr => attr.Name.Equals("TemplateEvent")).Arguments().First().Value);
            using (gen.StartBlock("if (IsEnabled())"))
            {
               if (!needsPrivateImpl || asPrivateImpl)
               {
                  gen.Write("WriteEvent(");
                  gen.Write(eventId);
                  foreach (var parameter in method.Parameters.Parameters())
                  {
                     gen.Write(", ");
                     gen.Write(parameter.Name);
                  }
                  gen.WriteLine(");");
               }
               else
               {
                  gen.Write(method.Name);
                  gen.Write("(");
                  foreach (var parameter in method.Parameters.Parameters().AsSmartEnumerable())
                  {
                     GetTypeConversionForWrapperMethod(gen, parameter.Value);

                     if (!parameter.IsLast)
                        gen.Write(", ");
                  }
                  gen.WriteLine(");");
               }
            }
         }

         if (needsPrivateImpl)
            GenerateEventMethod(gen, sourceClass, method, true, constants);
      }

      private static string GetTypeSubstitutionForWrapperMethod(ECodeTypeRef codeTypeRef)
      {
         if (IsNativelySupportedByEventSource(codeTypeRef))
            return codeTypeRef.FullTypeName();

         if (codeTypeRef.FullTypeName() == "System.TimeSpan")
            return "System.String";

         throw new GenerationException(String.Format("The type {0} is not a supported event parameter type.", codeTypeRef.FullTypeName()));
      }

      public static void GetTypeConversionForWrapperMethod(SourceCodeGenerator gen, ECodeParameter parameter)
      {
         if (parameter.Type.FullTypeName() == "System.TimeSpan")
         {
            gen.Write(parameter.Name);
            gen.Write(".ToString(\"c\")");
         }
         else
         {
            gen.Write(parameter.Name);
         }
      }

      public static void GenerateWriteEventOverload(SourceCodeGenerator gen, ECodeNamespace attributeNamespace, ECodeTypeRef[] signature)
      {
         gen.WriteLine();
         gen.WriteGeneratedCodeWarningComment();
         gen.WriteLine("[{0}.NonEvent]", attributeNamespace.FullName);
         gen.Write("private unsafe void WriteEvent(int eventId");
         for (int i = 0; i < signature.Length; i++)
         {
            gen.Write(", {0} arg{1}", signature[i].FullTypeName(), i);
         }
         gen.WriteLine(")");

         // Write WriteEvent method body
         using (gen.StartBlock())
         {
            // Perform null checks and argument conversions.
            for (int i = 0; i < signature.Length; i++)
            {
               if (signature[i].FullTypeName() == "System.String")
               {
                  gen.WriteLine("if (arg{0} == null)", i);
                  using (gen.StartIndent())
                  {
                     gen.WriteLine("arg{0} = String.Empty;", i);
                  }
                  gen.WriteLine();
               }
               else if (signature[i].FullTypeName() == "System.Byte[]")
               {
                  gen.WriteLine("if (arg{0} == null)", i);
                  using (gen.StartIndent())
                  {
                     gen.WriteLine("arg{0} = new byte[0];", i);
                  }
                  gen.WriteLine();
               }
               else if (signature[i].FullTypeName() == "System.DateTime")
               {
                  gen.WriteLine("System.Int64 fileTime{0} = arg{0}.ToFileTimeUtc();", i);
               }
               else if (signature[i].TypeKind == EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType && signature[i].CodeType.Kind == EnvDTE.vsCMElement.vsCMElementEnum)
               {
                  // TODO PP: When Roslyn can be used, we don't need this special handling. It is only here because we cannot
                  // find the underlying type with the EnvDTE methods... or at least I haven't figured out how.
                  gen.WriteLine("System.Int64 enumValue{0} = (System.Int64)arg{0};", i);
               }
            }

            // Descriptor length. A byte-array actually takes up two descriptor-slots (length + data), so we calculate the total size here.");
            int descrLength = signature.Select(t => t.FullTypeName() == "System.Byte[]" ? 2 : 1).Sum();

            gen.WriteLine("EventData* descrs = stackalloc EventData[{0}];", descrLength);

            bool anyFixed = false;
            for (int i = 0; i < signature.Length; i++)
            {
               if (signature[i].FullTypeName() == "System.String")
               {
                  anyFixed = true;
                  gen.WriteLine("fixed (char* str{0} = arg{0})", i);
               }
               else if (signature[i].FullTypeName() == "System.Byte[]")
               {
                  anyFixed = true;
                  gen.WriteLine("fixed (byte *bin{0} = arg{0})", i);
               }
            }

            using (IDisposable disp = anyFixed ? gen.StartBlock() : null)
            {
               int descrPos = 0;
               for (int i = 0; i < signature.Length; i++, descrPos++)
               {
                  if (signature[i].FullTypeName() == "System.Byte[]")
                  {
                     gen.WriteLine("int length{0} = arg{0}.Length;", i);
                     gen.WriteLine("descrs[{0}].DataPointer = (IntPtr)(&length{1});", descrPos, i);
                     gen.WriteLine("descrs[{0}].Size = 4;", descrPos);
                     descrPos++;
                     gen.WriteLine("descrs[{0}].DataPointer = (IntPtr)bin{1};", descrPos, i);
                     gen.WriteLine("descrs[{0}].Size = length{1};", descrPos, i);
                  }
                  else if (signature[i].FullTypeName() == "System.String")
                  {
                     gen.WriteLine("descrs[{0}].DataPointer = (IntPtr)str{1};", descrPos, i);
                     gen.WriteLine("descrs[{0}].Size = (arg{1}.Length + 1) * 2;", descrPos, i);
                  }
                  else if (signature[i].FullTypeName() == "System.DateTime")
                  {
                     gen.WriteLine("descrs[{0}].DataPointer = (IntPtr)(&fileTime{1});", descrPos, i);
                     gen.WriteLine("descrs[{0}].Size = 8;", descrPos, i);
                  }
                  else if (signature[i].TypeKind == EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType && signature[i].CodeType.Kind == EnvDTE.vsCMElement.vsCMElementEnum)
                  {
                     // TODO PP: When Roslyn can be used, we don't need this special handling. It is only here because we cannot
                     // find the underlying type with the EnvDTE methods... or at least I haven't figured out how.
                     gen.WriteLine("descrs[{0}].DataPointer = (IntPtr)(&enumValue{1});", descrPos, i);
                     gen.WriteLine("descrs[{0}].Size = 8;", descrPos, i);
                  }
                  else
                  {
                     gen.WriteLine("descrs[{0}].DataPointer = (IntPtr)(&arg{1});", descrPos, i);
                     gen.WriteLine("descrs[{0}].Size = {1};", descrPos, Marshal.SizeOf(Type.GetType(signature[i].FullTypeName())));
                  }
               }

               gen.WriteLine("WriteEventCore(eventId, {0}, descrs);", descrLength);
            }
         }
      }
      
      private static bool SignaturesEqual(IEnumerable<ECodeTypeRef> left, IEnumerable<ECodeTypeRef> right)
      {
         return left.Select(c => c.FullTypeName()).SequenceEqual(right.Select(c => c.FullTypeName()));
      }

      private static bool IsNativelySupportedByEventSource(ECodeTypeRef codeType)
      {
         switch (codeType.FullTypeName())
         {
            case "System.Byte":
            case "System.SByte":
            case "System.Int16":
            case "System.UInt16":
            case "System.Int32":
            case "System.UInt32":
            case "System.Int64":
            case "System.UInt64":
            case "System.Single":
            case "System.Double":
            case "System.Guid":
            case "System.DateTime":            
            case "System.String":
            case "System.Byte[]":
            case "System.Boolean":
               return true;
            default:
               break;
         }

         if (codeType.TypeKind == EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType && codeType.CodeType.Kind == EnvDTE.vsCMElement.vsCMElementEnum)
            return true;

         return false;
      }

      private static bool IsSupportedEventParameter(ECodeTypeRef codeType)
      {
         return IsNativelySupportedByEventSource(codeType) ||
            codeType.FullTypeName() == "System.TimeSpan";         
      }

      public static string GetTargetClassName(EnvDTE80.CodeClass2 sourceClass)
      {
         string targetClassName = sourceClass.Name + "Impl";
         if (sourceClass.Name.EndsWith("Template"))
            targetClassName = sourceClass.Name.Substring(0, sourceClass.Name.Length - "Template".Length);
         else if (sourceClass.Name.EndsWith("Base"))
            targetClassName = sourceClass.Name.Substring(0, sourceClass.Name.Length - "Base".Length);
         return targetClassName;
      }

      private static string GetMemberNameFromMemberAccessExpression(string expression)
      {
         int index = expression.IndexOf('.');
         if (index == -1 || index == expression.Length - 1)
            return expression;

         return expression.Substring(index + 1);
      }

      private static string GetTypeNameFromMemberAccessExpression(string expression)
      {
         int index = expression.IndexOf('.');
         if (index == -1)
            return null;

         return expression.Substring(0, index);
      }

      public static void WriteAttributes(SourceCodeGenerator gen, EnvDTE80.CodeClass2 sourceClass, IEnumerable<ECodeAttribute> attributes, ConstantsCollection constants, bool singleLine = false)
      {
         Dictionary<string, string> tasks = new Dictionary<string, string>();
         foreach (var sourceAttribute in attributes)
         {
            string targetAttributeTypeName = GetTargetAttributeTypeName(sourceClass, sourceAttribute);

            if (IsEventAttribute(sourceAttribute))
            {
               gen.Write("[{0}(", targetAttributeTypeName);

               foreach (var argumentEntry in sourceAttribute.Arguments().AsSmartEnumerable())
               {
                  var argument = argumentEntry.Value;

                  if (String.IsNullOrEmpty(argument.Name))
                  {
                     gen.Write(argument.Value);
                  }
                  else
                  {
                     gen.Write("{0} = ", argument.Name);
                     if (argument.Name == "Task" || argument.Name == "Opcode" || argument.Name == "Keywords")
                     {
                        switch (argument.Name)
                        {
                           case "Task":
                              constants.Tasks[GetMemberNameFromMemberAccessExpression(argument.Value)] = argument.Value;
                              break;
                           case "Opcode":
                              string opCodeType = GetTypeNameFromMemberAccessExpression(argument.Value);
                              if (opCodeType == "EventOpcode" ||
                                  opCodeType == GetEventSourceBase(sourceClass).Namespace.FullName + ".EventOpcode")
                              {
                                 gen.Write(argument.Value);
                                 if (!argumentEntry.IsLast)
                                    gen.Write(", ");

                                 continue;
                              }
                              constants.Opcodes[GetMemberNameFromMemberAccessExpression(argument.Value)] = argument.Value;
                              break;
                           case "Keywords":
                              constants.Keywords[GetMemberNameFromMemberAccessExpression(argument.Value)] = argument.Value;
                              break;
                        }

                        gen.Write("{0}{1}.{2}", argument.Name, argument.Name.EndsWith("s") ? "" : "s", GetMemberNameFromMemberAccessExpression(argument.Value));
                     }
                     else
                     {
                        gen.Write(argument.Value);
                     }
                  }

                  if (!argumentEntry.IsLast)
                     gen.Write(", ");
               }
               gen.Write(")]");

            }
            else
            {
               gen.WriteAttribute(targetAttributeTypeName, sourceAttribute.Arguments());
            }

            if (!singleLine)
               gen.WriteLine();
            else
               gen.Write(" ");
         }
      }

      protected override byte[] GenerateCode(string inputFileContent)
      {
         try
         {
            using (StringWriter writer = new StringWriter())
            {
               try
               {
                  GenerateCode(inputFileContent, writer);
               }
               catch (GenerationException gex)
               {
                  ReportError(gex.Message, gex.Line, gex.Column);
                  writer.WriteLine("#error Error generating EventSource: \"{0}\"", gex.Message);
               }

               writer.Flush();

               //Get the Encoding used by the writer. We're getting the WindowsCodePage encoding, 
               //which may not work with all languages
               Encoding enc = Encoding.GetEncoding(writer.Encoding.WindowsCodePage);

               //Get the preamble (byte-order mark) for our encoding
               byte[] preamble = enc.GetPreamble();
               int preambleLength = preamble.Length;

               //Convert the writer contents to a byte array
               byte[] body = enc.GetBytes(writer.ToString());

               //Prepend the preamble to body (store result in resized preamble array)
               Array.Resize<byte>(ref preamble, preambleLength + body.Length);
               Array.Copy(body, 0, preamble, preambleLength, body.Length);

               //Return the combined byte array
               return preamble;
            }
         }

         catch (Exception e)
         {
            ReportError(e.ToString(), 1, 1);
            return null;
         }
      }

      private static ECodeClass GetEventSourceBase(ECodeClass sourceClass)
      {
         ECodeClass baseClass = (ECodeClass)sourceClass.GetAllBaseClasses().FirstOrDefault(bc => bc.FullName.Equals("Microsoft.Diagnostics.Tracing.EventSource") || bc.FullName.Equals("System.Diagnostics.Tracing.EventSource"));
         if (baseClass == null)
            throw new GenerationException(String.Format("The class {0} does not appear to inherit from an EventSource class.", sourceClass.FullName), (CodeElement)sourceClass);

         return baseClass;
      }

      public static bool IsEventAttribute(ECodeAttribute attribute)
      {
         return attribute.Name == "TemplateEvent" || attribute.Name == "TemplateEventAttribute";
      }

      public static string GetTargetAttributeTypeName(ECodeClass sourceClass, EnvDTE80.CodeAttribute2 sourceAttribute)
      {
         string targetAttributeTypeName;
         // Replace TemplateEvent and TemplateEventSource with their appropriate attributes
         if (sourceAttribute.Name == "TemplateEvent" || sourceAttribute.Name == "TemplateEventSource")
         {
            bool isNuget = sourceClass.GetAllBaseClasses().Any(bc => bc.FullName.Equals("Microsoft.Diagnostics.Tracing.EventSource"));
            targetAttributeTypeName = (isNuget ? "Microsoft.Diagnostics.Tracing." : "System.Diagnostics.Tracing.") + sourceAttribute.Name.Substring("Template".Length);
         }
         else
         {
            targetAttributeTypeName = sourceAttribute.FullName;
         }
         return targetAttributeTypeName;
      }

      public static void GenerateSingletonDefinition(SourceCodeGenerator gen, string targetClassName)
      {
         using (gen.StartRegion("Singleton Definition"))
         {
            gen.WriteGeneratedCodeWarningComment();
            gen.WriteLine("private static readonly {0} s_log = new {0}();", targetClassName);
            gen.WriteLine();
            gen.WriteGeneratedCodeWarningComment();
            gen.WriteLine("public static {0} Log {{ get {{ return s_log; }} }}", targetClassName);
         }
      }

      public class ConstantsCollection
      {
         public readonly Dictionary<string, string> Tasks = new Dictionary<string, string>();
         public readonly Dictionary<string, string> Opcodes = new Dictionary<string, string>();
         public readonly Dictionary<string, string> Keywords = new Dictionary<string, string>();
      }
   }
}
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxGenerator = Microsoft.CodeAnalysis.Editing.SyntaxGenerator;
using DeclarationModifiers = Microsoft.CodeAnalysis.Editing.DeclarationModifiers;
using System.Reflection;

using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using Alphaleonis.Vsx;
using Alphaleonis.Vsx.Roslyn.CSharp;
using Microsoft.VisualStudio.Shell;

namespace Alphaleonis.EventSourceGenerator
{
   internal partial class EventSourceGenerator
   {
      #region Private Fields

      private readonly FrameworkName m_targetFramework;
      private const string TemplateEventSourceAttributeName = "TemplateEventSourceAttribute";
      private const string TemplateEventAttributeName = "TemplateEventAttribute";

      private readonly Document m_document;
      private readonly Compilation m_compilation;
      private readonly SyntaxTree m_syntaxTree;
      private readonly CompilationUnitSyntax m_root;
      private readonly SemanticModel m_semanticModel;
      private readonly SyntaxGenerator m_generator;
      private readonly ParameterConverterCollection m_parameterConverters;

      #endregion

      #region Constructor

      private EventSourceGenerator(Document document, Compilation compilation, SyntaxTree syntaxTree, CompilationUnitSyntax root, SemanticModel semanticModel, FrameworkName targetFramework)
      {
         if (document == null)
            throw new ArgumentNullException("document", "document is null.");

         if (compilation == null)
            throw new ArgumentNullException("compilation", "compilation is null.");

         if (syntaxTree == null)
            throw new ArgumentNullException("syntaxTree", "syntaxTree is null.");

         if (root == null)
            throw new ArgumentNullException("root", "root is null.");

         if (semanticModel == null)
            throw new ArgumentNullException("semanticModel", "semanticModel is null.");

         m_targetFramework = targetFramework;
         m_document = document;
         m_compilation = compilation;
         m_syntaxTree = syntaxTree;
         m_root = root;
         m_semanticModel = semanticModel;

         EventSourceTypes = ImmutableList.CreateRange(new[]
                 {
                        m_compilation.GetTypeByMetadataName("System.Diagnostics.Tracing.EventSource"),
                        m_compilation.GetTypeByMetadataName("Microsoft.Diagnostics.Tracing.EventSource")
                    }
                 .Where(s => s != null)
                 .Select(s => new EventSourceTypeInfo(m_semanticModel, s)));

         if (EventSourceTypes.Count == 0)
            throw new CodeGeneratorException("The class System.Diagnostics.Tracing.EventSource or Microsoft.Diagnostics.Tracing.EventSource could not be found. You need to add a reference to the assembly containing one of these classes to continue.");

         m_generator = SyntaxGenerator.GetGenerator(document);
         m_parameterConverters = new ParameterConverterCollection(m_semanticModel, m_generator);
      }

      #endregion

      #region Properties

      private IReadOnlyList<EventSourceTypeInfo> EventSourceTypes
      {
         get;
      }

      #endregion

      #region Public Methods

      public static async Task<CompilationUnitSyntax> GenerateEventSourceImplementationsAsync(Document document, FrameworkName targetFrameworkName, CancellationToken cancellationToken = default(CancellationToken))
      {
         Compilation compilation = await document.Project.GetCompilationAsync(cancellationToken);
         SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);

         CompilationUnitSyntax root = await document.GetCompilationUnitRootAsync(cancellationToken);
         SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);

         EventSourceGenerator generator = new EventSourceGenerator(document, compilation, syntaxTree, root, semanticModel, targetFrameworkName);

         return generator.Generate();
      }

      #endregion

      #region Private Methods

      private CompilationUnitSyntax Generate()
      {         
         IReadOnlyList<ClassDeclarationSyntax> sourceClasses = m_root.TopLevelClasses()
            .Where(c => GetEventSourceBaseTypeInfo(m_semanticModel.GetDeclaredSymbol(c)) != null).ToImmutableArray();

         CompilationUnitSyntax targetCompilationUnit = SyntaxFactory.CompilationUnit(m_root.Externs, m_root.Usings, SyntaxFactory.List<AttributeListSyntax>(), SyntaxFactory.List<MemberDeclarationSyntax>());

         if (sourceClasses.Count == 0)
            return targetCompilationUnit;

         Dictionary<string, NamespaceDeclarationSyntax> namespaces = new Dictionary<string, NamespaceDeclarationSyntax>();

         foreach (ClassDeclarationSyntax sourceClass in sourceClasses)
         {
            INamedTypeSymbol sourceClassSymbol = m_semanticModel.GetDeclaredSymbol(sourceClass);

            string targetNamespaceName = sourceClassSymbol.ContainingNamespace.IsGlobalNamespace ? "" : sourceClassSymbol.ContainingNamespace.GetFullName();

            NamespaceDeclarationSyntax targetNamespace;
            if (!namespaces.TryGetValue(targetNamespaceName, out targetNamespace))
            {
               targetNamespace = (NamespaceDeclarationSyntax)m_generator.NamespaceDeclaration(targetNamespaceName);
               namespaces.Add(targetNamespaceName, targetNamespace);
            }

            targetNamespace = targetNamespace.AddMembers(GenerateEventSourceClass(sourceClassSymbol));

            namespaces[targetNamespaceName] = targetNamespace;
         }

         if (namespaces.ContainsKey(String.Empty))
         {
            targetCompilationUnit = targetCompilationUnit.AddMembers(namespaces[String.Empty]);
            m_generator.AddMembers(targetCompilationUnit, m_generator.GetMembers(namespaces[String.Empty]));
         }

         targetCompilationUnit = targetCompilationUnit.AddMembers(namespaces.Where(ns => ns.Key != String.Empty).Select(kvp => kvp.Value).ToArray());

         return targetCompilationUnit;
      }

      private void ValidateReservedMemberName(INamedTypeSymbol sourceClass, string name)
      {
         var member = sourceClass.GetMembers(name).FirstOrDefault();
         if (member != null)
            throw new CodeGeneratorException(member, $"The class '{sourceClass.Name}' is not allowed to contain a member named '{name}'. Choose a different name for this member. The generated class will have a new generated nested class called '{name}', which is required by the event manifest generation.");
      }

      private ClassDeclarationSyntax GenerateEventSourceClass(INamedTypeSymbol sourceClass)
      {
         if (sourceClass.IsGenericType)
            throw new CodeGeneratorException(sourceClass, $"The template class '{sourceClass.Name}' for generating an EventSource implementation must not be generic.");

         if (!sourceClass.IsAbstract)
            throw new CodeGeneratorException(sourceClass, $"The class '{sourceClass.Name}' must be abstract to participate in EventSource implementation generation.");

         ValidateReservedMemberName(sourceClass, "Opcodes");
         ValidateReservedMemberName(sourceClass, "Keywords");
         ValidateReservedMemberName(sourceClass, "Tasks");

         GenerationOptions options = ParseGenerationOptions(sourceClass);

         EventSourceTypeInfo eventSourceTypeInfo = GetEventSourceBaseTypeInfo(sourceClass);

         IEnumerable<SyntaxNode> translatedAttributes = GetClassAttributesWithTranslation(sourceClass, eventSourceTypeInfo);

         var asms = AppDomain.CurrentDomain.GetAssemblies().OrderBy(asm => asm.FullName).ToArray();

         var targetClass = m_generator.ClassDeclaration(
            name: options.TargetClassName,
            baseType: m_generator.IdentifierName(sourceClass.Name),
            accessibility: sourceClass.DeclaredAccessibility == Accessibility.Public ? Accessibility.Public : Accessibility.Internal,
            modifiers: DeclarationModifiers.Sealed
         );

         targetClass = m_generator.AddAttributes(targetClass, translatedAttributes);
         var overloads = new CollectedGenerationInfo(eventSourceTypeInfo);
         ImmutableArray<SyntaxNode> eventSourceMethods = GenerateEventSourceMethods(sourceClass, eventSourceTypeInfo, overloads, options).ToImmutableArray();

         if (eventSourceMethods.Length > 0)
         {
            eventSourceMethods = eventSourceMethods.SetItem(0, eventSourceMethods[0].PrependLeadingTrivia(CreateRegionTriviaList("Event Methods")));
            eventSourceMethods = eventSourceMethods.SetItem(eventSourceMethods.Length - 1, eventSourceMethods.Last().WithTrailingTrivia(CreateEndRegionTriviaList().Add(SF.EndOfLine(Environment.NewLine)).Add(SF.EndOfLine(Environment.NewLine))));
         }

         targetClass = m_generator.AddMembers(targetClass, eventSourceMethods);

         if (!options.SuppressSingletonGeneration)
            targetClass = m_generator.InsertMembers(targetClass, 0, CreateSingletonProperty(sourceClass, options));

         targetClass = targetClass.WithLeadingTrivia(CreateWarningComment());

         if (options.AllowUnsafeCode)
         {
            var writeEventMethods = GenerateWriteEventOverloads(overloads, options, eventSourceTypeInfo).ToImmutableArray();

            if (writeEventMethods.Length > 0)
            {
               writeEventMethods = writeEventMethods.SetItem(0, writeEventMethods[0].PrependLeadingTrivia(CreateRegionTriviaList("WriteEvent Overloads")));
               writeEventMethods = writeEventMethods.SetItem(writeEventMethods.Length - 1, writeEventMethods.Last().WithTrailingTrivia(CreateEndRegionTriviaList().Add(SF.EndOfLine(Environment.NewLine)).Add(SF.EndOfLine(Environment.NewLine))));
            }

            targetClass = m_generator.AddMembers(targetClass, writeEventMethods);
         }

         if (overloads.Keywords.Count > 0)
            targetClass = m_generator.AddMembers(targetClass, GenerateConstantsClass("Keywords", overloads.Keywords, m_compilation.GetTypeByMetadataName(eventSourceTypeInfo.EventSourceNamespace.GetFullName() + ".EventKeywords")));

         if (overloads.Opcodes.Count > 0)
            targetClass = m_generator.AddMembers(targetClass, GenerateConstantsClass("Opcodes", overloads.Opcodes, m_compilation.GetTypeByMetadataName(eventSourceTypeInfo.EventSourceNamespace.GetFullName() + ".EventOpcode")));

         if (overloads.Tasks.Count > 0)
            targetClass = m_generator.AddMembers(targetClass, GenerateConstantsClass("Tasks", overloads.Tasks, m_compilation.GetTypeByMetadataName(eventSourceTypeInfo.EventSourceNamespace.GetFullName() + ".EventTask")));

         // Add GeneratedCode attribute to the target class.
         targetClass = m_generator.AddAttributes(targetClass, 
                                                 m_generator.Attribute(typeof(System.CodeDom.Compiler.GeneratedCodeAttribute).FullName, 
                                                      m_generator.AttributeArgument(m_generator.LiteralExpression(Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>()?.Title)), 
                                                      m_generator.AttributeArgument(m_generator.LiteralExpression(Assembly.GetExecutingAssembly().GetName().Version.ToString()))));

         return (ClassDeclarationSyntax)targetClass;
      }

      private SyntaxNode GenerateConstantsClass(string className, Dictionary<string, SyntaxNode> constants, INamedTypeSymbol targetType)
      {
         SyntaxNode classDecl = m_generator.ClassDeclaration(className,
            accessibility: Accessibility.Public,
            modifiers: DeclarationModifiers.Static);

         foreach (var entry in constants)
         {
            var member = m_generator.FieldDeclaration(
               name: entry.Key,
               type: m_generator.TypeExpression(targetType),
               accessibility: Accessibility.Public,
               modifiers: DeclarationModifiers.Const,
               initializer: entry.Value
            );

            classDecl = m_generator.AddMembers(classDecl, new[] { member });
         }

         classDecl = classDecl.WithLeadingTrivia(CreateWarningComment());
         return classDecl;

      }

      private SyntaxNode WithUnsafeModifier(SyntaxNode methodDeclaration)
      {
         // TODO: There seems to be a problem with using the DeclarationModifiers in this version of Roslyn. It works in the latest version, but that version
         // does not work in the RC of visual studio. This method can be removed, and the SyntaxGenerator used instead when VS2015 RTM is released.
         MethodDeclarationSyntax method = methodDeclaration as MethodDeclarationSyntax;
         if (method != null)
         {
            method = method.AddModifiers(SF.Token(SyntaxKind.UnsafeKeyword));
         }

         return method;
      }

      private IEnumerable<SyntaxNode> GenerateWriteEventOverloads(CollectedGenerationInfo overloads, GenerationOptions options, EventSourceTypeInfo eventSourceTypeInfo)
      {
         foreach (WriteEventOverloadInfo overload in overloads.Overloads)
         {
            SyntaxNode method = m_generator.MethodDeclaration("WriteEvent",
               new[] { m_generator.ParameterDeclaration("eventId", m_generator.TypeExpression(SpecialType.System_Int32)) }
               .Concat(overload.Parameters.Select((pi, idx) => m_generator.ParameterDeclaration($"arg{idx}", m_generator.TypeExpression(pi.TargetType)))),
               accessibility: Accessibility.Private);

            method = WithUnsafeModifier(method);

            method = m_generator.AddAttributes(method, m_generator.Attribute(
               m_generator.TypeExpression(
                  m_semanticModel.Compilation.GetTypeByMetadataName(eventSourceTypeInfo.EventSourceNamespace.GetFullName() + ".NonEventAttribute")
               )));
            List<SyntaxNode> statements = new List<SyntaxNode>();

            for (int i = 0; i < overload.Parameters.Length; i++)
            {
               if (overload.Parameters[i].TargetType.SpecialType == SpecialType.System_String)
               {
                  statements.Add(m_generator.IfStatement(m_generator.ReferenceEqualsExpression(m_generator.IdentifierName($"arg{i}"), m_generator.NullLiteralExpression()),
                     new[]
                     {
                        m_generator.AssignmentStatement(m_generator.IdentifierName($"arg{i}"),
                           m_generator.MemberAccessExpression(m_generator.TypeExpression(SpecialType.System_String), "Empty")
                        )
                     }
                  ));
               }
               else if (overload.Parameters[i].TargetType.TypeKind == TypeKind.Array && ((IArrayTypeSymbol)overload.Parameters[i].TargetType).ElementType.SpecialType == SpecialType.System_Byte)
               {
                  statements.Add(m_generator.IfStatement(m_generator.ReferenceEqualsExpression(m_generator.IdentifierName($"arg{i}"), m_generator.NullLiteralExpression()),
                     new[]
                     {
                        m_generator.AssignmentStatement(m_generator.IdentifierName($"arg{i}"),
                           m_generator.ArrayCreationExpression(
                              m_generator.TypeExpression(SpecialType.System_Byte),
                              m_generator.LiteralExpression(0)
                           )
                        )
                     }
                  ));
               }
               else if (overload.Parameters[i].TargetType.SpecialType == SpecialType.System_DateTime)
               {
                  statements.Add(
                     m_generator.LocalDeclarationStatement(
                        m_generator.TypeExpression(SpecialType.System_Int64),
                        $"fileTime{i}",
                        m_generator.InvocationExpression(
                           m_generator.MemberAccessExpression(
                              m_generator.IdentifierName($"arg{i}"),
                              m_generator.IdentifierName("ToFileTimeUtc")
                           )
                        )
                     )
                  );
               }
               else if (overload.Parameters[i].TargetType.TypeKind == TypeKind.Enum)
               {
                  INamedTypeSymbol namedTypeSymbol = (INamedTypeSymbol)overload.Parameters[i].TargetType;
                  statements.Add(
                     m_generator.LocalDeclarationStatement(
                        m_generator.TypeExpression(namedTypeSymbol.EnumUnderlyingType),
                        $"enumValue{i}",
                        m_generator.CastExpression(
                           m_generator.TypeExpression(namedTypeSymbol.EnumUnderlyingType),
                           m_generator.IdentifierName($"arg{i}")
                        )
                     )
                  );
               }
            }

            // Descriptor length. A byte-array actually takes up two descriptor-slots (length + data), so we calculate the total size here.");
            int descrLength = overload.Parameters.Select(pi => pi.TargetType.IsByteArray() ? 2 : 1).Sum();

            string eventDataTypeFullName = eventSourceTypeInfo.EventSourceClass.GetFullName() + "+EventData";
            INamedTypeSymbol eventDataType = m_compilation.GetTypeByMetadataName(eventDataTypeFullName);
            if (eventDataType == null)
               throw new CodeGeneratorException($"Failed to lookup type {eventDataTypeFullName}.");

            TypeSyntax eventDataTypeSyntax = (TypeSyntax)m_generator.TypeExpression(eventDataType);

            //EventData* descrs = stackalloc EventData[{descrLength}];
            statements.Add(
               SF.LocalDeclarationStatement(
                  SF.VariableDeclaration(
                     SF.PointerType(
                        eventDataTypeSyntax
                     ),
                     SF.SingletonSeparatedList(
                        SF.VariableDeclarator(
                           SF.Identifier("descrs"),
                           null,
                           SF.EqualsValueClause(
                              SF.Token(SyntaxKind.EqualsToken),
                              SF.StackAllocArrayCreationExpression(
                                 SF.ArrayType(
                                    eventDataTypeSyntax,
                                    SF.SingletonList(
                                       SF.ArrayRankSpecifier(
                                          SF.SingletonSeparatedList(
                                             m_generator.LiteralExpression(descrLength) as ExpressionSyntax
                                          )
                                       )
                                    )
                                 )
                              )
                           )
                        )
                     )
                  )
               )
            );


            List<SyntaxNode> innerStatements = new List<SyntaxNode>();
            int descrPos = 0;
            for (int i = 0; i < overload.Parameters.Length; i++, descrPos++)
            {
               if (overload.Parameters[i].TargetType.IsByteArray())
               {
                  // ==> int length{i} = arg{i}.Length;
                  innerStatements.Add(
                     m_generator.LocalDeclarationStatement(
                        m_generator.TypeExpression(SpecialType.System_Int32),
                        $"length{i}",
                        m_generator.MemberAccessExpression(m_generator.IdentifierName($"arg{i}"), "Length")
                     )
                  );

                  // ==> descrs[{descrPos}].DataPointer = (IntPtr)(&length{i});
                  innerStatements.Add(
                     AssignDescrSyntax("descrs", descrPos, "DataPointer", AddressOfVariableAsIntPtrSyntax($"length{i}"))
                  );

                  // ==> descrs[{descrPos}].Size = 4;
                  innerStatements.Add(
                     AssignDescrSyntax("descrs", descrPos, "Size", m_generator.LiteralExpression(4))
                  );

                  descrPos++;

                  // ==> descrs[{descrPos}].DataPointer = (IntPtr)bin{i}
                  innerStatements.Add(
                     AssignDescrSyntax("descrs", descrPos, "DataPointer", m_generator.CastExpression(IntPtrType, m_generator.IdentifierName($"bin{i}")))
                  );

                  // ==> descrs[{descrPos}].Size = length{i}
                  innerStatements.Add(
                     AssignDescrSyntax("descrs", descrPos, "Size", m_generator.IdentifierName($"length{i}"))
                  );
               }
               else if (overload.Parameters[i].TargetType.SpecialType == SpecialType.System_String)
               {
                  // ==> descrs[{descrPos}].DataPointer = (IntPtr)str{i}
                  innerStatements.Add(
                     AssignDescrSyntax("descrs", descrPos, "DataPointer", m_generator.CastExpression(IntPtrType, m_generator.IdentifierName($"str{i}")))
                  );

                  // ==> descrs[{descrPos}].Size = (arg{i}.Length + 1) * 2;
                  innerStatements.Add(
                     AssignDescrSyntax("descrs", descrPos, "Size",
                        m_generator.MultiplyExpression(
                           m_generator.AddExpression(
                              m_generator.MemberAccessExpression(
                                 m_generator.IdentifierName($"arg{i}"),
                                 m_generator.IdentifierName($"Length")
                              ),
                              m_generator.LiteralExpression(1)
                           ),
                           m_generator.LiteralExpression(2)
                        )
                     )
                  );
               }
               else if (overload.Parameters[i].TargetType.SpecialType == SpecialType.System_DateTime)
               {
                  // ==> descrs[{descrPos}].DataPointer = (IntPtr)(&fileTime{i});
                  innerStatements.Add(
                     AssignDescrSyntax("descrs", descrPos, "DataPointer", AddressOfVariableAsIntPtrSyntax($"fileTime{i}"))
                  );

                  // ==> descrs[{descrPos}].Size = 8;
                  innerStatements.Add(
                     AssignDescrSyntax("descrs", descrPos, "Size", m_generator.LiteralExpression(8))
                  );
               }
               else if (overload.Parameters[i].TargetType.TypeKind == TypeKind.Enum)
               {
                  INamedTypeSymbol namedTypeSymbol = (INamedTypeSymbol)overload.Parameters[i].TargetType;
                  // ==> descrs[{descrPos}].DataPointer = (IntPtr)(&enumValue{i});
                  innerStatements.Add(
                     AssignDescrSyntax("descrs", descrPos, "DataPointer", AddressOfVariableAsIntPtrSyntax($"enumValue{i}"))
                  );

                  // ==> descrs[{descrPos}].Size = 8;
                  innerStatements.Add(
                     AssignDescrSyntax("descrs", descrPos, "Size", GetTypeSize(overload.Parameters[i].TargetType))
                  );
               }
               else
               {
                  // ==> descrs[{descrPos}].DataPointer = (IntPtr)(&arg{i});
                  innerStatements.Add(
                     AssignDescrSyntax("descrs", descrPos, "DataPointer", AddressOfVariableAsIntPtrSyntax($"arg{i}"))
                  );

                  // ==> descrs[{descrPos}].Size = 8;
                  innerStatements.Add(
                     AssignDescrSyntax("descrs", descrPos, "Size", GetTypeSize(overload.Parameters[i].TargetType))
                  );

               }
            }

            innerStatements.Add(
               m_generator.ExpressionStatement(
                  m_generator.InvocationExpression(
                     m_generator.IdentifierName("WriteEventCore"),
                     m_generator.IdentifierName("eventId"),
                     m_generator.LiteralExpression(descrLength),
                     m_generator.IdentifierName("descrs")
                  )
               )
            );
           
            // Create fixed statements
            BlockSyntax fixedContent = SF.Block(innerStatements.Cast<StatementSyntax>());

            FixedStatementSyntax fixedStatementSyntax = null;
            for (int i = 0; i < overload.Parameters.Length; i++)
            {
               FixedStatementSyntax current = null;
               if (overload.Parameters[i].TargetType.SpecialType == SpecialType.System_String)
               {
                  current = GetFixedStatement(SyntaxKind.CharKeyword, fixedContent, $"str{i}", $"arg{i}");
               }
               else if (overload.Parameters[i].TargetType.IsByteArray())
               {
                  current = GetFixedStatement(SyntaxKind.ByteKeyword, fixedContent, $"bin{i}", $"arg{i}");
               }

               if (current != null)
               {
                  if (fixedStatementSyntax == null)
                  {
                     fixedStatementSyntax = current;
                  }
                  else
                  {
                     fixedStatementSyntax = current.WithStatement(SF.Block(fixedStatementSyntax));
                  }
               }

            }

            if (fixedStatementSyntax != null)
            {
               statements.Add(fixedStatementSyntax);
            }
            else
            {
               statements.Add(fixedContent);
            }

            method = m_generator.WithStatements(method, statements);

            method = method.PrependLeadingTrivia(CreateWarningComment());

            yield return method;
         }
      }

      private SyntaxNode GetTypeSize(ITypeSymbol type)
      {
         if (type.TypeKind == TypeKind.Enum)
         {
            INamedTypeSymbol enumType = type as INamedTypeSymbol;
            if (enumType == null && enumType.TypeKind == TypeKind.Enum)
               throw new ArgumentException("Not an enum type", "type");

            ITypeSymbol underlyingType = enumType.EnumUnderlyingType;
            if (underlyingType == null)
               throw new ArgumentException("Cannot get underlying type of enum", "type");

            switch (underlyingType.SpecialType)
            {
               case SpecialType.System_Byte:
               case SpecialType.System_SByte:
                  return m_generator.LiteralExpression(1);

               case SpecialType.System_Int16:
               case SpecialType.System_UInt16:
                  return m_generator.LiteralExpression(2);

               case SpecialType.System_Int32:
               case SpecialType.System_UInt32:
                  return m_generator.LiteralExpression(4);

               case SpecialType.System_Int64:
               case SpecialType.System_UInt64:
                  return m_generator.LiteralExpression(8);

               default:
                  throw new NotSupportedException($"Cannot get size of underlying enum type: {underlyingType.GetFullName()}");
            }
         }

         if (type.GetFullName() == typeof(Guid).FullName && type.ContainingAssembly.Name == typeof(Guid).Assembly.GetName().Name)
            return m_generator.LiteralExpression(Marshal.SizeOf<Guid>());
         
         switch (type.SpecialType)
         {
            case SpecialType.System_Boolean:
            case SpecialType.System_SByte:
            case SpecialType.System_Byte:
               return m_generator.LiteralExpression(1);
            case SpecialType.System_Char:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
               return m_generator.LiteralExpression(2);
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
               return m_generator.LiteralExpression(4);
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_DateTime:
               return m_generator.LiteralExpression(8);
            case SpecialType.System_Decimal:
               return m_generator.LiteralExpression(System.Runtime.InteropServices.Marshal.SizeOf<decimal>());
            case SpecialType.System_Single:
               return m_generator.LiteralExpression(System.Runtime.InteropServices.Marshal.SizeOf<float>());
            case SpecialType.System_Double:
               return m_generator.LiteralExpression(System.Runtime.InteropServices.Marshal.SizeOf<double>());
            case SpecialType.System_IntPtr:
            case SpecialType.System_UIntPtr:
               ITypeSymbol marshalType = m_compilation.GetTypeByMetadataName(typeof(System.Runtime.InteropServices.Marshal).FullName);
               if (marshalType == null)
                  throw new InvalidOperationException($"Failed to find type of {typeof(System.Runtime.InteropServices.Marshal).FullName}");

               return m_generator.InvocationExpression(
                  m_generator.MemberAccessExpression(
                     m_generator.WithTypeArguments(
                        m_generator.TypeExpression(marshalType),
                        m_generator.TypeExpression(type.SpecialType)
                     ),
                     "SizeOf"
                  )
               );
            default:
               throw new InvalidOperationException($"Unsupported event parameter type {type.GetFullName()} to get size of.");
         }

      }

      private SyntaxNode AddressOfVariableAsIntPtrSyntax(string identifier)
      {
         return SF.CastExpression(
                           IntPtrType,
                           SF.ParenthesizedExpression(
                              SF.PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                 SF.IdentifierName(identifier)
                              )
                           )
                        );
      }

      private SyntaxNode AssignDescrSyntax(string identifier, int position, string memberName, SyntaxNode expression)
      {
         return SF.ExpressionStatement(
                  SF.AssignmentExpression(
                     SyntaxKind.SimpleAssignmentExpression,
                     SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        SF.ElementAccessExpression(
                           SF.IdentifierName(identifier),
                           SF.BracketedArgumentList(
                              SF.SingletonSeparatedList(
                                 SF.Argument(
                                    GetLiteralExpression(position)
                                 )
                              )
                           )
                        ),
                        SF.IdentifierName(memberName)
                     ),
                     (ExpressionSyntax)expression
                  )
               );
      }

      private TypeSyntax IntPtrType
      {
         get
         {
            return m_generator.TypeExpression(m_compilation.GetTypeByMetadataName(typeof(IntPtr).FullName)) as TypeSyntax;
         }
      }

      private LiteralExpressionSyntax GetLiteralExpression(object value)
      {
         return m_generator.LiteralExpression(value) as LiteralExpressionSyntax;
      }

      private static FixedStatementSyntax GetFixedStatement(SyntaxKind pointerTypeKeyword, BlockSyntax fixedContent, string identifier, string initializer)
      {
         return SF.FixedStatement(
                                 SF.VariableDeclaration(
                                    SF.PointerType(
                                       SF.PredefinedType(SF.Token(pointerTypeKeyword))
                                    ),
                                    SF.SingletonSeparatedList(
                                       SF.VariableDeclarator(
                                          SF.Identifier(identifier),
                                          null,
                                          SF.EqualsValueClause(
                                             SF.IdentifierName(initializer)
                                          )
                                       )
                                    )
                                 )
                                 , fixedContent
                              );
      }

      private IEnumerable<SyntaxNode> GenerateEventSourceMethods(INamedTypeSymbol sourceClass, EventSourceTypeInfo eventSourceTypeInfo, CollectedGenerationInfo overloads, GenerationOptions options)
      {
         var templateMethods = GetEventSourceTemplateMethods(sourceClass, eventSourceTypeInfo);
         foreach (var sourceMethodEntry in templateMethods.AsSmartEnumerable())
         {
            IMethodSymbol sourceMethod = sourceMethodEntry.Value;

            TemplateEventMethodInfo eventAttributeInfo = TranslateMethodAttributes(sourceMethod, eventSourceTypeInfo, overloads);

            WriteEventOverloadInfo overloadInfo = new WriteEventOverloadInfo(sourceMethodEntry.Value, options, m_parameterConverters);

            if (overloadInfo.Parameters.Any(p => p.IsSupported == false))
            {
               throw new CodeGeneratorException(sourceMethod, $"The parameter(s) {StringUtils.Join(overloadInfo.Parameters.Where(p => !p.IsSupported).Select(p => $"{p.Parameter.Name} ({p.Parameter.Type.Name})"), ", ", " and ")} are not supported.");
            }

            overloads.TryAdd(overloadInfo);

            // Check if this method has needs a wrapper method to perform parameter conversion into a natively supported parameter type.            
            if (overloadInfo.NeedsConverter)
            {
               // Create the wrapper method
               SyntaxNode wrapperMethod = m_generator.MethodDeclaration(sourceMethod);
               
               // This method should only have the [NonEvent] attribute.
               wrapperMethod = m_generator.AddAttributes(m_generator.RemoveAllAttributes(wrapperMethod), m_generator.Attribute(eventSourceTypeInfo.EventSourceNamespace.GetFullName() + ".NonEvent"));

               wrapperMethod = m_generator.WithAccessibility(wrapperMethod, sourceMethod.DeclaredAccessibility);
               wrapperMethod = m_generator.WithModifiers(wrapperMethod, DeclarationModifiers.Override);

               // And it should call the overload of the same method that is generated below (the actual event method)
               wrapperMethod = m_generator.WithStatements(wrapperMethod,
                  new[]
                  {
                     m_generator.IfStatement(
                        // Condition
                        m_generator.InvocationExpression(m_generator.IdentifierName("IsEnabled")),

                        // True-Statements
                        new[]
                        {
                           m_generator.ExpressionStatement(
                              m_generator.InvocationExpression(m_generator.IdentifierName(sourceMethod.Name),
                                 overloadInfo.Parameters.Select(parameter => m_generator.Argument(
                                          parameter.HasConverter ?
                                             parameter.Converter.GetConversionExpression(m_generator.IdentifierName(parameter.Parameter.Name)) :
                                             m_generator.IdentifierName(parameter.Parameter.Name)
                                    )
                                 )
                              )
                           )
                        }
                     )
                  }
               );

               // And let's add some warning comments about this being generated code.
               wrapperMethod = wrapperMethod.WithLeadingTrivia(wrapperMethod.GetLeadingTrivia().AddRange(CreateWarningComment()));

               yield return wrapperMethod;
            }

            // Generate the actual event method.
            SyntaxNode eventMethod = m_generator.MethodDeclaration(sourceMethod.Name);

            if (overloadInfo.NeedsConverter)
            {
               // If we have a wrapper method converting parameters, this will be a private method.
               eventMethod = m_generator.WithAccessibility(eventMethod, Accessibility.Private);
            }
            else
            {
               // Otherwise it has the same accessibility as the base class method, except this is an override of course.
               eventMethod = m_generator.WithAccessibility(eventMethod, sourceMethod.DeclaredAccessibility);
               eventMethod = m_generator.WithModifiers(eventMethod, DeclarationModifiers.Override);
            }

            // The parameter list may be modified from the source method to account for any conversions performed by the wrapper method.
            eventMethod = m_generator.AddParameters(eventMethod,
               overloadInfo.Parameters.Select(pi => m_generator.ParameterDeclaration(pi.Parameter.Name, m_generator.TypeExpression(pi.TargetType)))
            );

            // Statement to call the WriteEvent() method.
            SyntaxNode writeEventStatement = m_generator.ExpressionStatement(
                                       m_generator.InvocationExpression(m_generator.IdentifierName("WriteEvent"),
                                          new[] {
                                             m_generator.Argument(m_generator.LiteralExpression(eventAttributeInfo.EventId)),
                                          }.Concat(overloadInfo.Parameters.Select(parameter => m_generator.Argument(
                                                        m_generator.IdentifierName(parameter.Parameter.Name))
                                             )
                                          )
                                       )
                                    );

            if (overloadInfo.NeedsConverter)
            {
               // If this method has a wrapper method, then the IsEnabled() check has already been made, so we skip that here
               // and just call the WriteEvent() method.
               eventMethod = m_generator.WithStatements(eventMethod, new[] { writeEventStatement });
            }
            else
            {
               // Otherwise we want to check the IsEnabled() flag first.
               eventMethod = m_generator.WithStatements(eventMethod,
                  new[]
                  {
                     m_generator.IfStatement(
                        // Condition
                        m_generator.InvocationExpression(m_generator.IdentifierName("IsEnabled")),

                        // True-Statements
                        new[]
                        {
                           writeEventStatement
                        }
                     )
                  }
               );
            }

            // Add all attributes from the source method. (Well, with translation of the TemplateEventAttribute).
            eventMethod = m_generator.AddAttributes(eventMethod, eventAttributeInfo.Attributes);

            // And some warning comments as usual.
            eventMethod = eventMethod.WithLeadingTrivia(eventMethod.GetLeadingTrivia().AddRange(CreateWarningComment()));

            yield return eventMethod;
         }
      }

      /// <summary>Gets the event source template methods in this collection.</summary>
      /// <param name="sourceClass">The class from which to retrieve methods.</param>
      /// <param name="eventSourceBase">The event source base of the <paramref name="sourceClass"/>.</param>
      /// <returns>
      ///     All methods that are candidates to be used for EventSource generation.
      /// </returns>
      private IEnumerable<IMethodSymbol> GetEventSourceTemplateMethods(INamedTypeSymbol sourceClass, EventSourceTypeInfo eventSourceBase)
      {
         System.Diagnostics.Debug.Assert(sourceClass.IsType);

         foreach (IMethodSymbol method in sourceClass.GetAllMembers().OfType<IMethodSymbol>())
         {
            if (method.GetAttributes().Any(attribute => attribute.AttributeClass.Equals(eventSourceBase.EventAttributeType)) && method.IsAbstract)
            {
               yield return method;
            }
            else if (method.GetAttributes().Any(attribute => attribute.AttributeClass.Name.Equals(TemplateEventAttributeName)))
            {
               if (!method.IsAbstract)
                  throw new CodeGeneratorException(method, $"The method {sourceClass.Name}.{method.Name} must be abstract to participate in EventSource generation.");

               yield return method;
            }
         }
      }

      private EventSourceTypeInfo GetEventSourceBaseTypeInfo(INamedTypeSymbol sourceClass)
      {
         return EventSourceTypes.FirstOrDefault(est => sourceClass.GetBaseTypes().Any(c => est.EventSourceClass.Equals(c)));
      }

      private GenerationOptions ParseGenerationOptions(INamedTypeSymbol sourceClass)
      {
         AttributeData eventSourceTemplateAttribute = GetCustomAttribute(sourceClass, TemplateEventSourceAttributeName);

         GenerationOptions options = new GenerationOptions(m_targetFramework.Version);

         if (eventSourceTemplateAttribute != null)
         {
            foreach (var namedArgument in eventSourceTemplateAttribute.NamedArguments)
            {
               if (namedArgument.Value.Value != null)
               {
                  var propertyInfo = typeof(GenerationOptions).GetProperty(namedArgument.Key);
                  if (propertyInfo != null)
                  {
                     if (!namedArgument.Value.Value.GetType().Equals(propertyInfo.PropertyType))
                     {
                        throw new CodeGeneratorException($"The value for argument {namedArgument.Key} of attribute {TemplateEventSourceAttributeName} on {sourceClass.Name} has the wrong type. Expected {propertyInfo.PropertyType.Name}, found {namedArgument.Value.Value.GetType()}.");
                     }
                     else
                     {
                        propertyInfo.SetValue(options, namedArgument.Value.Value);
                     }
                  }
               }
            }
         }

         if (options.TargetClassName == null)
         {
            if (sourceClass.Name.EndsWith("Base"))
            {
               options.TargetClassName = sourceClass.Name.Substring(0, sourceClass.Name.Length - "Base".Length);
            }
            else if (sourceClass.Name.EndsWith("Template"))
            {
               options.TargetClassName = sourceClass.Name.Substring(0, sourceClass.Name.Length - "Template".Length);
            }
            else
            {
               options.TargetClassName = sourceClass.Name + "Impl";
            }
         }

         return options;
      }

      private AttributeData GetCustomAttribute(INamedTypeSymbol sourceClass, string attributeName)
      {
         try
         {
            return sourceClass.GetAttributes().SingleOrDefault(attribute => attribute.AttributeClass.Name == attributeName);
         }
         catch (InvalidOperationException)
         {
            throw new CodeGeneratorException(sourceClass, $"The class '{sourceClass.Name}' is decorated with multiple attributes named '{attributeName}'. At most one such attribute may be present on any one class.");
         }
      }

      /// <summary>
      /// Gets all the attributes from the specified <paramref name="sourceClass"/>, with special handling and translation 
      /// of a TemplateEventSourceAttribute which will be translated into an EventSourceAttribute with additional arguments stripped away.
      /// </summary>
      /// <param name="sourceClass">The clas from which to retrieve and translate attribtues.</param>
      /// <returns>A list of all attributes that should be applied to the target class.</returns>
      private IEnumerable<SyntaxNode> GetClassAttributesWithTranslation(INamedTypeSymbol sourceClass, EventSourceTypeInfo baseTypeInfo)
      {
         foreach (var attributeData in sourceClass.GetAttributes())
         {
            if (attributeData.AttributeClass.Name.Equals(TemplateEventSourceAttributeName))
            {
               yield return m_generator.Attribute(baseTypeInfo.EventSourceAttributeType.GetFullName(),
                  attributeData.ConstructorArguments.Select(arg => m_generator.AttributeArgument(m_generator.LiteralExpression(arg.Value)))
                  .Concat(
                     attributeData.NamedArguments.Where(arg => !typeof(GenerationOptions).GetProperties().Any(p => p.Name.Equals(arg.Key)))
                     .Select(arg => m_generator.AttributeArgument(arg.Key, m_generator.LiteralExpression(arg.Value.Value)))
                  )
               );
            }
            else
            {
               yield return m_generator.Attribute(attributeData);
            }
         }
      }

      private TemplateEventMethodInfo TranslateMethodAttributes(IMethodSymbol sourceMethod, EventSourceTypeInfo eventSourceTypeInfo, CollectedGenerationInfo overloads)
      {
         List<SyntaxNode> attributes = new List<SyntaxNode>();

         SyntaxNode eventAttribute = null;
         int? eventId = null;
         
         // The sourceMethod symbol may originate from a compilation of a project other than the one that contains the source code, for 
         // example in the case of a derived class. We must get the method from the correct semantic model (belonging to the project defining the method)
         // to be able to get the ApplicationSyntaxReferences for the attributes (apparently).
         var methodDocument = m_document.Project.Solution.GetDocument(sourceMethod.DeclaringSyntaxReferences.First().SyntaxTree);
         if (methodDocument == null)
            throw new CodeGeneratorException(sourceMethod, $"Cannot find the document containing the method {sourceMethod.Name}.");

         var semanticModel = ThreadHelper.JoinableTaskFactory.Run(() => methodDocument.GetSemanticModelAsync());
         sourceMethod = (IMethodSymbol)semanticModel.GetDeclaredSymbol(sourceMethod.DeclaringSyntaxReferences.First().GetSyntax());

         foreach (AttributeData attributeData in sourceMethod.GetAttributes())
         {
            var attributeClass = attributeData.AttributeClass;

            if (attributeClass.Name.Equals(TemplateEventAttributeName) || attributeClass.Equals(eventSourceTypeInfo.EventAttributeType))
            {               
               SyntaxNode attributeSyntax = attributeData.ApplicationSyntaxReference?.GetSyntax();
               if (attributeSyntax == null)
               {
                  throw new CodeGeneratorException(sourceMethod, $"Cannot find the source file containing the method {sourceMethod.Name}. The source code must be available for any Template EventSource class to participate in generation. Is the project unloaded?");
               }
              
               overloads.AddConstants(attributeSyntax, semanticModel, eventSourceTypeInfo);

               attributeSyntax = m_generator.Attribute(eventSourceTypeInfo.EventAttributeType.GetFullName(), m_generator.GetAttributeArguments(attributeSyntax));
                  
               attributes.Add(attributeSyntax);


               TypedConstant eventIdArgument = attributeData.ConstructorArguments.FirstOrDefault();
               if (attributeData.ConstructorArguments.Length == 0)
                  throw new CodeGeneratorException(sourceMethod, $"The {attributeData.AttributeClass.Name} attribute must have an event ID as its first argument.");

               if (!(eventIdArgument.Value is int))
                  throw new CodeGeneratorException(sourceMethod, $"The first argument to the {attributeData.AttributeClass.Name} attribute must be of type Int32.");

               eventId = (int)eventIdArgument.Value;
               eventAttribute = attributeSyntax;
            }
            else
            {
               attributes.Add(CreateAttribute(attributeData));                  
            }
         }

         if (eventAttribute == null)
            throw new CodeGeneratorException(sourceMethod, $"Internal error; Unable to find EventAttribute or TemplateEventAttribute on method {sourceMethod.Name}");

         if (eventId == null)
            throw new CodeGeneratorException(sourceMethod, $"Unable to determine EventId for method {sourceMethod.Name}");

         return new TemplateEventMethodInfo(attributes, eventId.Value);
      }

      private SyntaxTriviaList CreateEndRegionTriviaList()
      {
         return
            SF.TriviaList(
               SF.EndOfLine(Environment.NewLine),
               SF.EndOfLine(Environment.NewLine),
               SF.Trivia(
                  SF.EndRegionDirectiveTrivia(true)
               )
            );
      }

      private SyntaxTriviaList CreateRegionTriviaList(string regionName)
      {
         return
            SF.TriviaList(
               SF.Trivia(
                  SF.RegionDirectiveTrivia(true)
                     .WithEndOfDirectiveToken(
                        SF.Token(
                           SF.TriviaList(SF.PreprocessingMessage(regionName)),
                           SyntaxKind.EndOfDirectiveToken,
                           SF.TriviaList()
                        )
                     ).NormalizeWhitespace()
               ),
               SF.EndOfLine(Environment.NewLine)
            );
      }

      private SyntaxTriviaList CreateWarningComment()
      {
         return
            SF.TriviaList(
               SF.Comment("/*****************************************************************/"),
               SF.EndOfLine(Environment.NewLine),
               SF.Comment("/* WARNING! THIS CODE IS AUTOMATICALLY GENERATED! DO NOT MODIFY! */"),
               SF.EndOfLine(Environment.NewLine),
               SF.Comment("/*****************************************************************/"),
               SF.EndOfLine(Environment.NewLine)
            );
      }

      private IEnumerable<SyntaxNode> CreateSingletonProperty(INamedTypeSymbol sourceClass, GenerationOptions options)
      {
         yield return m_generator.FieldDeclaration(
            name: "s_instance",
            type: m_generator.IdentifierName(options.TargetClassName),
            accessibility: Accessibility.Private,
            modifiers: DeclarationModifiers.Static | DeclarationModifiers.ReadOnly,
            initializer: m_generator.ObjectCreationExpression(m_generator.IdentifierName(options.TargetClassName))
         ).WithLeadingTrivia(CreateRegionTriviaList("Singleton Accessor")).AddLeadingTrivia(CreateWarningComment());
         
         yield return m_generator.PropertyDeclaration(
            name: "Log",
            type: m_generator.IdentifierName(sourceClass.Name),
            accessibility: Accessibility.Public,
            modifiers: DeclarationModifiers.Static | DeclarationModifiers.ReadOnly,
            getAccessorStatements: new[]
            {
               m_generator.ReturnStatement(m_generator.IdentifierName("s_instance"))
            }
         ).WithLeadingTrivia(CreateWarningComment()).WithTrailingTrivia(CreateEndRegionTriviaList().Add(SF.EndOfLine(Environment.NewLine)).Add(SF.EndOfLine(Environment.NewLine)));
      }

      private SyntaxNode CreateAttribute(AttributeData attributeData)
      {
         // This method should probably not be needed, but creating an attribute from attribute using the SyntaxFactory
         // seems to create invalid attribute declarations when it has enum parameters, since a cast is needed to generate the
         // correct code. Here we return the syntax if available, or otherwise copy all the arguments with casts to the appropriate type.
         SyntaxNode attributeSyntax = attributeData.ApplicationSyntaxReference?.GetSyntax();
         if (attributeSyntax != null)
            return attributeSyntax;

         attributeSyntax = m_generator.Attribute(
            m_generator.TypeExpression(attributeData.AttributeClass));
         
         foreach (var ctorArg in attributeData.ConstructorArguments)
         {
            attributeSyntax = m_generator.AddAttributeArguments(attributeSyntax,
               new[] 
               {
                  m_generator.CastExpression(
                     ctorArg.Type,
                     m_generator.LiteralExpression(ctorArg.Value)
                  ).WithAdditionalAnnotations(Simplifier.Annotation)
               }
            );
         }

         foreach (var namedArg in attributeData.NamedArguments)
         {
            attributeSyntax = m_generator.AddAttributeArguments(attributeSyntax,
               new[]
               {
                  m_generator.AttributeArgument(namedArg.Key,
                     m_generator.CastExpression(
                        namedArg.Value.Type,
                        m_generator.LiteralExpression(namedArg.Value.Value)
                     ).WithAdditionalAnnotations(Simplifier.Annotation)
                  )
               }
            );
         }

         return attributeSyntax;
         //List<SyntaxNode> arguments = null;
         //m_generator.
      }

      #endregion
   }
}
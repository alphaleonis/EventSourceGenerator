using Alphaleonis.Vsx;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Alphaleonis.EventSourceGenerator
{
   partial class EventSourceGenerator
   {
      private sealed class CollectedGenerationInfo
      {
         #region Private Fields

         private readonly EventSourceTypeInfo m_eventSourceTypeInfo;
         private readonly List<WriteEventOverloadInfo> m_overloads = new List<WriteEventOverloadInfo>();
         private readonly Dictionary<string, SyntaxNode> m_keywords = new Dictionary<string, SyntaxNode>();
         private readonly Dictionary<string, SyntaxNode> m_opcodes = new Dictionary<string, SyntaxNode>();
         private readonly Dictionary<string, SyntaxNode> m_tasks = new Dictionary<string, SyntaxNode>();

         #endregion

         #region Constructor

         public CollectedGenerationInfo(EventSourceTypeInfo eventSourceTypeInfo)
         {
            if (eventSourceTypeInfo == null)
               throw new ArgumentNullException("eventSourceTypeInfo", "eventSourceTypeInfo is null.");

            m_eventSourceTypeInfo = eventSourceTypeInfo;
         }

         #endregion

         #region Properties

         public Dictionary<string, SyntaxNode> Keywords
         {
            get
            {
               return m_keywords;
            }
         }

         public Dictionary<string, SyntaxNode> Opcodes
         {
            get
            {
               return m_opcodes;
            }
         }

         public IReadOnlyList<WriteEventOverloadInfo> Overloads
         {
            get
            {
               return m_overloads;
            }
         }

         public Dictionary<string, SyntaxNode> Tasks
         {
            get
            {
               return m_tasks;
            }
         }

         #endregion

         #region Methods

         public bool TryAdd(WriteEventOverloadInfo overload)
         {
            if (overload.Parameters.Any(p => !p.IsSupported))
               return false;

            if (m_eventSourceTypeInfo.WriteEventOverloads.Any(ol => ol.Select(p => p.Type).SequenceEqual(overload.Parameters.Select(p => p.TargetType))))
               return false;

            if (m_overloads.Any(ol => ol.Parameters.Select(p => p.TargetType).SequenceEqual(overload.Parameters.Select(p => p.TargetType))))
               return false;

            m_overloads.Add(overload);

            return true;
         }

         public void AddConstants(SyntaxNode attribute, SemanticModel semanticModel, EventSourceTypeInfo eventSourceTypeInfo)
         {
            if (attribute == null)
               throw new ArgumentNullException("attribute", "attribute is null.");

            AttributeSyntax attributeSyntax;
            AttributeListSyntax attributeListSyntax = attribute as AttributeListSyntax;
            if (attributeListSyntax != null)
            {
               if (attributeListSyntax.Attributes.Count != 1)
                  throw new CodeGeneratorException(attribute, "Expected a single attribute in attribute list, but either none or more than one were found.");

               attributeSyntax = attributeListSyntax.Attributes.Single();
            }
            else
            {
               attributeSyntax = attribute as AttributeSyntax;
               if (attributeSyntax == null)
                  throw new CodeGeneratorException(attribute, $"SyntaxNode was not of expected type {typeof(AttributeSyntax).FullName}");
            }

            AddToDictionary(attributeSyntax, "Keywords", m_keywords, semanticModel, eventSourceTypeInfo.EventKeywordsType);
            AddToDictionary(attributeSyntax, "Opcode", m_opcodes, semanticModel, eventSourceTypeInfo.EventOpcodeType);
            AddToDictionary(attributeSyntax, "Task", m_tasks, semanticModel, eventSourceTypeInfo.EventTaskType);
         }

         private void AddToDictionary(AttributeSyntax attributeSyntax, string name, Dictionary<string, SyntaxNode> dictionary, SemanticModel semanticModel, INamedTypeSymbol predefinedType)
         {
            var argument = attributeSyntax.ArgumentList.Arguments.FirstOrDefault(arg => arg.NameEquals?.Name?.Identifier.Text == name);

            if (argument != null)
            {
               KeywordsExpressionCollectorVisitor visitor = new KeywordsExpressionCollectorVisitor();
                              
               foreach (var memberAccess in visitor.Visit(argument.Expression))
               {
                  TypeInfo ti = semanticModel.GetTypeInfo(memberAccess.Expression);
                  if (!predefinedType.Equals(ti.Type))
                     dictionary[memberAccess.Name.Identifier.Text] = memberAccess;
               }
            }
         }

         class KeywordsExpressionCollectorVisitor : CSharpSyntaxVisitor<IEnumerable<MemberAccessExpressionSyntax>>
         {
            public override IEnumerable<MemberAccessExpressionSyntax> DefaultVisit(SyntaxNode node)
            {
               throw new CodeGeneratorException(node, $"Unsupported node {node.Kind()} found in enum assignment expression.");
            }

            public override IEnumerable<MemberAccessExpressionSyntax> VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
            {
               return node.Expression.Accept(this);
            }

            public override IEnumerable<MemberAccessExpressionSyntax> VisitBinaryExpression(BinaryExpressionSyntax node)
            {
               switch (node.Kind())
               {
                  case SyntaxKind.BitwiseAndExpression:
                  case SyntaxKind.BitwiseOrExpression:
                  case SyntaxKind.ExclusiveOrExpression:
                  case SyntaxKind.AddExpression:
                     break;

                  default:
                     throw new CodeGeneratorException(node.GetLocation(), $"Unsupported binary expression {node.Kind()}. Only bitwise OR, AND, XOR and addition is allowed.");
               }

               return node.ChildNodes().SelectMany(n => Visit(n));
            }

            public override IEnumerable<MemberAccessExpressionSyntax> VisitLiteralExpression(LiteralExpressionSyntax node)
            {
               return Enumerable.Empty<MemberAccessExpressionSyntax>();
            }

            public override IEnumerable<MemberAccessExpressionSyntax> VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
               if (node.Kind() != SyntaxKind.SimpleMemberAccessExpression)
                  throw new CodeGeneratorException(node, $"Unsupported expression of type {node.Kind()} in enum assignment. Only simple member access is allowed.");

               return new[] { node };
            }
         }
         #endregion
      }
   }
}


using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Alphaleonis.EventSourceClassGenerator
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
                  throw new GenerationException(attribute.GetLocation(), "Expected a single attribute in attribute list, but either none or more than one were found.");

               attributeSyntax = attributeListSyntax.Attributes.Single();
            }
            else
            {
               attributeSyntax = attribute as AttributeSyntax;
               if (attributeSyntax == null)
                  throw new GenerationException($"SyntaxNode was not of expected type {typeof(AttributeSyntax).FullName}");
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
               MemberAccessExpressionSyntax memberAccess = argument.Expression as MemberAccessExpressionSyntax;
               IdentifierNameSyntax nameSyntax = memberAccess?.Expression as IdentifierNameSyntax;
               if (memberAccess == null || nameSyntax == null)
                  throw new GenerationException(attributeSyntax.GetLocation(), $"The assignment to the Keyword, Task and Opcode  arguments must be a simple member access expression.");

               TypeInfo ti = semanticModel.GetTypeInfo(memberAccess.Expression);
               if (!predefinedType.Equals(ti.Type))
                  dictionary[memberAccess.Name.Identifier.Text] = argument.Expression;
            }
         }

         #endregion
      }
   }
}


using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Alphaleonis.EventSourceClassGenerator
{
   internal class TimeSpanParameterConverter : IParameterConverter
   {
      private readonly SemanticModel m_semanticModel;
      private readonly SyntaxGenerator m_syntaxGenerator;

      public TimeSpanParameterConverter(SemanticModel semanticModel, SyntaxGenerator syntaxGenerator)
      {
         if (semanticModel == null)
            throw new ArgumentNullException("semanticModel", "semanticModel is null.");

         if (syntaxGenerator == null)
            throw new ArgumentNullException("syntaxGenerator", "syntaxGenerator is null.");

         m_syntaxGenerator = syntaxGenerator;
         m_semanticModel = semanticModel;
      }

      public SyntaxNode GetConversionExpression(SyntaxNode expression)
      {
         return m_syntaxGenerator.InvocationExpression(m_syntaxGenerator.MemberAccessExpression(expression, WellKnownMemberNames.ObjectToString), m_syntaxGenerator.LiteralExpression("c"));
      }

      public ITypeSymbol TargetType
      {
         get
         {
            return m_semanticModel.Compilation.GetSpecialType(SpecialType.System_String);
         }
      }

      public ITypeSymbol SourceType
      {
         get
         {
            return m_semanticModel.Compilation.GetTypeByMetadataName("System.TimeSpan");
         }
      }
   }
}

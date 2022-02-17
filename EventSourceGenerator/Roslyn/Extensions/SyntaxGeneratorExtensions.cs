using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaVSX.Roslyn
{
   public static class SyntaxGeneratorExtensions
   {
      public static T AddModifiers<T>(this SyntaxGenerator generator, T node, DeclarationModifiers modifiers) where T : SyntaxNode
      {
         return (T)generator.WithModifiers(node, generator.GetModifiers(node) | modifiers);
      }

      /// <summary>
      /// Creates a statement that checks if the specified identifier is equal to null, and if so throws an ArgumentNullException.
      /// </summary>
      public static SyntaxNode ThrowIfNullStatement(this SyntaxGenerator generator, string identifier)
      {
         
         return generator.IfStatement(generator.ReferenceEqualsExpression(generator.IdentifierName(identifier), generator.NullLiteralExpression()),
            new SyntaxNode[]
            {
               generator.ThrowStatement(
                  generator.ObjectCreationExpression(
                     generator.QualifiedName(generator.IdentifierName("System"), generator.IdentifierName("ArgumentNullException")),
                     generator.InvocationExpression(generator.IdentifierName("nameof"), generator.IdentifierName(identifier))                     
                     
                  )
               )                        
            }
         );
      }

      public static SyntaxNode AddStatements(this SyntaxGenerator generator, SyntaxNode declaration, params SyntaxNode[] statements)
      {
         return generator.WithStatements(declaration, generator.GetStatements(declaration).Concat(statements));
      }      
   }
}

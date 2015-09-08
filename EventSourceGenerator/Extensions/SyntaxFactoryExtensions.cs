using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CS = Microsoft.CodeAnalysis.CSharp.Syntax;
using CSSF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Alphaleonis.EventSourceClassGenerator
{
   public static class SyntaxFactoryExtensions
   {
      public static SyntaxNode ArrayCreationExpression(this SyntaxGenerator generator, SyntaxNode type, SyntaxNode sizeExpression)
      {
         return ArrayCreationExpression(generator, type, new[] { sizeExpression });
      }

      public static SyntaxNode ArrayCreationExpression(this SyntaxGenerator generator, SyntaxNode type, IEnumerable<SyntaxNode> sizeExpression)
      {
         if (generator.NullLiteralExpression() is CS.ExpressionSyntax)
         {
            CS.TypeSyntax typeSyntax = type as CS.TypeSyntax;
            if (typeSyntax == null)
               throw new ArgumentException($"Invalid syntax node type; Expected {typeof(CS.TypeSyntax).FullName}.", "type");

            IEnumerable<CS.ExpressionSyntax> csSizeExpressions = sizeExpression.Select(exp => exp as CS.ExpressionSyntax);
            if (csSizeExpressions.Any(exp => exp == null))
               throw new ArgumentException($"Invalid syntax node type; Expected {typeof(CS.ExpressionSyntax).FullName}.", "sizeExpression");

            return CSSF.ArrayCreationExpression(
               CSSF.ArrayType(
                  typeSyntax,
                  CSSF.SingletonList(
                     CSSF.ArrayRankSpecifier(
                        CSSF.SeparatedList(csSizeExpressions)
                     )
                  )
               )
            );
         }
         else
            throw new ArgumentException("Not a CSharp ExpressionSyntax");         
      }
   }
}

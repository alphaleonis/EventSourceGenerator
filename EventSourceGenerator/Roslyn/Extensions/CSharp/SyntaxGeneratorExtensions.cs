using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alphaleonis.Vsx.Roslyn.CSharp
{
   public static class SyntaxGeneratorExtensions
   {
      public static SyntaxNode CatchClause(this SyntaxGenerator generator, IEnumerable<SyntaxNode> statements)
      {
         return SyntaxFactory.CatchClause().WithBlock(SyntaxFactory.Block(statements.Cast<StatementSyntax>()));
      }

      public static SyntaxNode CatchClause(this SyntaxGenerator generator, INamedTypeSymbol exceptionType, IEnumerable<SyntaxNode> statements)
      {
         return SyntaxFactory.CatchClause(
            SyntaxFactory.CatchDeclaration(
               (TypeSyntax)generator.TypeExpression(exceptionType)
            ),
            null,
            SyntaxFactory.Block(
               statements.Cast<StatementSyntax>()
            )
         );
      }

      public static SyntaxTriviaList CreateEndRegionTrivia(this SyntaxGenerator generator)
      {
         return
            SyntaxFactory.TriviaList(
               SyntaxFactory.EndOfLine(Environment.NewLine),
               SyntaxFactory.Trivia(
                  SyntaxFactory.EndRegionDirectiveTrivia(true)
               ),
               SyntaxFactory.EndOfLine(Environment.NewLine)
            );
      }

      public static SyntaxTriviaList CreateRegionTrivia(this SyntaxGenerator generator, string regionName)
      {
         return
            SyntaxFactory.TriviaList(
               SyntaxFactory.Trivia(
                  SyntaxFactory.RegionDirectiveTrivia(true)
                     .WithEndOfDirectiveToken(
                        SyntaxFactory.Token(
                           SyntaxFactory.TriviaList(SyntaxFactory.PreprocessingMessage(regionName)),
                           SyntaxKind.EndOfDirectiveToken,
                           SyntaxFactory.TriviaList()
                        )
                     ).NormalizeWhitespace()
               ),
               SyntaxFactory.EndOfLine(Environment.NewLine)
            );
      }

      public static SyntaxTrivia Comment(this SyntaxGenerator generator, string comment)
      {
         return SyntaxFactory.Comment(comment);
      }

      public static SyntaxTrivia NewLine(this SyntaxGenerator generator)
      {
         return SyntaxFactory.EndOfLine(Environment.NewLine);
      }

      public static SyntaxNode WithThisConstructorInitializer(this SyntaxGenerator generator, SyntaxNode constructor, IEnumerable<SyntaxNode> thisConstructorArguments)
      {
         ConstructorDeclarationSyntax ctor = constructor as ConstructorDeclarationSyntax;
         if (ctor == null)
            throw new ArgumentException($"Invalid SyntaxNode in call to {nameof(WithThisConstructorInitializer)}.");

         return ctor.WithInitializer(
            SyntaxFactory.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer,
               SyntaxFactory.ArgumentList(
                  SyntaxFactory.SeparatedList(
                     thisConstructorArguments.Select(arg => SyntaxFactory.Argument((ExpressionSyntax)arg))
                  )
               )
            )
         );
      }
   }
}

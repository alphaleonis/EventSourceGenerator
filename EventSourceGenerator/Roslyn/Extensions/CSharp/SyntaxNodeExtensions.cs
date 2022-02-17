using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Alphaleonis.Vsx.Roslyn.CSharp
{
   public static class SyntaxNodeExtensions
   {
      public static T AddLeadingTrivia<T>(this T node, IEnumerable<SyntaxTrivia> trivia) where T : SyntaxNode
      {
         return node.AddLeadingTrivia(trivia.ToSyntaxTriviaList());
      }

      public static T PrependLeadingTrivia<T>(this T node, IEnumerable<SyntaxTrivia> trivia) where T : SyntaxNode
      {
         return node.PrependLeadingTrivia(trivia.ToSyntaxTriviaList());
      }

      public static T AddTrailingTrivia<T>(this T node, IEnumerable<SyntaxTrivia> trivia) where T : SyntaxNode
      {
         return node.AddTrailingTrivia(trivia.ToSyntaxTriviaList());
      }

      public static T AddNewLineTrivia<T>(this T node) where T : SyntaxNode
      {
         return node.AddTrailingTrivia(SyntaxFactory.EndOfLine(Environment.NewLine));
      }

      public static T PrependLeadingTrivia<T>(this T node, params SyntaxTrivia[] trivia) where T : SyntaxNode
      {
         if (trivia.Length == 0)
         {
            return node;
         }

         return node.PrependLeadingTrivia((IEnumerable<SyntaxTrivia>)trivia);
      }

      public static T PrependLeadingTrivia<T>(this T node, SyntaxTriviaList trivia) where T : SyntaxNode
      {
         if (trivia.Count == 0)
         {
            return node;
         }

         return node.WithLeadingTrivia(trivia.Concat(node.GetLeadingTrivia()));
      }

      public static T AddLeadingTrivia<T>(this T node, params SyntaxTrivia[] trivia) where T : SyntaxNode
      {
         if (trivia.Length == 0)
         {
            return node;
         }

         return node.AddLeadingTrivia((IEnumerable<SyntaxTrivia>)trivia);

      }
      public static T AddLeadingTrivia<T>(this T node, SyntaxTriviaList trivia) where T : SyntaxNode
      {
         if (trivia.Count == 0)
         {
            return node;
         }

         return node.WithLeadingTrivia(node.GetLeadingTrivia().Concat(trivia));
      }


      public static T AddTrailingTrivia<T>(this T node, params SyntaxTrivia[] trivia) where T : SyntaxNode
      {
         if (trivia.Length == 0)
         {
            return node;
         }

         return node.AddTrailingTrivia((IEnumerable<SyntaxTrivia>)trivia);
      }

      public static T AddTrailingTrivia<T>(this T node, SyntaxTriviaList trivia) where T : SyntaxNode
      {
         if (trivia.Count == 0)
         {
            return node;
         }

         return node.WithTrailingTrivia(node.GetTrailingTrivia().Concat(trivia));
      }
   }
}

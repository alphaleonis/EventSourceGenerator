using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Alphaleonis.EventSourceGenerator
{
   internal interface IParameterConverter
   {
      SyntaxNode GetConversionExpression(SyntaxNode expression);
      ITypeSymbol TargetType { get; }
      ITypeSymbol SourceType { get; }
   }
}

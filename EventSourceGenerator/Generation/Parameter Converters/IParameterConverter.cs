using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Alphaleonis.EventSourceClassGenerator
{
   internal interface IParameterConverter
   {
      SyntaxNode GetConversionExpression(SyntaxNode expression);
      ITypeSymbol TargetType { get; }
      ITypeSymbol SourceType { get; }
   }
}

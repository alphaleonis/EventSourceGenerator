using Microsoft.CodeAnalysis;
using System;
using System.Runtime.Serialization;

namespace Alphaleonis.Vsx
{

   [Serializable]
   public class CodeGeneratorException : Exception
   {
      public CodeGeneratorException()
      {
         Line = -1;
         Column = -1;
      }

      public CodeGeneratorException(Location location, string message, Exception inner)
         : this(location == null ? -1 : location.GetMappedLineSpan().StartLinePosition.Line, location == null ? -1 : location.GetMappedLineSpan().StartLinePosition.Character, message, inner)
      {

      }

      public CodeGeneratorException(AttributeData location, string message)
         : this(location?.ApplicationSyntaxReference?.GetSyntax()?.GetLocation(), message)              
      {

      }

      public CodeGeneratorException(Location location, string message)
         : this(location == null ? -1 : location.GetMappedLineSpan().StartLinePosition.Line, location == null ? -1 : location.GetMappedLineSpan().StartLinePosition.Character, message)
      {

      }

      public CodeGeneratorException(SyntaxNode syntax, string message)
         : this(syntax.GetLocation(), message)
      {
      }

      public CodeGeneratorException(ISymbol symbol, string message)
         : this(symbol.Locations.Length == 0 ? null : symbol.Locations[0], message)
      {
      }

      public CodeGeneratorException(int line, string message) : this(line, -1, message) { }

      public CodeGeneratorException(int line, int column, string message)
         : base(message)
      {
         Line = line;
         Column = column;
      }

      public CodeGeneratorException(int line, int column, string message, Exception inner)
         : base(message, inner)
      {
         Line = line;
         Column = column;
      }

      public CodeGeneratorException(string message) : this(-1, -1, message) { }
      public CodeGeneratorException(string message, Exception inner) : this(-1, -1, message, inner) { }

      protected CodeGeneratorException(SerializationInfo info, StreamingContext context) : base(info, context)
      {
         Line = info.GetInt32(nameof(Line));
         Column = info.GetInt32(nameof(Column));
      }

      public int Line
      {
         get;
      }

      public int Column
      {
         get;
      }

      public override void GetObjectData(SerializationInfo info, StreamingContext context)
      {
         base.GetObjectData(info, context);
         info.AddValue(nameof(Line), Line);
         info.AddValue(nameof(Column), Column);
      }
   }
}

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
namespace Alphaleonis.EventSourceClassGenerator
{
   [Serializable]
   public class GenerationException : Exception
   {
      public Location Location { get; }

      public GenerationException()
      {
      }

      public GenerationException(string message)
         : base(message)
      {
      }

      public GenerationException(SyntaxNode syntax, string message)
         : this(syntax.GetLocation(), message)
      {
      }

      public GenerationException(ISymbol symbol, string message)
         : this(symbol.Locations.Length == 0 ? null : symbol.Locations[0], message)
      {
      }

      public GenerationException(Location location, string message)
         : this(message)
      {
         Location = location;
      }

      public GenerationException(string message, Exception inner) : base(message, inner) { }

      public GenerationException(Location location, string message, Exception inner) : base(message, inner)
      {
         Location = location;
      }

      protected GenerationException(SerializationInfo info, StreamingContext context)
         : base(info, context)
      {
         Location = (Location)info.GetValue("Location", typeof(Location));
      }

      public override void GetObjectData(SerializationInfo info, StreamingContext context)
      {
         base.GetObjectData(info, context);
         info.AddValue("Location", Location);
      }
   }
}

using System;
using System.Runtime.Serialization;

namespace Alphaleonis.Vsx.Roslyn
{

   [Serializable]
   public class TypeNotFoundException : Exception
   {
      public TypeNotFoundException()
      {
      }

      public TypeNotFoundException(string message)
          : base(message)
      {
      }

      public TypeNotFoundException(string message, Exception innerException)
          : base(message, innerException)
      {
      }

      protected TypeNotFoundException(SerializationInfo info, StreamingContext context)
          : base(info, context)
      {
      }
   }





}

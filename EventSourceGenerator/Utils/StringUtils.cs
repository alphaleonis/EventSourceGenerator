using Alphaleonis.EventSourceGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alphaleonis.Vsx
{
   public static class StringUtils
   {
      public static string Join(IEnumerable<string> strings, string separator, string lastSeparator = null, string quoteChar = null)
      {
         if (strings == null)
            throw new ArgumentNullException(nameof(strings), $"{nameof(strings)} is null.");

         StringBuilder sb = new StringBuilder();

         if (separator == null)
            separator = String.Empty;

         if (lastSeparator == null)
            lastSeparator = separator;

         if (quoteChar == null)
            quoteChar = String.Empty;

         foreach (var entry in strings.AsSmartEnumerable())
         {
            if (!entry.IsFirst)
            {
               if (entry.IsLast)
               {
                  sb.Append(lastSeparator);
               }
               else
               {
                  sb.Append(separator);
               }
            }

            sb.Append(quoteChar);
            sb.Append(entry.Value);
            sb.Append(quoteChar);
         }

         return sb.ToString();       
      }
   }

   

}

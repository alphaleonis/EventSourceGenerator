using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Text;
namespace Alphaleonis.EventSourceClassGenerator
{
   public abstract class BaseTextCodeGenerator : BaseCodeGenerator
   {
      protected override byte[] GenerateCode(string inputFilePath, string inputFileContent)
      {
         try
         {
            using (StringWriter writer = new StringWriter())
            {
               try
               {
                  GenerateCode(inputFilePath, inputFileContent, writer);
               }
               catch (GenerationException gex)
               {
                  int line = -1;
                  int column = -1;
                  if (gex.Location != null)
                  {
                     LinePosition startPosition = gex.Location.GetMappedLineSpan().StartLinePosition;
                     line = startPosition.Line;
                     column = startPosition.Character;
                  }

                  ReportError(gex.Message, line, column);
                  writer.WriteLine("#error Error generating code: \"{0}\"", gex.Message);
               }

               writer.Flush();

               //Get the Encoding used by the writer. We're getting the WindowsCodePage encoding, 
               //which may not work with all languages
               Encoding enc = Encoding.GetEncoding(writer.Encoding.WindowsCodePage);

               //Get the preamble (byte-order mark) for our encoding
               byte[] preamble = enc.GetPreamble();
               int preambleLength = preamble.Length;

               //Convert the writer contents to a byte array
               byte[] body = enc.GetBytes(writer.ToString());

               //Prepend the preamble to body (store result in resized preamble array)
               Array.Resize<byte>(ref preamble, preambleLength + body.Length);
               Array.Copy(body, 0, preamble, preambleLength, body.Length);

               //Return the combined byte array
               return preamble;
            }
         }
         catch (Exception e)
         {
            ReportError(e.ToString(), 1, 1);
            return null;
         }
      }

      protected abstract void GenerateCode(string inputFilePath, string inputFileContent, TextWriter writer);
   }
}

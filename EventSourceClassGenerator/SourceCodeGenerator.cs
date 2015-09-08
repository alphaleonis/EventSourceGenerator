using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using EnvDTE;
using EnvDTE80;

namespace Alphaleonis.EventSourceClassGenerator
{
   public class SourceCodeGenerator
   {
      private int m_indentLevel;
      private readonly TextWriter m_writer;
      private bool m_prependIndent = true;

      public SourceCodeGenerator(TextWriter writer)
      {
         m_writer = writer;
      }

      #region Indenting

      public IDisposable StartRegion(string regionName)
      {
         return new RegionScope(this, regionName);
      }

      public IDisposable StartBlock()
      {
         return StartBlock(String.Empty);
      }

      public IDisposable StartBlock(string header)
      {
         if (!String.IsNullOrEmpty(header))
            WriteLine(header);

         return new IndentScope(this, true);
      }

      public IDisposable StartBlock(string header, params object[] args)
      {
         return StartBlock(String.Format(header, args));
      }

      public IDisposable StartIndent()
      {
         return new IndentScope(this, false);
      }

      private string Indent
      {
         get
         {
            return new string(' ', m_indentLevel * 3);
         }
      }

      #endregion

      #region Low Level Output Methods

      public void WriteLine(object o)
      {
         Write(o);
         WriteLine();
      }

      public void WriteLine(string text)
      {
         Write(text + Environment.NewLine);
      }

      public void WriteLine(string format, params object[] args)
      {
         WriteLine(String.Format(CultureInfo.InvariantCulture, format, args));
      }

      public void Write(object o)
      {
         Write(String.Format(CultureInfo.InvariantCulture, "{0}", o));
      }

      public void Write(string text)
      {
         if (text != null)
         {
            for (int i = 0; i < text.Length; i++)
            {
               if (text[i] == '\r' && i < text.Length - 1 && text[i + 1] == '\n')
               {
                  m_writer.Write(Environment.NewLine);
                  i++;
                  m_prependIndent = true;
               }
               else if (text[i] == '\n')
               {
                  m_writer.Write(Environment.NewLine);
                  m_prependIndent = true;
               }
               else if (m_prependIndent)
               {
                  m_writer.Write(Indent);
                  m_writer.Write(text[i]);
                  m_prependIndent = false;
               }
               else
               {
                  m_writer.Write(text[i]);
               }
            }
         }
      }

      public void Write(string format, params object[] args)
      {
         Write(String.Format(CultureInfo.InvariantCulture, format, args));
      }

      public void WriteLine()
      {
         Write(Environment.NewLine);

      }

      #endregion

      internal IDisposable StartNamespace(string namespaceName)
      {
         return StartBlock("namespace {0}", namespaceName);
      }

      internal void WriteImport(EnvDTE80.CodeImport import)
      {
         if (!String.IsNullOrEmpty(import.Alias))
         {
            WriteLine("using {0} = {1};", import.Alias, import.Namespace);
         }
         else
         {
            WriteLine("using {0};", import.Namespace);
         }
      }
      
      public void WriteGeneratedCodeWarningComment()
      {
         WriteLine("/*************************************************************************************/");
         WriteLine("/* WARNING! THIS CODE IS AUTOMATICALLY GENERATED. DO NOT MODIFY THIS CODE FILE. ANY  */");
         WriteLine("/*          CHANGES MADE TO THIS FILE WILL BE OVERWRITTEN NEXT TIME GENERATION       */");
         WriteLine("/*          OCCURS.                                                                  */");
         WriteLine("/*************************************************************************************/");
      }


      public void WriteAccess(vsCMAccess access)
      {
         switch (access)
         {
            case vsCMAccess.vsCMAccessPublic:
               Write("public ");
               break;
            case vsCMAccess.vsCMAccessPrivate:
               Write("private ");
               break;
            case vsCMAccess.vsCMAccessProject:
            case vsCMAccess.vsCMAccessAssemblyOrFamily:
               Write("internal ");
               break;
            case vsCMAccess.vsCMAccessProtected:
               Write("protected ");
               break;
            case vsCMAccess.vsCMAccessDefault:
            case vsCMAccess.vsCMAccessWithEvents:
               Write("");
               break;
            case vsCMAccess.vsCMAccessProjectOrProtected:
               Write("protected internal ");
               break;
         }
      }      

      public void WriteAttribute(string attributeTypeName, IEnumerable<CodeAttributeArgument> arguments)
      {
         Write("[");
         Write(attributeTypeName);
         if (arguments != null && arguments.Any())
         {
            Write("(");
            
            foreach (var arg in arguments.AsSmartEnumerable())
            {
               if (!arg.IsFirst)
                  Write(", ");

               if (String.IsNullOrEmpty(arg.Value.Name))
               {
                  Write(arg.Value.Value);
               }
               else
               {
                  Write(arg.Value.Name);
                  Write(" = ");
                  Write(arg.Value.Value);
               }
            }
            Write(")");
         }

         Write("]");
      }

      #region Nested Types

      private class IndentScope : IDisposable
      {
         private readonly bool m_emitBlock;
         private readonly SourceCodeGenerator m_generator;

         public IndentScope(SourceCodeGenerator generator, bool emitBlock)
         {
            m_emitBlock = emitBlock;
            m_generator = generator;
            if (emitBlock)
            {
               m_generator.WriteLine("{");
            }
            m_generator.m_indentLevel++;
         }

         public void Dispose()
         {
            m_generator.m_indentLevel = Math.Max(0, m_generator.m_indentLevel - 1);
            if (m_emitBlock)
            {
               m_generator.WriteLine("}");               
            }
         }
      }

      private class RegionScope : IDisposable
      {
         private readonly SourceCodeGenerator m_generator;

         public RegionScope(SourceCodeGenerator generator, string regionName)
         {
            m_generator = generator;
            m_generator.WriteLine("#region {0}", regionName);
            m_generator.WriteLine();
         }

         public void Dispose()
         {
            m_generator.WriteLine();
            m_generator.WriteLine("#endregion");
            m_generator.WriteLine();
         }
      }

      #endregion
   }
}

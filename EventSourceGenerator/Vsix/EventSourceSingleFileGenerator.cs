using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Simplification;
using Alphaleonis.Vsx;

namespace Alphaleonis.EventSourceGenerator
{
   [ComVisible(true)]
   [Guid("C0B0D7EA-A1E9-46B7-B7B2-93EC5249A719")]
   [Microsoft.VisualStudio.Shell.CodeGeneratorRegistration(typeof(EventSourceSingleFileGenerator), "C# Event Source Class Generator", VSLangProj80.vsContextGuids.vsContextGuidVCSProject, GeneratesDesignTimeSource = true, GeneratorRegKeyName = Name)]
   [Microsoft.VisualStudio.Shell.ProvideObject(typeof(EventSourceSingleFileGenerator))]
   public class EventSourceSingleFileGenerator : RoslynCodeGeneratorBase
   {
#pragma warning disable 0414
      //The name of this generator (use for 'Custom Tool' property of project item)
      internal const string Name = "EventSourceGenerator";
#pragma warning restore 0414

      public EventSourceSingleFileGenerator()
      {
      }

      
      protected override async Task<Document> GenerateCodeAsync(Document document)
      {
         ReportProgress(0, 100);
         var result = await EventSourceGenerator.GenerateEventSourceImplementations(document);
         Document doc = document.Project.AddDocument("GeneratedFile.cs", result);         
         doc = await Simplifier.ReduceAsync(doc);
         ReportProgress(100, 100);
         return doc;
      }

      protected override string GetDefaultExtension()
      {
         return ".g.cs";
      }

      
   }
}
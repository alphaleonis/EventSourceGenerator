using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Threading;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Options;

namespace Alphaleonis.EventSourceClassGenerator
{
   [ComVisible(true)]
   [Guid("C0B0D7EA-A1E9-46B7-B7B2-93EC5249A719")]
   [Microsoft.VisualStudio.Shell.CodeGeneratorRegistration(typeof(EventSourceSingleFileGenerator), "C# Event Source Class Generator", VSLangProj80.vsContextGuids.vsContextGuidVCSProject, GeneratesDesignTimeSource = true, GeneratorRegKeyName = Name)]
   [Microsoft.VisualStudio.Shell.ProvideObject(typeof(EventSourceSingleFileGenerator))]
   public class EventSourceSingleFileGenerator : BaseTextCodeGenerator
   {
#pragma warning disable 0414
      //The name of this generator (use for 'Custom Tool' property of project item)
      internal const string Name = "EventSourceClassGenerator";
#pragma warning restore 0414

      public EventSourceSingleFileGenerator()
      {
      }

      protected override void GenerateCode(string inputFilePath, string inputFileContent, TextWriter writer)
      {
         Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () => await GenerateCodeAsync(inputFilePath, inputFileContent, writer));
      }

      private async Task GenerateCodeAsync(string inputFilePath, string inputFileContent, TextWriter writer)
      {
         IComponentModel componentModel = (IComponentModel)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SComponentModel));
         var workspace = componentModel.GetService<VisualStudioWorkspace>();

         Solution solution = workspace.CurrentSolution;
         DocumentId documentId = solution.GetDocumentIdsWithFilePath(inputFilePath).FirstOrDefault();
         if (documentId == null)
            throw new GenerationException(String.Format("Unable to find a document matching the file path \"{0}\".", inputFilePath));

         var document = solution.GetDocument(documentId);

         var result = await EventSourceGenerator.GenerateEventSourceImplementations(document);
         
         Document doc = document.Project.AddDocument("GeneratedFile.cs", result);                  
         doc = Simplifier.ReduceAsync(doc).Result;
         
         writer.WriteLine(await doc.GetTextAsync());
                  
         ReportProgress(0, 100);
      }

      protected override string GetDefaultExtension()
      {
         return ".g.cs";
      }
   }
}
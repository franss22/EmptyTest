using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;

namespace EmptyTest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EmptyTestAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "EmptyTest";

        //Defining localized names and info for the diagnostic
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Test Smells";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            // Controls analysis of generated code (ex. EntityFramework Migration) None means generated code is not analyzed
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.EnableConcurrentExecution();

            //Registers callback to start analysis
            context.RegisterCompilationStartAction(FindTestingClass);
        }

        private static void FindTestingClass(CompilationStartAnalysisContext context)
        {

            // Get the attribute object from the compilation
            var testClassAttr = context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute");
            if (testClassAttr is null) { return; }
            var testMethodAttr = context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute");
            if (testMethodAttr is null) { return; }



            // We register a Symbol Start Action to filter all test classes and their test methods
            context.RegisterSymbolStartAction((ctx) =>
            {
                var methodSymbol = (IMethodSymbol)ctx.Symbol;


                //Check if the container class is [TestClass]
                var container = methodSymbol.ContainingSymbol;
                if (container is null) { return; }
                var isTestClass = false;
                foreach (var attr in container.GetAttributes())
                {
                    if (attr.AttributeClass.Name.Equals("TestClassAttribute"))
                    {
                        isTestClass = true; break;
                    }
                }
                if (!isTestClass) { return; }

                //Check if method is [TestMethod]
                foreach (var attr in methodSymbol.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, testMethodAttr))
                    {
                        // If it's a test method in a test class, we check it internally to see if it has no statements
                        ctx.RegisterOperationBlockAction(AnalyzeMethodBlockIOperation);
                        break;
                    }
                }
            }
            , SymbolKind.Method);
        }

        private static void AnalyzeMethodBlockIOperation(OperationBlockAnalysisContext context)
        {

            foreach (var block in context.OperationBlocks)//we look for the method body
            {
                if (block.Kind != OperationKind.Block) { continue; }
                if (block.Descendants().Count() == 0)//if the method body has no operations, it is empty
                {
                    var methodBlock = (IMethodBodyOperation)block.Parent;
                    var methodSyntax = (MethodDeclarationSyntax)methodBlock.Syntax;
                    var diagnostic = Diagnostic.Create(Rule, methodSyntax.Identifier.GetLocation(), methodSyntax.Identifier.ToString());
                    context.ReportDiagnostic(diagnostic);
                }

            }

        }

    }
}
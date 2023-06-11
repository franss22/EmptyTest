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

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(FindTestingClass);
        }

        private static void FindTestingClass(CompilationStartAnalysisContext context)
        {
            var testMethodAttr = context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute");
            var testClassAttr = context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute");

            if (testMethodAttr is null) { return; }

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
                        ctx.RegisterOperationBlockAction(AnalyzeMethodBlockIOperation);
                        break;
                    }
                }
            }
            , SymbolKind.Method);
        }

        private static void AnalyzeMethodBlockIOperation(OperationBlockAnalysisContext context)
        {

            foreach (var block in context.OperationBlocks)
            {
                if (block.Kind != OperationKind.Block) { continue; }
                if (block.Descendants().Count() == 0)
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
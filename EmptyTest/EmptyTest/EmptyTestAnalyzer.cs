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
            context.RegisterSymbolStartAction(FindTestingClass, SymbolKind.NamedType);
        }

        private static void FindTestingClass(SymbolStartAnalysisContext context)
        {
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            var classAttributes = namedTypeSymbol.GetAttributes();
            var isTestClass = false;
            foreach (var item in classAttributes)
            {
                if (item.AttributeClass.Name.Equals("TestClassAttribute"))
                {
                    isTestClass = true; break;
                }
            }
            if (!isTestClass) { return; }
            foreach (var item in namedTypeSymbol.GetMembers())
            {
                if (item.Kind != SymbolKind.Method) { continue; }
                var methodSymbol = (IMethodSymbol)item;
                var attributes = methodSymbol.GetAttributes();
                foreach (var attr in attributes)
                {
                    if (attr.AttributeClass.Name.Equals("TestMethodAttribute"))
                    {
                        context.RegisterSyntaxNodeAction(AnalyzeMethodSyntax, SyntaxKind.MethodDeclaration); break;
                    }
                }

            }

        }

        private static void AnalyzeMethodSyntax(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;
            var body = methodDeclaration.Body;

            if (body.Statements.Count == 0)
            {
                var diagnostic = Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier.ToString());
                context.ReportDiagnostic(diagnostic);
            }


        }
    }
}

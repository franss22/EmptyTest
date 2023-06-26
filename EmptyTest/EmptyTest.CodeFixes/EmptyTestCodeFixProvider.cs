using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EmptyTest
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EmptyTestCodeFixProvider)), Shared]
    public class EmptyTestCodeFixProvider : CodeFixProvider
    {
        private const string SystemNotImplementedExceptionTypeName = "System.NotImplementedException";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(EmptyTestAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var methodDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: c => AddNotImplementedException(context.Document, methodDeclaration, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private async Task<Document> AddNotImplementedException(Document document, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
        {

            // Compute new uppercase name.
            var bodyBlockSyntax = methodDeclaration.Body;
            var bodyStatements = bodyBlockSyntax.Statements;
            //var newBlock = bodyStatements.Insert(0, );

            // Get the symbol representing the type to be renamed.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            //var typeSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);

            var notImplementedExceptionType = semanticModel.Compilation.GetTypeByMetadataName(SystemNotImplementedExceptionTypeName);

            var generator = SyntaxGenerator.GetGenerator(document);

            var trivia = bodyBlockSyntax.DescendantTrivia().ToList();
            


            var throwStatement = (StatementSyntax) generator.ThrowStatement(generator.ObjectCreationExpression(
                generator.TypeExpression(notImplementedExceptionType))).WithLeadingTrivia(trivia).NormalizeWhitespace().WithAdditionalAnnotations(Simplifier.AddImportsAnnotation);


            var newBlockStatements = bodyStatements.Insert(0, throwStatement);

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newBodyBlockSyntax = bodyBlockSyntax.AddStatements(throwStatement).NormalizeWhitespace();
            var newDocument = document.WithSyntaxRoot(root.ReplaceNode(bodyBlockSyntax, newBodyBlockSyntax));

            // Return the new solution with the now-uppercase type name.
            return newDocument;
        }
    }
}

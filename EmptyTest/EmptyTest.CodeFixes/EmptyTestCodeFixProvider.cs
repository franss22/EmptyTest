using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
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
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the method declaration identified by the diagnostic.
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


            //NotImplementedException class
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var notImplementedExceptionType = semanticModel.Compilation.GetTypeByMetadataName(SystemNotImplementedExceptionTypeName);

            //creating the new body with the added "raise new NotImplementedException();" at the end.
            //Method statements
            var bodyBlockSyntax = methodDeclaration.Body;
            var bodyStatements = bodyBlockSyntax.Statements;
            var endBrace = bodyBlockSyntax.CloseBraceToken;
   
            //We generate "raise new NotImplementedException();"
            
            var generator = SyntaxGenerator.GetGenerator(document);
            
            var throwStatement = (StatementSyntax)generator.ThrowStatement(generator.ObjectCreationExpression(
                generator.TypeExpression(notImplementedExceptionType))).WithLeadingTrivia(endBrace.LeadingTrivia).WithAdditionalAnnotations(Simplifier.AddImportsAnnotation, Formatter.Annotation);

            //We add to the start of the statement block
            var newBlockStatements = bodyStatements.Insert(0, throwStatement);
            var newBodyBlockSyntax = bodyBlockSyntax.WithCloseBraceToken(endBrace.WithLeadingTrivia()).WithStatements(newBlockStatements);



            //Editing the document
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            //var newroot = root.ReplaceTokens(bodyBlockSyntax.Statements, newBlockStatements);
            //var newDocument = document.WithSyntaxRoot(newroot);
            var newDocument = document.WithSyntaxRoot(root.ReplaceNode(bodyBlockSyntax, newBodyBlockSyntax));

            return newDocument;
        }
    }
}

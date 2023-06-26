using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis;
using Microsoft;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.Testing.ReferenceAssemblies;
using VerifyCS = EmptyTest.Test.CSharpCodeFixVerifier<
    EmptyTest.EmptyTestAnalyzer,
    EmptyTest.EmptyTestCodeFixProvider>;
using System.Collections.Immutable;
using TestReading;
using System;

namespace EmptyTest.Test
{
    [TestClass]
    public class EmptyTestUnitTest

    {

        private readonly ReferenceAssemblies UnitTestingAssembly = ReferenceAssemblies.NetFramework.Net48.Default.AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.VisualStudio.UnitTesting", "11.0.50727.1"))).AddAssemblies(ImmutableArray.Create("Microsoft.VisualStudio.UnitTesting"));

        private readonly TestReader testReader = new TestReader();
        //No diagnostics expected to show up
        [TestMethod]
        public async Task EmptyProgram()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task EmptyTestReported()
        {
            var testFile = @"Corpus\Emptytest.cs";
            var expected = VerifyCS.Diagnostic("EmptyTest").WithSpan(9, 21, 9, 32).WithArguments("TestMethod1");
            await new VerifyCS.Test
            {
                TestCode = testReader.ReadTest(testFile),
                ExpectedDiagnostics = { expected },
                ReferenceAssemblies = UnitTestingAssembly
            }.RunAsync();
        }

        
        [TestMethod]
        public async Task EmptyTestWithCommentReported()
        {
            var testFile = @"Corpus\EmptyTestWithComments.cs";
            var expected = VerifyCS.Diagnostic("EmptyTest").WithSpan(9, 21, 9, 32).WithArguments("TestMethod1");
            await new VerifyCS.Test
            {
                TestCode = testReader.ReadTest(testFile),
                ExpectedDiagnostics = { expected },
                ReferenceAssemblies = UnitTestingAssembly
            }.RunAsync();
        }


        [TestMethod]
        public async Task NotEmptyTestNotReported()
        {
            var testFile = @"Corpus\TestNotEmpty.cs";
            await new VerifyCS.Test
            {
                TestCode = testReader.ReadTest(testFile),
                ExpectedDiagnostics = { },
                ReferenceAssemblies = UnitTestingAssembly
            }.RunAsync();
        }

        [TestMethod]
        public async Task EmptyMethodNotTestMethodNotReported()
        {
            var testFile = @"Corpus\NotTestMethod.cs";
            await new VerifyCS.Test
            {
                TestCode = testReader.ReadTest(testFile),
                ExpectedDiagnostics = { },
                ReferenceAssemblies = UnitTestingAssembly
            }.RunAsync();
        }

        [TestMethod]
        public async Task EmptyTestNotTestClassNotReported()
        {
            var testFile = @"Corpus\EmptyTestNotTestClass.cs";
            await new VerifyCS.Test
            {
                TestCode = testReader.ReadTest(testFile),
                ExpectedDiagnostics = { },
                ReferenceAssemblies = UnitTestingAssembly
            }.RunAsync();
        }

        [TestMethod]
        public async Task EmptyTestFixed()
        {
            var testFile = @"Corpus\Emptytest.cs";
            var fixedFile = @"Corpus\EmptytestFixed.cs";

            var expected = VerifyCS.Diagnostic("EmptyTest").WithSpan(9, 21, 9, 32).WithArguments("TestMethod1");
            await new VerifyCS.Test
            {
                TestCode = testReader.ReadTest(testFile),
                FixedCode = testReader.ReadTest(fixedFile),
                ExpectedDiagnostics = { expected },
                ReferenceAssemblies = UnitTestingAssembly
            }.RunAsync();
        }

        [TestMethod]
        public async Task EmptyTestWithCommentFixed()
        {
            var testFile = @"Corpus\EmptyTestWithComments.cs";
            var fixedFile = @"Corpus\EmptyTestWithCommentsFixed.cs";

            var expected = VerifyCS.Diagnostic("EmptyTest").WithSpan(9, 21, 9, 32).WithArguments("TestMethod1");
            await new VerifyCS.Test
            {
                TestCode = testReader.ReadTest(testFile),
                FixedCode = testReader.ReadTest(fixedFile),
                ExpectedDiagnostics = { expected },
                ReferenceAssemblies = UnitTestingAssembly
            }.RunAsync();
        }

    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace TestReading
{
    public class TestReader
    {
        string basePath;
        public TestReader()
        {
            string workingDirectory = Environment.CurrentDirectory;
            basePath = Directory.GetParent(workingDirectory).Parent.Parent.FullName; ;
        }

        public string ReadTest(string fileName)
        {
            return File.ReadAllText(Path.Combine(basePath, fileName));
        }
    }
}

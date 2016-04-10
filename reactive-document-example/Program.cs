using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace reactive_document_example
{
    class Program
    {
        static void Main(string[] args)
        {
            Run().Wait();
        }

        public static async Task Run()
        {
            var source = Observable
                .Range(0, 10).
                Select(x => (byte) x);

            using (var document = new Document(new MemoryStream()))
            {
                await document.Write(source).LastOrDefaultAsync();
            }
        }
    }
}
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

namespace reactive_document_example
{


    class Program
    {
        static void Main(string[] args)
        {
            var source = Observable
                .Range(0, 20)
                .Select(x => (byte) x).Select(x => Observable.Empty<byte>()
                    .Delay(TimeSpan.FromMilliseconds(100))
                    .StartWith(x))
                .Concat()
                .Do(x => Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] Source {x}"));

            var document = new Document(new MemoryStream());

            document.Write(source);

            document.Read().ToArray().ToTask().Wait();
        }
    }
}
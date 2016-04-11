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
            Task.Run(async () =>
            {
                var source = Observable.Range(0, 15).Select(x => (byte) x);
                var expected = await source.ToArray();

                using (var document = new Document(new MemoryStream(), true))
                {
                    var writeTask = document.Write(
                        source
                            .Select(x => Observable.Empty<byte>()
                                .Delay(TimeSpan.FromMilliseconds(100))
                                .StartWith(x))
                            .Concat()
                            .Do(x => Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId:D2}] Source {x}")));

                    await Task.Delay(500);

                    var result = await document.Read().ToArray();

                    await writeTask;

                    CollectionAssert.AreEqual(expected, result);

                    Console.WriteLine("Read data equals expected data");
                }
            }).Wait();
        }
    }
}
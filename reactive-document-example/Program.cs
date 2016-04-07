using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace reactive_document_example
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                var source = Observable.Range(0, 10).SelectMany(BitConverter.GetBytes);
                var expected = await source.ToArray();
                using (var document = new Document(new MemoryStream()))
                {
                    await document.Write(source);
                    var result = await document.Read().ToArray();

                    var same = expected.Zip(result, (a, b) => a == b).All(x => x);
                    Console.WriteLine($"await Write, await Read: {(same ? "Pass" : "Fail")}");
                }

            }).Wait();
        }
    }
}

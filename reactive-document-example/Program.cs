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
            var document = new Document(new MemoryStream());

            //document.Write()
        }
    }
}
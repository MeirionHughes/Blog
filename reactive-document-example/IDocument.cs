using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace reactive_document_example
{
    /// <summary>
    /// A reactive stream of bytes that can be read-from and written-to.
    /// </summary>
    /// <remarks>
    /// Disposing a document with an active writer, disposes the subscription
    /// </remarks>
    public interface IDocument : IDisposable
    {
        /// <summary>
        /// Subscribes to the source and writes the stream to the document
        /// </summary>
        /// <param name="source">observable to subscribe to for data</param>
        /// <returns>observable data that has succesfully been written</returns>
        IObservable<byte> Write(IObservable<byte> source);

        /// <summary>
        /// Reads the document and joins the current writer if there is one. 
        /// </summary>
        /// <returns>An observable that reads the document data</returns>
        IObservable<byte> Read();
    }
}

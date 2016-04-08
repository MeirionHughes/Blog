using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace reactive_document_example
{
    [TestFixture]
    public class DocumentUnitTests
    {
        private IObservable<byte> _source;

        [SetUp]
        public void Setup()
        {
            _source = Observable.Range(0, 10).SelectMany(BitConverter.GetBytes);
        }

        [Test]
        public async Task given_document_when_disposed_and_write_called_then_throws_exception()
        {
            var expected = await _source.ToArray();
            using (var stream = new MemoryStream())
            {
                var document = new Document(new MemoryStream());

                document.Dispose();

                Assert.Throws<AggregateException>(() => { document.Write(_source).Wait(); });
            }
        }

        [Test]
        public async Task given_document_writing_data_writes_to_stream()
        {
            var expected = await _source.ToArray();
            using (var stream = new MemoryStream())
            {
                using (var document = new Document(stream, true))
                {
                    await document.Write(_source);
                }
                
                var result = new byte[stream.Length];
                stream.Seek(0, SeekOrigin.Begin);
                stream.Read(result, 0, result.Length);
                    
                CollectionAssert.AreEqual(expected, result);
            }
        }
    }
}
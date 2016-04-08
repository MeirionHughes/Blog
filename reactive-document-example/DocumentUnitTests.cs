using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
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
            var document = new Document(new MemoryStream());

            document.Dispose();

            Assert.Throws<AggregateException>(() => { document.Write(_source).Wait(); });
        }

        [Test]
        public async Task given_document_when_disposed_and_disposed_called_then_returns_quitely()
        {
            var document = new Document(new MemoryStream());

            document.Dispose();
            document.Dispose();
        }

        [Test]
        public async Task given_document_and_document_is_empty_and_document_disposed_when_read_called_throws_exception()
        {
            var document = new Document(new MemoryStream());

            var result = await document.Read().ToArray();

            Assert.That(result.Length, Is.EqualTo(0));
        }

        [Test]
        public async Task given_document_when_disposed_and_read_called_then_throws_exception()
        {
            var document = new Document(new MemoryStream());

            document.Dispose();

            Assert.ThrowsAsync<ObjectDisposedException>(
                async () =>
                {
                    await document.Read().ToArray();
                });
        }

        [Test]
        public async Task given_document_when_write_data_then_stream_contains_data()
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

        [Test]
        public async Task given_document_when_write_cold_observable_then_stream_contains_data()
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

        [Test]
        public async Task given_document_when_write_hot_observable_then_stream_contains_data()
        {
            var expected = await _source.ToArray();
            var hotSource = _source.Delay(TimeSpan.FromMilliseconds(200)).Publish();

            hotSource.Connect();

            using (var stream = new MemoryStream())
            {
                using (var document = new Document(stream, true))
                {
                    await document.Write(hotSource);
                }

                var result = new byte[stream.Length];
                stream.Seek(0, SeekOrigin.Begin);
                stream.Read(result, 0, result.Length);

                CollectionAssert.AreEqual(expected, result);
            }
        }

        [Test]
        public async Task given_document_when_writing_and_disposed_called_then_writing_stopped()
        {
            var hotSource = Observable
                .Return(1)
                .Delay(TimeSpan.FromMilliseconds(500))
                .Publish();

            var awaitable = hotSource
                .Do((Console.WriteLine))
                .Finally(() => Console.WriteLine("Complete"))
                .LastOrDefaultAsync()
                .ToTask();

            var disposable = hotSource.Connect();

            disposable.Dispose();

            await awaitable;
        }
    }
}
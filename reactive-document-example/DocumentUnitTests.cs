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
        private const int NumberCount = 40;

        private IObservable<byte> _source;

        [SetUp]
        public void Setup()
        {
            _source = Observable.Range(0, NumberCount).Select(x => (byte) x);
        }

        [Test]
        public async Task given_document_when_disposed_and_write_called_then_throws_exception()
        {
            var document = new Document(new MemoryStream());

            document.Dispose();

            Assert.ThrowsAsync<ObjectDisposedException>(async () => { await document.Write(_source); });
        }

        [Test]
        public async Task given_document_when_disposed_and_disposed_called_then_returns_quitely()
        {
            var document = new Document(new MemoryStream());

            document.Dispose();
            document.Dispose();
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
        public async Task given_document_when_writing_and_disposed_called_then_writing_stopped()
        {
            var notexpected = (await _source.ToArray()).Length;

            var hotSource = _source.Delay(TimeSpan.FromMilliseconds(100)).Publish();

            hotSource.Connect();

            using (var stream = new MemoryStream())
            {

                using (var document = new Document(stream, true))
                {
                    document.Write(hotSource).Subscribe();
                }

                Assert.That(stream.Length, Is.Not.EqualTo(notexpected));
            }
        }

        [Test]
        public async Task given_document_when_writing_and_disposed_called_after_writing_complete_then_does_not_throw_exception()
        {
            using (var stream = new MemoryStream())
            {
                Assert.DoesNotThrowAsync(async () =>
                {
                    using (var document = new Document(stream, true))
                    {
                        await document.Write(_source).LastOrDefaultAsync();
                    }
                });
            }
        }

        [Test]
        public async Task given_document_and_document_is_empty_and_when_read_called_then_returns_empty_observable()
        {
            var document = new Document(new MemoryStream());

            var result = await document.Read().ToArray();

            Assert.That(result.Length, Is.EqualTo(0));
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
        public async Task given_document_when_write_data_then_observable_written_contains_data()
        {
            var expected = await _source.ToArray();
            using (var stream = new MemoryStream())
            {
                using (var document = new Document(stream, true))
                {
                    var result = await document.Write(_source).ToArray();

                    CollectionAssert.AreEqual(expected, result);
                }
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
            var hotSource = _source.Delay(TimeSpan.FromMilliseconds(100)).Publish();

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
        public async Task given_document_when_write_data_and_await_write_and_read_data_then_reader_observable_returns_data()
        {
            var expected = await _source.ToArray();
            using (var stream = new MemoryStream())
            {
                using (var document = new Document(stream, true))
                {
                    await document.Write(_source);

                    var result = await document.Read().ToArray();

                    CollectionAssert.AreEqual(expected, result);
                }
            }
        }

        [Test]
        public async Task given_document_when_write_slow_hot_data_and_read_data_then_reader_observable_returns_data()
        {
            var expected = await _source.ToArray();

            using (var stream = new MemoryStream())
            {
                using (var document = new Document(stream, true))
                {
                    int writtenCount = 0;

                    document.Write(_source
                        .Select(x =>
                            Observable.Empty<byte>()
                                .Delay(TimeSpan.FromMilliseconds(10))
                                .StartWith(x)
                        ).Concat())
                        .Subscribe((_) => writtenCount++);

                    await Task.Delay(100);

                    Assert.That(writtenCount, Is.Not.Zero);
                    Assert.That(writtenCount, Is.Not.EqualTo(expected.Length));

                    var result = await document.Read().ToArray();

                    CollectionAssert.AreEqual(expected, result);
                }
            }
        }
    }
}
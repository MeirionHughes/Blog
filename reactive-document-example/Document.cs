using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace reactive_document_example
{
    public class Document : IDocument
    {
        private readonly EventLoopScheduler _documentScheduler = new EventLoopScheduler();

        private readonly Stream _stream;
        private readonly object _streamLock;
        private readonly object _documentLock;

        private readonly bool _streamKeepAlive;

        private IDisposable _disposable;
        private IObservable<byte> _writer;

        private int _writerIndex;
        private bool _disposed;


        public Document(Stream memoryStream, bool keepAlive = false)
        {
            _stream = memoryStream;
            _streamLock = new object();
            _documentLock = new object();
            _streamKeepAlive = keepAlive;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposable?.Dispose();
            _disposable = null;
            _disposed = true;

            if (!_streamKeepAlive)
                _stream.Dispose();
        }

        /// <summary>
        /// Write an observable sequence of bytes to the document
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if
        /// document disposed
        /// or when document is disposed before writing completes</exception>
        /// <returns>Observable of written bytes that also produces an error if the document is disposed before writing completes</returns>
        public Task Write(IObservable<byte> source)
        {
            lock (_documentLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(Document));

                if (_writer != null)
                    throw new InvalidOperationException("already writing to document");

                _writerIndex = (int) _stream.Length;

                var sourceWriter = source
                    .ObserveOn(_documentScheduler)
                    .Do(next =>
                    {
                        lock (_streamLock)
                        {
                            _stream.Seek(_writerIndex, SeekOrigin.Begin);
                            _stream.WriteByte(next);
                            _writerIndex += 1;
                        }
                    })
                    .Do(x => Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId:D2}] Written {x}"))
                    .Finally(() =>
                    {
                        lock (_documentLock)
                        {
                            _writer = null;
                        }
                    })
                    .Publish();

                _writer = sourceWriter;

                var subject = new Subject<byte>();
                var cts = new CancellationTokenSource();
                var awaitable = subject.LastOrDefaultAsync().ToTask(cts.Token);

                _disposable = new CompositeDisposable()
                {
                    Disposable.Create(() => cts.Cancel()),
                    _writer.Subscribe(subject),
                    sourceWriter.Connect(),
                };

                return awaitable;
            }
        }

        public IObservable<byte> Read()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Document));

            var streamReader =
                Observable.Defer(() =>
                {
                    int length = 0;

                    lock (_streamLock)
                    {
                        length = (int) _stream.Length;
                    }

                    Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId:D2}] Length at subscription is: {length}");

                    return Observable.Create<byte>(
                        observer =>
                        {
                            var cts = new CancellationTokenSource();
                            var disposable = ThreadPoolScheduler.Instance.Schedule(
                                () =>
                                {
                                    Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId:D2}] Stream Reader Started");

                                    try
                                    {
                                        var position = 0;
                                        var buffer = new byte[4096];

                                        do
                                        {
                                            cts.Token.ThrowIfCancellationRequested();

                                            var count = Math.Min(4096, length - position);

                                            lock (_streamLock)
                                            {
                                                _stream.Seek(position, SeekOrigin.Begin);
                                                _stream.Read(buffer, 0, count);
                                            }

                                            for (int i = 0; i < count; i++)
                                            {
                                                Task.Delay(50).Wait(); // Add some delay so we can see it reading concurrently with source
                                                observer.OnNext(buffer[i]);
                                            }

                                            position += count;
                                        } while (position < length);

                                        observer.OnCompleted();
                                    }
                                    catch (Exception error)
                                    {
                                        observer.OnError(error);
                                    }
                                    Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId:D2}] Stream Reader Stopped");
                                });

                            return new CompositeDisposable()
                            {
                                Disposable.Create(() => cts.Dispose()),
                                disposable
                            };
                        });
                });

            return Observable.Create((IObserver<byte> observer) =>
            {
                Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId:D2}] Creating Reader Observable");

                if (_writer == null)
                    return streamReader.Do(
                        x => Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId:D2}] Read(stream) {x}"))
                        .Subscribe(observer);

                var bufferedSource = _writer.BufferUntilSubscribed();
                var bufferedSourceDispose = bufferedSource.Connect();

                Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId:D2}] Created Reader Observable");

                var disposable = new CompositeDisposable()
                {
                    Observable.Concat(
                        streamReader.Do(
                            x => Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId:D2}] Read(stream) {x}")),
                        bufferedSource
                        ).Subscribe(observer),
                    bufferedSourceDispose
                };
                return disposable;
            }).SubscribeOn(_documentScheduler); //important
        }
    }
}
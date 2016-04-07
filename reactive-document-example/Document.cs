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
        private static readonly EventLoopScheduler DocumentScheduler = new EventLoopScheduler();

        private readonly Stream _stream;
        private readonly object _streamLock;
        private readonly bool _streamKeepAlive;

        private IDisposable _disposable;
        private IObservable<byte> _source;
        private IObservable<byte> _writer;

        private int _writerIndex;
        private bool _disposed;

        public Document(MemoryStream memoryStream, bool keepAlive = false)
        {
            _stream = memoryStream;
            _streamLock = new object();
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

        public async Task Write(IObservable<byte> source)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Document));
            if (_writer != null)
                throw new InvalidOperationException("already writing to document");

            _source = source
                .ObserveOn(DocumentScheduler)
                .SubscribeOn(DocumentScheduler)
                .Publish();

            _writerIndex = (int) _stream.Length;
            _writer = _source
                .Do(next =>
                {
                    lock (_streamLock)
                    {
                        _stream.Seek(_writerIndex, SeekOrigin.Begin);
                        _stream.WriteByte(next);
                        _writerIndex += 1;
                    }
                })
                .Finally(() =>
                {
                    _source = null;
                    _writer = null;
                })
                .Publish();


            _disposable = new CompositeDisposable()
            {
                ((IConnectableObservable<byte>) _writer).Connect(),
                ((IConnectableObservable<byte>) _source).Connect()
            };

            await _writer.LastOrDefaultAsync();
        }

        public IObservable<byte> Read()
        {
            var streamReader = Observable
                .Create<byte>(
                    observer =>
                    {
                        var cts = new CancellationTokenSource();
                        var disposable = ThreadPoolScheduler.Instance.Schedule(
                            () =>
                            {
                                var position = 0;
                                var length = _stream.Length;
                                var buffer = new byte[4096];

                                do
                                {
                                    cts.Token.ThrowIfCancellationRequested();

                                    var count = Math.Min(4096, (int) (length - position));

                                    lock (_streamLock)
                                    {
                                        _stream.Seek(position, SeekOrigin.Begin);
                                        _stream.Read(buffer, 0, count);
                                    }

                                    for (int i = 0; i < count; i++)
                                        observer.OnNext(buffer[i]);

                                    position += count;
                                } while (position < length);

                                observer.OnCompleted();
                            });

                        return new CompositeDisposable()
                        {
                            Disposable.Create(() => cts.Dispose()),
                            disposable
                        };
                    });

            return Observable.Create((IObserver<byte> observer) =>
            {
                if (_source == null)
                    return streamReader.Subscribe(observer);

                var bufferedSource = _source.BufferUntilSubscribed();
                var bufferedSourceDispose = bufferedSource.Connect();

                var disposable = new CompositeDisposable()
                {
                    streamReader
                        .Concat(bufferedSource)
                        .Subscribe(observer),
                    bufferedSourceDispose
                };
                return disposable;
            }).SubscribeOn(DocumentScheduler);
        }
    }
}
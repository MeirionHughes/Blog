using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;


public class Document : IDocument
{
    private readonly EventLoopScheduler _scheduler;
    private Stream _stream;
    private bool _streamKeepOpen;
    private bool _disposed;
    private object _streamLock;
    private IObservable<byte> _writer;


    public Document(Stream stream, bool streamKeepOpen = false)
    {
        _stream = stream;
        _streamLock = new object();
        _streamKeepOpen = streamKeepOpen;
        _scheduler = new EventLoopScheduler();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (!_streamKeepOpen)
            _stream.Dispose();

        _disposed = true;
    }

    public IObservable<byte> Write(IObservable<byte> source)
    {
        var writerPosition = 0;
        var writerConnectable = source
            .ObserveOn(_scheduler)
            .Do((next) =>
            {
                lock (_streamLock)
                {
                    _stream.Seek(writerPosition++, SeekOrigin.Begin);
                    _stream.WriteByte(next);
                }
            })
            .Publish();

        _writer = writerConnectable
            .ObserveOn(_scheduler)
            .SubscribeOn(_scheduler);

        writerConnectable.Connect();

        return _writer;
    }

    public IObservable<byte> Read()
    {
        var length = (int) _stream.Position;

        var readerStream = Observable
                .Create((IObserver<byte> observer) =>
                {
                    var buffer = new byte[1024];
                    var position = 0;
                    do
                    {
                        var count = Math.Min(buffer.Length, length - position);
                        lock (_streamLock)
                        {
                            _stream.Seek(position, SeekOrigin.Begin);
                            _stream.Read(buffer, 0, count);
                            foreach (var item in buffer)
                            {
                                observer.OnNext(item);
                            }
                        }
                    }while (position < length);

                    observer.OnCompleted();

                    return () => { };
                });



        var writerConnectable = source
            .ObserveOn(_scheduler)
            .Do((next) =>
            {
                lock (_streamLock)
                {
                    _stream.Seek(writerPosition++, SeekOrigin.Begin);
                    _stream.WriteByte(next);
                }
            })
            .Publish();
    }
}
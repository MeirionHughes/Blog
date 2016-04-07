﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace System.Reactive.Linq
{
    /// <summary>
    /// source: http://stackoverflow.com/a/24805489/1657476
    /// </summary>
    public static class BufferUntilSubscribedExtentions
    {
        /// <summary>
        /// Returns a connectable observable, that once connected, will start buffering data until the observer subscribes, at which time it will send all buffered data to the observer and then start sending new data.
        /// Thus the observer may subscribe late to a hot observable yet still see all of the data.  Later observers will not see the buffered events.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="scheduler">Scheduler to use to dump the buffered data to the observer.</param>
        /// <returns></returns>
        public static IConnectableObservable<T> BufferUntilSubscribed<T>(this IObservable<T> source, IScheduler scheduler)
        {
            return new BufferUntilSubscribedObservable<T>(source, scheduler);
        }

        /// <summary>
        /// Returns a connectable observable, that once connected, will start buffering data until the observer subscribes, at which time it will send all buffered data to the observer and then start sending new data.
        /// Thus the observer may subscribe late to a hot observable yet still see all of the data.  Later observers will not see the buffered events.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IConnectableObservable<T> BufferUntilSubscribed<T>(this IObservable<T> source)
        {
            return new BufferUntilSubscribedObservable<T>(source, Scheduler.Immediate);
        }

        private class BufferUntilSubscribedObservable<T> : IConnectableObservable<T>
        {
            private readonly IObservable<T> _source;
            private readonly IScheduler _scheduler;
            private readonly Subject<T> _liveEvents;
            private bool _observationsStarted;
            private Queue<T> _buffer;
            private readonly object _gate;

            public BufferUntilSubscribedObservable(IObservable<T> source, IScheduler scheduler)
            {
                _source = source;
                _scheduler = scheduler;
                _liveEvents = new Subject<T>();
                _buffer = new Queue<T>();
                _gate = new object();
                _observationsStarted = false;
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                lock (_gate)
                {
                    if (_observationsStarted)
                    {
                        return _liveEvents.Subscribe(observer);
                    }

                    _observationsStarted = true;

                    var bufferedEvents = GetBuffers().Concat().Finally(RemoveBuffer);
                    // Finally clause to remove the buffer if the first observer stops listening.
                    return _liveEvents.Merge(bufferedEvents).Subscribe(observer);
                }
            }

            public IDisposable Connect()
            {
                return _source.Subscribe(OnNext, _liveEvents.OnError, _liveEvents.OnCompleted);
            }

            private void RemoveBuffer()
            {
                lock (_gate)
                {
                    _buffer = null;
                }
            }

            /// <summary>
            /// Acquires a lock and checks the buffer.  If it is empty, then replaces it with null and returns null.  Else replaces it with an empty buffer and returns the old buffer.
            /// </summary>
            /// <returns></returns>
            private Queue<T> GetAndReplaceBuffer()
            {
                lock (_gate)
                {
                    if (_buffer == null)
                    {
                        return null;
                    }

                    if (_buffer.Count == 0)
                    {
                        _buffer = null;
                        return null;
                    }

                    var result = _buffer;
                    _buffer = new Queue<T>();
                    return result;
                }
            }

            /// <summary>
            /// An enumerable of buffers that will complete when a call to GetAndReplaceBuffer() returns a null, e.g. when the observer has caught up with the incoming source data.
            /// </summary>
            /// <returns></returns>
            private IEnumerable<IObservable<T>> GetBuffers()
            {
                Queue<T> buffer;
                while ((buffer = GetAndReplaceBuffer()) != null)
                {
                    yield return buffer.ToObservable(_scheduler);
                }
            }

            private void OnNext(T item)
            {
                lock (_gate)
                {
                    if (_buffer != null)
                    {
                        _buffer.Enqueue(item);
                        return;
                    }
                }

                _liveEvents.OnNext(item);
            }
        }
    }
}


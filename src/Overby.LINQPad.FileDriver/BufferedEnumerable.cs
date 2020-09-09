using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Overby.LINQPad.FileDriver
{
    public class BufferedEnumerable<T> : IEnumerable<T>, IDisposable
    {
        readonly IEnumerator<T> _it;
        readonly ICollection<T> _buffer;
        readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public BufferedEnumerable(IEnumerable<T> enumerable)
        {
            if (enumerable is ICollection<T> collection)
            {
                _buffer = collection;
            }
            else
            {
                _it = enumerable?.GetEnumerator();
                _buffer = new List<T>();
            }
        }

        public BufferedEnumerable(IEnumerator<T> enumerator)
        {
            _it = enumerator;
            _buffer = new List<T>();
        }

        public IEnumerator<T> GetEnumerator()
        {
            _lock.EnterReadLock();

            try
            {
                foreach (var item in _buffer)
                    yield return item;
            }
            finally
            {
                _lock.ExitReadLock();
            }

            _lock.EnterWriteLock();
            try
            {
                while (_it?.MoveNext() == true)
                {
                    _buffer.Add(_it.Current);
                    yield return _it.Current;
                }

                if (_buffer is List<T> list)
                    list.TrimExcess();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Dispose() => _it?.Dispose();
    }
}

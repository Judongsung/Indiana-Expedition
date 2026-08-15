using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Persistence;

namespace IndianaExpedition.Core.Tests
{
    internal static class TestAssert
    {
        internal static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    message + " Expected: " + expected + ", Actual: " + actual);
            }
        }

        internal static void True(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        internal static void Throws<TException>(Action action, string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException(message);
        }

        internal static void WithTemporaryDirectory(Action<string> test)
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "indiana-expedition-tests-" + Guid.NewGuid().ToString(StorageConstants.CompactIdentifierFormat));
            Directory.CreateDirectory(root);
            try
            {
                test(root);
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }
    }

    internal sealed class MemoryDocumentStore<T> : IDocumentStore<T> where T : class
    {
        private readonly Func<T, T> _clone;
        private T _value;

        internal MemoryDocumentStore(T value, Func<T, T> clone)
        {
            _clone = clone;
            _value = clone(value);
        }

        internal bool FailWrites { get; set; }
        internal int SaveCount { get; private set; }
        internal T LastSaved => _clone(_value);

        public T Load()
        {
            return _clone(_value);
        }

        public void Save(T value)
        {
            SaveCount++;
            if (FailWrites)
            {
                throw new IOException("Injected storage failure.");
            }
            _value = _clone(value);
        }
    }

    internal sealed class BlockingDocumentStore<T> : IDocumentStore<T>, IDisposable
        where T : class
    {
        private readonly Func<T, T> _clone;
        private readonly ManualResetEventSlim _saveStarted = new ManualResetEventSlim();
        private readonly ManualResetEventSlim _releaseSave = new ManualResetEventSlim();
        private T _value;

        internal BlockingDocumentStore(T value, Func<T, T> clone)
        {
            _clone = clone ?? throw new ArgumentNullException(nameof(clone));
            _value = _clone(value);
        }

        public T Load() => _clone(_value);

        public void Save(T value)
        {
            _saveStarted.Set();
            _releaseSave.Wait();
            _value = _clone(value);
        }

        internal bool WaitUntilSaveStarts(TimeSpan timeout) => _saveStarted.Wait(timeout);

        internal void ReleaseSave() => _releaseSave.Set();

        public void Dispose()
        {
            _releaseSave.Set();
            _saveStarted.Dispose();
            _releaseSave.Dispose();
        }
    }
}

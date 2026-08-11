using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using IndianaExpedition.Core.Constants;

namespace IndianaExpedition.Core.Persistence
{
    public sealed class AtomicJsonFileStore<T> where T : class
    {
        private readonly object _gate = new object();
        private readonly string _path;
        private readonly Func<T> _factory;
        private readonly DataContractJsonSerializer _serializer;

        public AtomicJsonFileStore(string path, Func<T> factory)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(CoreMessages.StoragePathRequired, nameof(path));
            }

            _path = Path.GetFullPath(path);
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _serializer = new DataContractJsonSerializer(
                typeof(T),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
        }

        public T Load()
        {
            lock (_gate)
            {
                if (!File.Exists(_path))
                {
                    return _factory();
                }

                try
                {
                    using (var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        return (T)_serializer.ReadObject(stream);
                    }
                }
                catch (Exception ex) when (
                    ex is SerializationException ||
                    ex is IOException ||
                    ex is UnauthorizedAccessException ||
                    ex is InvalidCastException)
                {
                    BackupCorruptFile();
                    return _factory();
                }
            }
        }

        public void Save(T value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            lock (_gate)
            {
                var directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var temporaryPath = _path + StorageConstants.TemporaryFileSuffix;
                var backupPath = _path + StorageConstants.BackupFileSuffix;

                try
                {
                    using (var stream = new FileStream(
                        temporaryPath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None))
                    {
                        _serializer.WriteObject(stream, value);
                        stream.Flush(true);
                    }

                    if (File.Exists(_path))
                    {
                        File.Replace(temporaryPath, _path, backupPath, true);
                    }
                    else
                    {
                        File.Move(temporaryPath, _path);
                    }
                }
                finally
                {
                    if (File.Exists(temporaryPath))
                    {
                        File.Delete(temporaryPath);
                    }
                }
            }
        }

        private void BackupCorruptFile()
        {
            try
            {
                var stamp = DateTime.UtcNow.ToString(StorageConstants.BackupTimestampFormat);
                File.Copy(
                    _path,
                    _path + StorageConstants.CorruptFileMarker + stamp + StorageConstants.BackupFileSuffix,
                    true);
            }
            catch
            {
                // Recovery must still return defaults when a backup cannot be written.
            }
        }
    }
}

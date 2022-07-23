using AsynchronousArchiver.Model.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsynchronousArchiver.Model
{
    class Archiving : IСompressDecompress
    {
        public const int MAX_TASKS = 5;
        public const int MAX_PART_FILE_LENGHT = 1000000;

        private string[] _filePath;
        private byte _progress;
        private StatusOperation _status;
        private TimeSpan _timeProcess;

        private ConcurrentDictionary<int, int> _sizeArchivedParts = new ConcurrentDictionary<int, int>();
        private MultiFileStream _multiFileStream;
        private FileStream _targetFileStream;
        private object _locker = new object();
        private object _lockerWriteFile = new object();

        public delegate void HandlerMess(string message);
        public event HandlerMess EventEndArchiving;

        /// <summary>
        /// Количество частей на которые разбивается файлы
        /// </summary>
        private int _countPatrs;

        /// <summary>
        /// Номер последней считаной на текущий момент части
        /// </summary>
        private int? _numberPart;

        /// <summary>
        /// Таски на сжатие кусков байтов
        /// </summary>
        private List<Task> _archivedPartsTasks = new List<Task>();

        /// <summary>
        /// Сжатые части файлов
        /// </summary>
        private ConcurrentDictionary<long, byte[]> _archivedParts = new ConcurrentDictionary<long, byte[]>();

        private ConcurrentDictionary<string, StatusOperation> _statusThread = new ConcurrentDictionary<string, StatusOperation>();

        private ConcurrentDictionary<string, long> _positionFileStream = new ConcurrentDictionary<string, long>();

        public string[] FilePath
        {
            get { return _filePath; }
            private set
            {
                _filePath = value;
            }
        }

        /// <summary>
        /// Имя сжатого файла
        /// </summary>
        public string NameArchivedFile => "ArchivedFile.gz";

        public StatusOperation Status
        {
            get { return _status; }
            private set
            {
                _status = value;
            }
        }

        public byte Progress
        {
            get { return _progress; }
            private set
            {
                _progress = value;
            }
        }

        public TimeSpan TimeProcess
        {
            get { return Status != StatusOperation.Done ? DateTime.Now.TimeOfDay - _timeProcess : _timeProcess; }
            private set
            {
                _timeProcess = value - _timeProcess;
            }
        }

        public Archiving()
        {
            FilePath = AppState.Instance.FilePath;
            Status = StatusOperation.InProgress;
            _timeProcess = DateTime.Now.TimeOfDay;

            _multiFileStream = new MultiFileStream(FilePath);
            _targetFileStream = new FileStream(AppState.Instance.Directory + NameArchivedFile, FileMode.Create, FileAccess.Write);
            _countPatrs = GetCountParts(_multiFileStream);
            EventEndArchiving += HandlerEndArchiving;
            AppState.Instance.CompressDecompress = this;

            //Костыль
            SetAdditionalInformationToFile();
        }

        public void ThreadArchiving()
        {
            try
            {
                // Создание заданий на архивацию частей файла
                for (int i = 0; i < MAX_TASKS; i++)
                    _archivedPartsTasks.Add(new Task(() => ArchivingPart()));

                //Таска для записи сжатых кусков в файл
                Task.Factory.StartNew(() => WriteFileArchivedParts());

                //Таска для записи дополнительной информации о архивации в файл
                Task.Factory.StartNew(() => WriteAdditionalInformationToFile());

                //
                while (Status != StatusOperation.Done)
                {                   
                    if (!IsEndFile() && CheckTask().Count() != 0)
                        CheckTask()?.First()?.Start();

                    if (IsEndArchiving())
                        EventEndArchiving?.Invoke("");
                }
            }
            catch (Exception ex)
            {

            }          
        }

        public IEnumerable<Task> CheckTask()
        {
            //Удаление уже отработавших тасков
            List<Task> taskForRemove = new List<Task>();
            foreach (var task in _archivedPartsTasks.Where(x => x.Status != TaskStatus.Created))
                taskForRemove.Add(task);
            foreach (var task in taskForRemove)
                _archivedPartsTasks.Remove(task);

            if (_archivedPartsTasks.Count < MAX_TASKS)
                _archivedPartsTasks.Add(new Task(() => ArchivingPart()));

            return _archivedPartsTasks.Where(x => x.Status == TaskStatus.Created);
        }

        public void ArchivingPart()
        {
            byte[] compressByteArr;
            MemoryStream streamPart;
            GZipStream compression_stream;

            try
            {
                var partFile = GetPartFile();
                if (partFile == null) return;

                streamPart = new MemoryStream();
                compression_stream = new GZipStream(streamPart, CompressionMode.Compress);
                compression_stream.Write(partFile.Part, 0, partFile.CountRead);
                compression_stream.Close();
                compressByteArr = streamPart.ToArray();
                streamPart = null;
                _sizeArchivedParts.TryAdd(partFile.IdPart, compressByteArr.Length);
                _archivedParts.TryAdd(partFile.IdPart, compressByteArr);
            }
            catch (Exception ex)
            {

            }
        }

        public PartFile GetPartFile()
        {
            byte[] partByteArr = new byte[MAX_PART_FILE_LENGHT];
            lock (_locker)
            {
                try
                {
                    if (!IsEndFile())
                    {
                        var read = _multiFileStream.Read(partByteArr, 0, MAX_PART_FILE_LENGHT);
                        _numberPart = _numberPart == null ? 0 : _numberPart + 1;
                        var numberPart = (int)_numberPart;
                        return new PartFile(numberPart, partByteArr, read);
                    }
                }
                catch (Exception ex)
                {

                }

                return null;
            }
        }

        /// <summary>
        /// Запись сжатых кусков в файл
        /// </summary>
        public void WriteFileArchivedParts()
        {
            try
            {
                _positionFileStream.TryAdd("ArchivingParts", 4 * 1000000 + 4 * 10000 + 4 + 4 + 200000);
                _statusThread.TryAdd("WriteFileArchivedParts", StatusOperation.InProgress);
                byte[] byteArr;
                int count = 0;
                while (Status != StatusOperation.Done)
                {
                    if (!_archivedParts.ContainsKey(count) && count == _countPatrs)
                        _statusThread["WriteFileArchivedParts"] = StatusOperation.Done;

                    if (_archivedParts.ContainsKey(count) == true && 
                        _statusThread["WriteFileArchivedParts"] != StatusOperation.Done)
                    {
                        lock (_lockerWriteFile)
                        {
                            _archivedParts.TryRemove(count, out byteArr);
                            _targetFileStream.Position = _positionFileStream["ArchivingParts"];
                            _targetFileStream.Write(byteArr, 0, byteArr.Length);
                            _positionFileStream["ArchivingParts"] = _targetFileStream.Position;
                            count++;
                            Progress = (byte)(100 * count / _countPatrs);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }           
        }

        /// <summary>
        /// Запись в файл дополнительной информации
        /// </summary>
        public void WriteAdditionalInformationToFile()
        {
            try
            {
                _statusThread.TryAdd("WriteAdditionalInformationToFile", StatusOperation.InProgress);
                int count = 0;
                while (Status != StatusOperation.Done)
                {
                    if (!_sizeArchivedParts.ContainsKey(count) && count == _countPatrs)
                        _statusThread["WriteAdditionalInformationToFile"] = StatusOperation.Done;

                    if (_sizeArchivedParts.ContainsKey(count) == true &&
                        _statusThread["WriteAdditionalInformationToFile"] != StatusOperation.Done)
                    {
                        lock (_lockerWriteFile)
                        {
                            //Количество байтов в пакетах
                            _sizeArchivedParts.TryRemove(count, out var sizePart);
                            _targetFileStream.Position = _positionFileStream["Information"];
                            _targetFileStream.Write(BitConverter.GetBytes(sizePart), 0, 4);
                            _positionFileStream["Information"] = _targetFileStream.Position;
                            count++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        public int GetCountParts(MultiFileStream multiFileStream)
        {
            int countParts = 0;
            for (int i = 0; i < multiFileStream.NumberStream; i++)
            {
                countParts += (int)(multiFileStream.LengthStreams[i] / MAX_PART_FILE_LENGHT);
                countParts = ((int)(multiFileStream.LengthStreams[i] % MAX_PART_FILE_LENGHT)) > 0 ? (countParts + 1) : countParts;
            }

            return countParts;
        }

        /// <summary>
        /// Запись дополнительной информации в начало файла на сжатие
        /// </summary>
        public void SetAdditionalInformationToFile()
        {
            _targetFileStream.Position = 0;

            //Передаем количество файлов
            _targetFileStream.Write(BitConverter.GetBytes(_multiFileStream.NumberStream), 0, 4);

            //Передаем количество пакетов в файлах
            long sumLength = 0;
            for (int i = 0; i < _multiFileStream.NumberStream; i++)
            {
                int countParts = 0;
                sumLength = sumLength + _multiFileStream.LengthStreams[i];
                countParts = (int)(sumLength / MAX_PART_FILE_LENGHT);
                countParts = (int)(sumLength % MAX_PART_FILE_LENGHT) > 0 ? countParts + 1 : countParts;

                if (i == _multiFileStream.NumberStream - 1)
                    countParts++;

                _targetFileStream.Write(BitConverter.GetBytes(countParts), 0, 4);
            }            

            /////Имена файлов\\\\\
            //////////////////////
            for (int i = 0; i < _multiFileStream.NumberStream; i++)
            {
                byte[] Name_Bytes = Encoding.UTF8.GetBytes(Path.GetFileName(FilePath[i]));
                byte[] Name_Bytes_Size = new byte[1];
                Name_Bytes_Size[0] = (byte)(Name_Bytes.Length);

                //Передача формата
                _targetFileStream.Write(Name_Bytes_Size, 0, 1);
                _targetFileStream.Write(Name_Bytes, 0, Name_Bytes.Length);
            }

            //Общее количество пакетов
            _targetFileStream.Write(BitConverter.GetBytes(_countPatrs), 0, 4);

            _positionFileStream.TryAdd("Information", _targetFileStream.Position);
        }

        public void HandlerEndArchiving(string empty)
        {
            Status = StatusOperation.Done;
            TimeProcess = DateTime.Now.TimeOfDay;
            Dispose();
        }

        /// <summary>
        /// Показывает считаны ли файл/файлы
        /// </summary>
        /// <returns></returns>
        public bool IsEndFile()
        {
            return _multiFileStream.Position == _multiFileStream.Length;
        }

        /// <summary>
        /// Показывает закончилось ли архивирование
        /// </summary>
        /// <returns></returns>
        public bool IsEndArchiving()
        {
            return _multiFileStream.Position == _multiFileStream.Length && _archivedPartsTasks.Where(x => x.Status == TaskStatus.Running).Count() == 0 &&
            _statusThread.Where(x => x.Value == StatusOperation.InProgress).Count() == 0;
        }

        public void Dispose()
        {
            _targetFileStream.Close();
            _multiFileStream.Close();
        }
    }
}

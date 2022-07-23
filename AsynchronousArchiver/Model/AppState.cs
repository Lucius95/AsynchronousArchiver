using AsynchronousArchiver.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsynchronousArchiver.Model
{
    class AppState : IAppState
    {
        private static AppState _state;

        private IСompressDecompress _сompressDecompress;
        private string[] _filePath;

        public IСompressDecompress CompressDecompress
        {
            get { return _сompressDecompress; }
            set
            {
                _сompressDecompress = value;
            }
        }

        public string NameAction => CompressDecompress == null ? "<No Action>" 
            : CompressDecompress is Archiving ? "Compress" : "Decompress";

        public string[] FilePath
        {
            get { return _filePath; }
            set
            {
                _filePath = value;
            }
        }

        public StatusOperation Status => CompressDecompress == null ? StatusOperation.Unknown : CompressDecompress.Status;

        public byte Progress => CompressDecompress == null ? (byte)0 : CompressDecompress.Progress;

        public TimeSpan TimeProcess => CompressDecompress == null ? new TimeSpan(0, 0, 0) : CompressDecompress.TimeProcess;

        /// <summary>
        /// Директория в которой будет происходить операция архивации/разархивации
        /// </summary>
        public string Directory => FilePath == null ? "<Directory not selected>" : Path.GetDirectoryName(FilePath[0]) + @"\";

        public static AppState Instance => _state;

        public AppState()
        {

        }

        public static IAppState InitAppState()
        {
            _state ??= new AppState();
            return _state;
        }

        public void Dispose()
        {

        }
    }
}

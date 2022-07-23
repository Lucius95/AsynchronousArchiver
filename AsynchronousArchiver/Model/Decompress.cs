using AsynchronousArchiver.Model.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsynchronousArchiver.Model
{
    class Decompress : IСompressDecompress
    {
        private string _filePath;
        private byte _progress;
        private StatusOperation _status;
        private TimeSpan _timeProcess;

        public string FilePath
        {
            get { return _filePath; }
            private set
            {
                _filePath = value;
            }
        }

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

        public Decompress()
        {
            FilePath = AppState.Instance.FilePath.First();
            Status = StatusOperation.InProgress;
            _timeProcess = DateTime.Now.TimeOfDay;
            AppState.Instance.CompressDecompress = this;
        }

        public void ThreadDecompress()
        {
            var CountFile = 0;
            var NumberFile = 0;
            int[] Number_Packages = new int[10000];
            int[] SizeFile = new int[1000000];


            byte[] ByteArr = new byte[1000000];
            byte[] ByteArr2 = new byte[1000000];
            int Read = 0;
            int Read2 = 0;
            MemoryStream Part2;

            try
            {
                using (FileStream source_stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
                {
                    int size = 0;
                    byte[] bytes_size = new byte[4];                //
                    byte[] bytes_format = new byte[10];             //
                    byte[] bytes_names = new byte[100];             //
                    byte[] bytes_NumberFile = new byte[4];          //
                    byte[] bytes_Number_Packages = new byte[7];     //
                    byte[] bytes_CountFile = new byte[4];           //
                    byte[] bytes_SizeFile = new byte[7];            //                   

                    source_stream.Position = 0;

                    //Количество файлов
                    source_stream.Read(bytes_NumberFile, 0, 4);
                    NumberFile = BitConverter.ToInt32(bytes_NumberFile, 0);

                    //Количество пакетов в файлах
                    for (int i = 0; i < NumberFile; i++)
                    {
                        source_stream.Read(bytes_Number_Packages, 0, 4);
                        Number_Packages[i] = BitConverter.ToInt32(bytes_Number_Packages, 0);
                    }

                    /////Имена файлов\\\\\
                    //////////////////////
                    string[] Names = new string[NumberFile];
                    for (int i = 0; i < NumberFile; i++)
                    {
                        //
                        source_stream.Read(bytes_size, 0, 1);
                        size = (int)bytes_size[0];

                        //
                        source_stream.Read(bytes_names, 0, size);
                        Names[i] = Encoding.UTF8.GetString(bytes_names, 0, size);
                    }

                    //Общее количество пакетов
                    source_stream.Read(bytes_CountFile, 0, 4);
                    CountFile = BitConverter.ToInt32(bytes_CountFile, 0);

                    //Количество байтов в пакетах
                    for (int i = 0; i < CountFile; i++)
                    {
                        source_stream.Read(bytes_SizeFile, 0, 4);
                        SizeFile[i] = BitConverter.ToInt32(bytes_SizeFile, 0);
                    }

                    source_stream.Position = 4 * 1000000 + 4 * 10000 + 4 + 4 + 200000;    

                    int loc_count = 0;
                    for (int i = 0; i < NumberFile; i++)
                    {
                        //Поток для записи востановленного файла
                        using (FileStream target_stream = new FileStream(AppState.Instance.Directory + @"\" + Names[i], FileMode.Create, FileAccess.Write))
                        {
                            for (int j = loc_count; j < Number_Packages[i]; j++)
                            {
                                ByteArr = new byte[1200000];
                                ByteArr2 = new byte[1200000];
                                Part2 = new MemoryStream();
                                using (MemoryStream Part = new MemoryStream())
                                {
                                    Read = source_stream.Read(ByteArr, 0, SizeFile[loc_count]);
                                    Part.Write(ByteArr, 0, Read);
                                    Part.Position = 0;

                                    using (GZipStream decompression_stream = new GZipStream(Part, CompressionMode.Decompress))
                                    {
                                        decompression_stream.CopyTo(Part2);
                                        Part2.Position = 0;
                                        Read2 = Part2.Read(ByteArr2, 0, (int)Part2.Length);
                                        target_stream.Write(ByteArr2, 0, Read2);
                                        Progress = (byte)(100 * loc_count / (CountFile - 1));
                                    }
                                }
                                loc_count++;
                            }
                        }
                    }
                }
                Status = StatusOperation.Done;
                TimeProcess = DateTime.Now.TimeOfDay;
            }
            catch (Exception ex)
            {

            }
        }

        public void Dispose()
        {

        }
    }
}

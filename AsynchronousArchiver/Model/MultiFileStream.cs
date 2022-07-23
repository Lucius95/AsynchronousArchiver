using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsynchronousArchiver.Model
{
    class MultiFileStream
    {
        private FileStream[] source_streamArr;
        private long[] range_stream;
        public long Length = 0;
        public long[] LengthStreams;
        public long Position = 0;
        public int NumberStream = 0;

        public MultiFileStream(string[] path)
        {
            try
            {
                source_streamArr = new FileStream[path.Length];
                range_stream = new long[path.Length];
                LengthStreams = new long[path.Length];
                NumberStream = path.Length;
                for (int i = 0; i < path.Length; i++)
                {
                    source_streamArr[i] = new FileStream(path[i], FileMode.Open, FileAccess.Read);
                    LengthStreams[i] = source_streamArr[i].Length;
                    Length += source_streamArr[i].Length;
                    range_stream[i] = Length;
                }
            }
            catch (Exception ex)
            {

            }
        }

        public int Read(byte[] arr, int offset, int count)
        {
            int ReadCount = 0;
            int i = 0;

            i = Numberstream(Position);
            ReadCount = source_streamArr[i].Read(arr, offset, count);
            Position += ReadCount;

            return ReadCount;
        }

        public int Numberstream(long Position)
        {
            for (int i = 0; i < source_streamArr.Length; i++)
            {
                if (Position < range_stream[i])
                {
                    return i;
                }
            }

            return 0;
        }

        public void Close()
        {
            for (int i = 0; i < source_streamArr.Length; i++)
            {
                source_streamArr[i].Close();
            }
        }
    }
}

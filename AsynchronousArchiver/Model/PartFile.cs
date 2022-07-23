using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsynchronousArchiver.Model
{
    class PartFile
    {
        private int _idPart;
        private byte[] _part;
        private int _countRead;

        public int IdPart
        {
            get { return _idPart; }
            private set
            {
                _idPart = value;
            }
        }

        public byte[] Part
        {
            get { return _part; }
            private set
            {
                _part = value;
            }
        }

        public int CountRead
        {
            get { return _countRead; }
            private set
            {
                _countRead = value;
            }
        }

        public PartFile(int idPart, byte[] part, int countRead)
        {
            IdPart = idPart;
            Part = part;
            CountRead = countRead;
        }
    }
}

using BitMiracle.LibTiff.Classic;
using System;
using System.IO;
using System.Threading;

namespace Image.Tiff
{
    public class TiffMemoryDestination : TiffStream
    {

        private Int64 _size = 0;
        private Int64 _offset = 0;
        public byte[] _data;

        public TiffMemoryDestination(int size)
        {
            _data = new byte[size];
        }

        public override int Read(object clientData, byte[] buffer, int offset, int count)
        {
            if (_size == 0)
            {
                return 0;
            }
            Buffer.BlockCopy(_data, (int)_offset, buffer, 0, count);
            return count;
        }

        public override void Write(object clientData, byte[] buffer, int offset, int count)
        {
            Buffer.BlockCopy(buffer, offset, _data, (int)_offset, count);
            _offset = Interlocked.Add(ref _offset, count);
            _size = Interlocked.Add(ref _size, count);
        }

        public override long Size(object clientData)
        {
            return _size;
        }

        public override long Seek(object clientData, long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
            {
                Interlocked.Exchange(ref _offset, offset);
            }
            else
            {
                Interlocked.Exchange(ref _offset, _size - offset);
            }
            return _offset;
        }
        public override void Close(object clientData)
        {
            
        }
    }
}

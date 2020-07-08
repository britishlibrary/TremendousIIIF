using kdu_mni;
using System.Buffers;
using System.IO;
using System;

namespace Jpeg2000
{
    public class StreamCompressedSource : Ckdu_compressed_source_nonnative
    {
        private readonly Stream _stream;
        private readonly int _capabilities = Ckdu_global.KDU_SOURCE_CAP_SEQUENTIAL;

        public StreamCompressedSource(Stream stream)
        {
            _stream = stream;
            //if (_stream.CanSeek)
            //    _capabilities |= Ckdu_global.KDU_SOURCE_CAP_SEEKABLE;
        }

        public override int get_capabilities()
        {
            return _capabilities;
        }

        public override int post_read(int num_bytes)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(num_bytes);
            try
            {
                var bytesRead = _stream.Read(buffer, 0, num_bytes);
                push_data(buffer, 0, bytesRead);
                return bytesRead;
            }
            catch (Exception)
            {
                return 0;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            
        }

        public override bool close()
        {
            _stream.Dispose();
            return base.close();
        }
    }
}

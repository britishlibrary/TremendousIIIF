using kdu_mni;
using System.Buffers;
using System.IO;

namespace Jpeg2000
{
    public class MemoryTarget : Ckdu_compressed_target_nonnative
    {
        public Stream Data { get; set; }
        private int Offset { get; set; }

        public MemoryTarget()
        {
        }

        public override bool post_write(int num_bytes)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(num_bytes);
            try
            {
                var count = pull_data(buffer, 0, num_bytes);
                Data.Write(buffer, Offset, count);
                Offset += count;
                return true;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        public override int get_capabilities()
        {
            return Ckdu_global.KDU_TARGET_CAP_SEQUENTIAL;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using kdu_mni;
using System.IO;

namespace Jpeg2000
{
    public class MemoryTarget : Ckdu_compressed_target_nonnative
    {
        public Stream Data { get; set; }
        private int Offset { get; set; }

        public MemoryTarget()
        {
            Data = new MemoryStream();
            Offset = 0;
        }

        public override bool post_write(int num_bytes)
        {
            var buffer = new byte[num_bytes];
            var count = pull_data(buffer, 0, num_bytes);
            Data.Write(buffer, 0, count);
            Offset += count;
            return true;
        }
        public override int get_capabilities()
        {
            return Ckdu_global.KDU_TARGET_CAP_SEQUENTIAL;
        }
    }
}

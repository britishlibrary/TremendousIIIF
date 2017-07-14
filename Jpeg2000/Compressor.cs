using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using kdu_mni;
using System.IO;

namespace Jpeg2000
{
    public class Compressor
    {
        public static Stream Compress(SKImage input)
        {
            using (var tgt = new Cjp2_target())
            using (var fam = new Cjp2_family_tgt())
            using (var memory_target = new MemoryTarget())
            using (var compressor = new Ckdu_stripe_compressor())
            using (var bmp = SKBitmap.FromImage(input))
            {
                fam.open(memory_target);
                tgt.open(fam);


                var codestream = new Ckdu_codestream();

                var siz = new Csiz_params();

                siz.set(Ckdu_global.Ssize, 0, 0, bmp.Width);
                siz.set(Ckdu_global.Ssize, 0, 1, bmp.Height);

                siz.set(Ckdu_global.Sorigin, 0, 0, 0);
                siz.set(Ckdu_global.Sorigin, 0, 1, 0);
                siz.set(Ckdu_global.Scomponents, 0, 0, bmp.BytesPerPixel);
                siz.set(Ckdu_global.Nprecision, 0, 0, bmp.BytesPerPixel);

                siz.set(Ckdu_global.Sdims, 0, 0, input.Width);
                siz.set(Ckdu_global.Sdims, 0, 1, input.Height);


                siz.set(Ckdu_global.Ssigned, 0, 0, false);

                siz.finalize_all();

                var access_dims = tgt.access_dimensions();
                access_dims.init(siz);

                var access_color = tgt.access_colour();
                access_color.init(Ckdu_global.JP2_sRGB_SPACE);

                codestream.create(siz, tgt);

                tgt.write_header();

                tgt.open_codestream();


                compressor.start(codestream);

                int[] stripe_heights = new int[] { bmp.Height, bmp.Height, bmp.Height, bmp.Height };
                //compressor.get_recommended_stripe_heights(16,
                //                              bmp.Height,
                //                              stripe_heights, null);
                int[] offsets = { 0, 0, 0, 0 };
                int[] rowGaps = new int[] { bmp.Width, bmp.Width, bmp.Width, bmp.Width };
                var res = true;
                while (res)
                {
                    res = compressor.push_stripe(bmp.Bytes, stripe_heights);
                }


                compressor.finish();
                codestream.destroy();
                //memory_target.close();
                //codestream.destroy();
                //tgt.close();
                //fam.close();
                var ms = new MemoryStream();
                memory_target.Data.Seek(0, SeekOrigin.Begin);
                memory_target.Data.CopyTo(ms);

                ms.Seek(0, SeekOrigin.Begin);
                return ms;
            }

        }
    }
}

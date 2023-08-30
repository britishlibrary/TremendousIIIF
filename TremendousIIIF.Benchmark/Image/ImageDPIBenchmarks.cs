using BenchmarkDotNet.Attributes;
using Image.Tiff;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using T = BitMiracle.LibTiff.Classic;

namespace TremendousIIIF.Benchmark.Image
{
    [HtmlExporter, CsvExporter, RPlotExporter]
    [MemoryDiagnoser]
    public class ImageDPIBenchmarks
    {
        public static SKData Image { get; set; }

        public ImageDPIBenchmarks()
        {
            using (var tiff = T.Tiff.Open(@"./TestData/image0.tif", "r"))
            {
                int width = tiff.GetField(T.TiffTag.IMAGEWIDTH)[0].ToInt();
                int height = tiff.GetField(T.TiffTag.IMAGELENGTH)[0].ToInt();

                var raster = new int[width * height];
                if (!tiff.ReadRGBAImageOriented(width, height, raster, T.Orientation.TOPLEFT))
                {
                    throw new Exception("Unable to decode TIFF file");
                }
                var imageAsBmp = TiffExpander.CreateBitmapFromPixels(ref raster, width, height);
                var bmp = SKImage.FromBitmap(imageAsBmp);
                Image = bmp.Encode(SKEncodedImageFormat.Jpeg, 100);
            }
        }

        [Benchmark]
        public void SetJpegDPI()
        {
            unsafe
            {
                ImageProcessing.ImageProcessing.SetJpgDpi(new Span<byte>((void*)Image.Data, (int)Image.Size), 300, 300);
            }
        }
    }
}

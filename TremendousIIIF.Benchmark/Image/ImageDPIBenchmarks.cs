using BenchmarkDotNet.Attributes;
using Image.Common;
using Image.Tiff;
using SkiaSharp;
using System;
using System.Threading.Tasks;
using TremendousIIIF.Common;
using T = BitMiracle.LibTiff.Classic;
using Serilog;

namespace TremendousIIIF.Benchmark.Image
{
    [HtmlExporter, CsvExporter, RPlotExporter]
    [Config(typeof(MultipleRuntimes))]
    [MemoryDiagnoser]
    public class ImageDPIBenchmarks
    {
        public static SKData Image { get; set; }



        [GlobalSetup]
        public static void SetUp()
        {

            using (var tiff = T.Tiff.Open(@"C:\Jp2Cache\vdc_tiff", "r"))
            {
                int width = tiff.GetField(T.TiffTag.IMAGEWIDTH)[0].ToInt();
                int height = tiff.GetField(T.TiffTag.IMAGELENGTH)[0].ToInt();

                var raster = new int[width * height];
                if (!tiff.ReadRGBAImageOriented(width, height, raster, T.Orientation.TOPLEFT))
                {
                    throw new Exception("Unable to decode TIFF file");
                }
                var imageAsBmp = TiffExpander.CreateBitmapFromPixels(raster, width, height);
                var bmp = SKImage.FromBitmap(imageAsBmp);
                Image = bmp.Encode(SKEncodedImageFormat.Jpeg, 100);
            }


        }

        [Benchmark]
        public void SetJpegDPI() => ImageProcessing.ImageProcessing.SetJpgDpi(Image.Handle, Image.Size, 300, 300);

    }
}

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;
using BenchmarkDotNet.Attributes.Jobs;
using Image.Common;
using Image.Tiff;
using SkiaSharp;
using System;
using T = BitMiracle.LibTiff.Classic;
using TremendousIIIF.Common;
using System.IO;

namespace TremendousIIIF.Benchmark.TIFF
{
    [HtmlExporter, CsvExporter, RPlotExporter]
    //[MemoryDiagnoser]
    public class TiffExpanderBenchmarks
    {
        int[] imageData;
        SKBitmap imageAsBmp;
        SKImage imageAsImg;
        int width; int height;

        //[Params(256, 512, 1024, 2048, 2940)]
        [Params(256)]
        public int Width { get; set; }
        //        [Params(256, 512, 1024, 2048, 4688)]
        [Params(256)]
        public int Height { get; set; }

        [Params(0)]//, 1, 2, 3)]
        public int Row { get; set; }
        [Params(0)]//, 1, 2, 3)]
        public int Column { get; set; }

        public SKRectI Region
        {
            get
            {
                return new SKRectI(Row * 256, Column * 256, Width, Height);
            }
        }

        [GlobalSetup]
        public void Setup()
        {
            var request = new ImageRequest("", new ImageRegion(ImageRegionMode.Full), new ImageSize(ImageSizeMode.Max, 1, 10, 10), new ImageRotation(0, false), ImageQuality.@default, ImageFormat.jpg);
            using (var tiff = T.Tiff.Open(@"C:\Jp2Cache\vdc_tiff", "r"))
            {
                int width = tiff.GetField(T.TiffTag.IMAGEWIDTH)[0].ToInt();
                int height = tiff.GetField(T.TiffTag.IMAGELENGTH)[0].ToInt();

                var raster = new int[width * height];
                if (!tiff.ReadRGBAImageOriented(width, height, raster, T.Orientation.TOPLEFT))
                {
                    throw new IOException("Unable to decode TIFF file");
                }
                imageData = raster;
                this.width = width;
                this.height = height;
                imageAsBmp = TiffExpander.CreateBitmapFromPixels(imageData, width, height);
                imageAsImg = SKImage.FromBitmap(imageAsBmp);
            }
        }

        [Benchmark]
        public SKBitmap BmpFromPixels() => TiffExpander.CreateBitmapFromPixels(imageData, width, height);

        [Benchmark]
        public SKImage CopyBitmapRegion() => TiffExpander.CopyBitmapRegion(imageAsBmp, Width, Height, Region);

        [Benchmark]
        public SKImage CopyImageRegion() => TiffExpander.CopyImageRegion(imageAsImg, Width, Height, Region);
        [Benchmark]
        public SKImage CopyImageRegion2() => TiffExpander.CopyImageRegion2(imageAsBmp, Width, Height, Region);
    }
}



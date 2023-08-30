using BenchmarkDotNet.Attributes;
using Image.Tiff;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using T = BitMiracle.LibTiff.Classic;

namespace TremendousIIIF.Benchmark.TIFF
{
    [HtmlExporter, CsvExporter, RPlotExporter]
    [MemoryDiagnoser]
    public class TiffExpanderBenchmarks
    {
        int[] imageData;
        SKBitmap imageAsBmp;
        SKImage imageAsImg;
        int width; int height;

        [Params(256, 512, 1024, 2048, 2940)]

        public int Width { get; set; }
        [Params(256, 512, 1024, 2048, 4688)]
        public int Height { get; set; }

        [Params(0, 1, 2, 3)]
        public int Row { get; set; }
        [Params(0, 1, 2, 3)]
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
            using (var tiff = T.Tiff.Open(@"./TestData/image0.tif", "r"))
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
                imageAsBmp = TiffExpander.CreateBitmapFromPixels(ref imageData, width, height);
                imageAsImg = SKImage.FromBitmap(imageAsBmp);
            }
        }

        [Benchmark]
        public SKBitmap BmpFromPixels() => TiffExpander.CreateBitmapFromPixels(ref imageData, width, height);

        [Benchmark]
        public SKImage CopyBitmapRegion() => TiffExpander.CopyBitmapRegion(imageAsBmp, Width, Height, Region);

        [Benchmark]
        public SKImage CopyImageRegion() => TiffExpander.CopyImageRegion(imageAsImg, Width, Height, Region);
        [Benchmark]
        public SKImage CopyImageRegion2() => TiffExpander.CopyImageRegion2(imageAsBmp, Width, Height, Region);

        [Benchmark]
        public SKImage CopyImageRegion3() => TiffExpander.CopyImageRegion3(imageAsBmp, Width, Height, Region);
        [Benchmark]
        public SKImage CopyImageRegion4() => TiffExpander.CopyImageRegion4(imageAsBmp, Width, Height, Region);

    }
}

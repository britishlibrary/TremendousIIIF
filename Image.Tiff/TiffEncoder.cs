using SkiaSharp;
using System.IO;
using T = BitMiracle.LibTiff.Classic;

namespace Image.Tiff
{
    public static class TiffEncoder
    {
        public static Stream Encode(SKImage image)
        {
            (var bytes, var bpp) = GetImageRasterBytes(image);
            var stream = new TiffMemoryDestination(bytes.Length * bpp);

            using (var tiff = T.Tiff.ClientOpen("in-memory", "w", null, stream))
            {
                tiff.SetField(T.TiffTag.IMAGEWIDTH, image.Width);
                tiff.SetField(T.TiffTag.IMAGELENGTH, image.Height);

                tiff.SetField(T.TiffTag.ROWSPERSTRIP, image.Height);

                tiff.SetField(T.TiffTag.ORIENTATION, T.Orientation.TOPLEFT);

                tiff.SetField(T.TiffTag.COMPRESSION, T.Compression.LZW);
                tiff.SetField(T.TiffTag.PHOTOMETRIC, T.Photometric.RGB);

                tiff.SetField(T.TiffTag.PLANARCONFIG, T.PlanarConfig.CONTIG);

                tiff.SetField(T.TiffTag.BITSPERSAMPLE, 8);
                tiff.SetField(T.TiffTag.SAMPLESPERPIXEL, bpp);
      
                ConvertSamples(bytes, image.Width, image.Height, bpp);

                int stride = bytes.Length / image.Height;

                for (int i = 0, offset = 0; i < image.Height; i++)
                {
                    tiff.WriteScanline(bytes, offset, i, 0);
                    offset += stride;
                }
            }
            var ms = new MemoryStream(stream._data);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        private static (byte[], int) GetImageRasterBytes(SKImage image)
        {
            using (var bmp = SKBitmap.FromImage(image))
            {
                return (bmp.Bytes, bmp.BytesPerPixel);
            }
        }

        private static void ConvertSamples(byte[] data, int width, int height, int samplesPerPixel)
        {
            int stride = data.Length / height;

            for (int y = 0; y < height; y++)
            {
                int offset = stride * y;
                int strideEnd = offset + width * samplesPerPixel;

                for (int i = offset; i < strideEnd; i += samplesPerPixel)
                {
                    byte temp = data[i + 2];
                    data[i + 2] = data[i];
                    data[i] = temp;
                }
            }
        }
    }
}

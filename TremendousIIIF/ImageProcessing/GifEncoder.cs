using SkiaSharp;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TremendousIIIF.ImageProcessing
{
    public class GifEncoder
    {
        public static Stream Encode(SKImage image)
        {
            var output = new MemoryStream();
            WriteHeader(output, image.Width, image.Height, 0);

            output.Seek(0, SeekOrigin.Begin);
            return output;
        }

        private static void WriteHeader(Stream output, int width, int height, int repeat)
        {
            Span<byte> signature = Encoding.ASCII.GetBytes("GIF89a");
            
            output.Write(signature);
            Span<byte> header = stackalloc byte[9];
            BinaryPrimitives.WriteUInt32LittleEndian(header, (uint)width);
            BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(4, 4), (uint)height);
            header[8] = 0x0;


            Span<byte> buffer = stackalloc byte[19];
            buffer[0] = 0x21; // Extension introducer
            buffer[1] = 0xFF; // Application extension
            buffer[2] = 0x0B; // Size of block
            buffer[3] = (byte)'N'; // NETSCAPE2.0
            buffer[4] = (byte)'E';
            buffer[5] = (byte)'T';
            buffer[6] = (byte)'S';
            buffer[7] = (byte)'C';
            buffer[8] = (byte)'A';
            buffer[9] = (byte)'P';
            buffer[10] = (byte)'E';
            buffer[11] = (byte)'2';
            buffer[12] = (byte)'.';
            buffer[13] = (byte)'0';
            buffer[14] = 0x03; // Size of block
            buffer[15] = 0x01; // Loop indicator
            buffer[16] = (byte)(repeat % 0x100); // Number of repetitions
            buffer[17] = (byte)(repeat / 0x100); // 0 for endless loop
            buffer[18] = 0x00; // Block terminator

            output.Write(buffer);
        }



    }
}

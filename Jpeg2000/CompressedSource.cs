using System;
using kdu_mni;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Jpeg2000
{
    public class CompressedSource: Ckdu_compressed_source_nonnative
    {
        long _startOfCodestreamOffset = 0;
        long position = 0;
        byte[] data = null;
        Uri imageUri;
        Stream stream;

        public CompressedSource(byte[] buf, int codestreamOffset)        {
            Init(null, buf, codestreamOffset);
        }

        public CompressedSource(Uri imageUri, byte[] buf)
        {
            Init(imageUri, buf, 0);
        }

        public CompressedSource(Uri imageUri)
        {
            Init(imageUri, null, 0);
        }

        public CompressedSource(Stream stream)
        {
            //Init(imageUri, null, 0);
            this.stream = stream;
        }

        private void Init(Uri imageUri, byte[] buf, int codestreamOffset)
        {
            this.imageUri = imageUri;

            this.data = buf;
            this._startOfCodestreamOffset = codestreamOffset;
        }

        public override int get_capabilities()
        {
            return Ckdu_global.KDU_SOURCE_CAP_SEQUENTIAL | Ckdu_global.KDU_SOURCE_CAP_SEEKABLE;// | Ckdu_global.KDU_SOURCE_CAP_IN_MEMORY;
        }

        public override bool seek(long _offset)
        {
            //stream.Seek(_offset, SeekOrigin.Begin);
            //return true;
            try
            {
                position = _offset;
                if (data != null && _offset < data.Length)
                {
                    position = _offset + _startOfCodestreamOffset;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        //public override int post_read(int requested_bytes)
        //{

        //    var num_bytes = requested_bytes;// (data.Length - position) < requested_bytes ? data.Length - (int)position : requested_bytes;
        //    byte[] bytesread = new byte[num_bytes];
        //    try
        //    {

        //        var read = stream.Read(bytesread, 0, num_bytes);

        //        //if (data == null || position + num_bytes > data.Length)
        //        //{
        //        //    bytesread = RequestByteRange(position, num_bytes);
        //        //}
        //        //else
        //        //{
        //        //    Buffer.BlockCopy(data, (int)position, bytesread, 0, num_bytes);
        //        //}

        //        position += num_bytes;

        //        //push data to Kakadu
        //        base.push_data(bytesread, 0, bytesread.Length);
        //    } catch (Exception e)
        //    {
        //        var x = e;
        //    }
        //    return bytesread.Length;
        //}
        public override int post_read(int requested_bytes)
        {
            var num_bytes = (data.Length - position) < requested_bytes ? data.Length - (int)position : requested_bytes;
            byte[] bytesread = new byte[num_bytes];

            if (data == null || position + num_bytes > data.Length)
            {
                bytesread = RequestByteRange(position, num_bytes);
            }
            else
            {
                Buffer.BlockCopy(data, (int)position, bytesread, 0, num_bytes);
            }

            position += num_bytes;

            //push data to Kakadu
            base.push_data(bytesread, 0, bytesread.Length);
            return bytesread.Length;
        }

        public override long get_pos()
        {
            return position;
        }

        private byte[] RequestByteRange(long position, int length)
        {
            byte[] buffer = buffer = new byte[length];

            using (HttpClient client = new HttpClient())
            using (HttpRequestMessage firstByteRequest = new HttpRequestMessage(HttpMethod.Get, imageUri))
            {
                firstByteRequest.Headers.Range = new RangeHeaderValue(position, position + length);
                using (HttpResponseMessage response = client.SendAsync(firstByteRequest).Result)
                {
                    response.EnsureSuccessStatusCode();
                    using (Stream stream = response.Content.ReadAsStreamAsync().Result)
                    {
                        int BytesRead = stream.Read(buffer, 0, buffer.Length);
                    }
                }
            }

            return buffer;
        }

        public override bool close()
        {
            return base.close();
        }
    }

}

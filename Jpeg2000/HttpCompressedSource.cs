using kdu_mni;
using System;
using System.Threading;
using System.Net.Http;
using Nito.AsyncEx;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.IO;
using Serilog;

namespace Jpeg2000
{
    public class HttpCompressedSource : Ckdu_compressed_source_nonnative
    {
        private Uri _imageUri;
        private Int64 _offset = 0;
        private bool _headerOnly;
        const int JP2HeaderLength = 1135;
        private string RequestId;
        private AsyncLazy<byte[]> _data;
        private HttpClient _client;
        private ILogger Log;

        public HttpCompressedSource(HttpClient client, ILogger log, Uri imageUri, string requestId, bool headerOnly = false)
        {
            _imageUri = imageUri;
            _headerOnly = headerOnly;
            RequestId = requestId;
            _client = client;
            _data = new AsyncLazy<byte[]>(() => GetData(_headerOnly));
            Log = log;
        }

        private async Task<byte[]> ReadData()
        {
            return await _data;
        }

        public override int post_read(int num_bytes)
        {
            byte[] requestedBytes = new byte[num_bytes];
            var data = ReadData().Result;
            var actual_bytes = (data.Length - _offset) < num_bytes ? data.Length - (int)_offset : num_bytes;
            Buffer.BlockCopy(data, (int)_offset, requestedBytes, 0, actual_bytes);
            _offset = Interlocked.Add(ref _offset, actual_bytes);
            push_data(requestedBytes, 0, actual_bytes);
            return actual_bytes;
        }

        public override int get_capabilities()
        {
            return Ckdu_global.KDU_SOURCE_CAP_SEQUENTIAL| Ckdu_global.KDU_SOURCE_CAP_SEEKABLE;
        }

        public override bool seek(long offset)
        {
            if (offset > ReadData().Result.Length)
            {
                return false;
            }
            Interlocked.Exchange(ref _offset, offset);
            return true;
        }

        public override long get_pos()
        {
            return _offset;
        }

        public override bool close()
        {
            _client = null;
            _data = null;
            return base.close();
        }

        private async Task<byte[]> GetData(bool headerOnly)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, _imageUri))
            {
                if (headerOnly)
                {
                    request.Headers.Range = new RangeHeaderValue(0, JP2HeaderLength);
                }
                request.Headers.Add("X-Request-ID", RequestId);
                try
                {
                    using (var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.PartialContent)
                        {
                            return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                        }
                        switch (response.StatusCode)
                        {
                            case System.Net.HttpStatusCode.NotFound:
                                throw new FileNotFoundException("Unable to load source image", _imageUri.ToString());
                            default:
                            case System.Net.HttpStatusCode.InternalServerError:
                                throw new IOException("Unable to load source image");
                        }
                    }
                }
                catch (TaskCanceledException e)
                {
                    if(e.CancellationToken.IsCancellationRequested)
                    {
                        Log.Error(e, "HTTP Request Cancelled");
                        throw;
                    }
                    else {
                        Log.Error(e, "HTTP Request Failed");
                        throw e.InnerException;
                    }
                }
            }
        }
    }
}

using BitMiracle.LibTiff.Classic;
using System;
using System.IO;
using System.Threading;
using System.Net.Http;
using Nito.AsyncEx;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Serilog;

namespace Image.Tiff
{
    public class TiffSource : TiffStream
    {
        private HttpClient httpClient;
        private Int64 _size = 0;
        private AsyncLazy<byte[]> _data;
        private Uri _imageUri;
        private Int64 _offset = 0;
        private bool headerOnly = false;
        private string RequestId;
        private ILogger Log;
        private long TiffHeaderLength = 460800;

        public TiffSource(HttpClient httpClient, ILogger log, Uri imageUri, string requestId, bool headerOnly = false)
        {
            Log = log;
            this.headerOnly = headerOnly;
            this.httpClient = httpClient;
            _imageUri = imageUri;
            RequestId = requestId;
            _data = new AsyncLazy<byte[]>(() => GetData(headerOnly));
        }


        public override int Read(object clientData, byte[] buffer, int offset, int count)
        {
            var data = ReadData().Result;
            var actual_bytes = (data.Length - _offset) < count ? data.Length - (int)_offset : count;
            Buffer.BlockCopy(data, (int)_offset, buffer, offset, actual_bytes);
            _offset = Interlocked.Add(ref _offset, actual_bytes);
            return actual_bytes;
        }
        private async Task<byte[]> ReadData()
        {
            return await _data;
        }

        public override long Size(object clientData)
        {
            return _size;
        }

        public override long Seek(object clientData, long offset, SeekOrigin origin)
        {
            Log.Debug("Seek offset={@offset} origin={@origin}", offset, origin);
            Interlocked.Exchange(ref _offset, offset);
            return offset;
        }

        private async Task<byte[]> GetData(bool headerOnly)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, _imageUri))
            {
                if (headerOnly)
                {
                    request.Headers.Range = new RangeHeaderValue(0, TiffHeaderLength);
                }
                request.Headers.Add("X-Request-ID", RequestId);
                try
                {
                    using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
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
                                throw new FileLoadException("Unable to load source image");
                        }
                    }
                }
                catch (TaskCanceledException e)
                {
                    if (e.CancellationToken.IsCancellationRequested)
                    {
                        Log.Error(e, "HTTP Request Cancelled");
                        throw;
                    }
                    else
                    {
                        Log.Error(e, "HTTP Request Failed");
                        throw e.InnerException;
                    }
                }
            }
        }
        public override void Close(object clientData)
        {
            
        }
    }
}

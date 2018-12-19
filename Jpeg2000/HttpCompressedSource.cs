using kdu_mni;
using System;
using System.Threading;
using System.Net.Http;
using Nito.AsyncEx;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.IO;
using Serilog;
using System.IO.Pipelines;
using System.Runtime.InteropServices;

namespace Jpeg2000
{
    /// <summary>
    /// Naive HTTP source for Kakadu
    /// </summary>
    public class HttpCompressedSource : Ckdu_compressed_source_nonnative
    {
        private Uri _imageUri;
        private long _offset = 0;
        private bool _headerOnly;
        const int JP2HeaderLength = 1135;
        private readonly string RequestId;
        //private AsyncLazy<Memory<byte>> _data;
        private HttpClient _client;
        private ILogger Log;
        private Pipe Pipe;
        Task Reader;
        Task Writer;
        private Stream _httpData;
        private MemoryStream _data;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client">Shared <see cref="HttpClient"/> to use for HTTP requests</param>
        /// <param name="log">Shared <see cref="ILogger"/> instance to use for logging</param>
        /// <param name="imageUri">The <see cref="Uri"/> of the remote image</param>
        /// <param name="requestId">The correlation ID to include on subsequent HTTP requests</param>
        /// <param name="headerOnly">Attempt to retrieve heade bytes only for metadata requests</param>
        public HttpCompressedSource(HttpClient client, ILogger log, Uri imageUri, string requestId, bool headerOnly = false)
        {
            _imageUri = imageUri;
            _headerOnly = headerOnly;
            RequestId = requestId;
            _client = client;
            //_data = new AsyncLazy<Memory<byte>>(() => GetData(_headerOnly));
            Log = log;
            _data = new MemoryStream();
            //Reader = GetData(Pipe.Reader);
            //_httpData = Task.Run(()=>GetData(_headerOnly)).Result;
        }

        public async Task Initialise()
        {
            _httpData = await GetData(_headerOnly).ConfigureAwait(false);
            await _httpData.CopyToAsync(_data).ConfigureAwait(false);
            _data.Seek(0, SeekOrigin.Begin);
        }

        //private async Task<Memory<byte>> ReadData()
        //{
        //    return await _data;
        //}

        //public override int post_read(int num_bytes)
        //{
        //    //byte[] requestedBytes = new byte[num_bytes];
        //    var data = ReadData().Result;
        //    var actual_bytes = (data.Length - _offset) < num_bytes ? data.Length - (int)_offset : num_bytes;
        //    //Buffer.BlockCopy(data, (int)_offset, requestedBytes, 0, actual_bytes);

        //    //push_data(requestedBytes, 0, actual_bytes);
        //    //return actual_bytes;

        //    push_data(data.Slice((int)_offset, actual_bytes).Span.(), 0, actual_bytes);
        //    _offset = Interlocked.Add(ref _offset, actual_bytes);
        //    return actual_bytes;
        //}

        public override int post_read(int num_bytes)
        {
            var buffer = new byte[num_bytes];

            try
            {

                var bytes_read = _data.Read(buffer, 0, num_bytes);

                push_data(buffer, 0, bytes_read);
                _offset += bytes_read;
                return bytes_read;



            }
            catch (Exception e)
            {
                Log.Error(e, "Exception reading network string");
            }
            return 0;

        }

        public override int get_capabilities()
        {
            return Ckdu_global.KDU_SOURCE_CAP_SEQUENTIAL | Ckdu_global.KDU_SOURCE_CAP_SEEKABLE;
        }

        public override bool seek(long offset)
        {
            //return false;
            //if (offset > ReadData().Result.Length)
            if (offset > _data.Length)
            {
                return false;
            }
            _offset = offset;
            _data.Seek(offset, SeekOrigin.Begin);
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
            _httpData.Dispose();
            _httpData = null;
            return base.close();
        }
        private async Task ReadPipe(PipeReader reader)
        {
            await reader.ReadAsync();
        }

        //private async Task WritePipe(PipeWriter writer)
        //{
        //    using (var request = new HttpRequestMessage(HttpMethod.Get, _imageUri))
        //    {
        //        request.Headers.Add("X-Request-ID", RequestId);
        //        try
        //        {
        //            using (var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
        //            {
        //                if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.PartialContent)
        //                {
        //                    var memory = writer.GetMemory();
        //                    var dataStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        //                    var rawBuffer = MemoryMarshal.Cast<Memory<byte>, byte>(memory.Span);
        //                    dataStream.Read(rawBuffer, 0, 0);
        //                }
        //                switch (response.StatusCode)
        //                {
        //                    case System.Net.HttpStatusCode.NotFound:
        //                        throw new FileNotFoundException("Unable to load source image", _imageUri.ToString());
        //                    default:
        //                    case System.Net.HttpStatusCode.InternalServerError:
        //                        throw new IOException("Unable to load source image");
        //                }
        //            }
        //        }
        //        catch (TaskCanceledException e)
        //        {
        //            if (e.CancellationToken.IsCancellationRequested)
        //            {
        //                Log.Error(e, "HTTP Request Cancelled");
        //                throw;
        //            }
        //            else
        //            {
        //                Log.Error(e, "HTTP Request Failed");
        //                throw e.InnerException;
        //            }
        //        }
        //    }
        //}

        private async Task<Stream> GetData(bool headerOnly, CancellationToken token = default)
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
                    //using (var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
                    var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.PartialContent)
                        {
                            //using (token.Register(response.Dispose))
                            //{
                            return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                            

                            //}
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
    }
}

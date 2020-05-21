using BitMiracle.LibTiff.Classic;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace Image.Tiff
{
    /// <summary>
    /// libtiff is very Seeky, meaning we can't simply read the network stream back in order from the HTTP request. NetworkSTream doesn't support seeking (obviously)
    /// so we have to copy it to a memory stream; this is bad for memory use.
    /// </summary>
    public class TiffHttpSource : TiffStream
    {
        private readonly HttpClient httpClient;
        private long _size = 0;
        private MemoryStream _data;
        private readonly Uri _imageUri;
        private long _offset = 0;
        private readonly ILogger Log;

        public TiffHttpSource(HttpClient httpClient, ILogger log, Uri imageUri)
        {
            Log = log;
            this.httpClient = httpClient;
            _imageUri = imageUri;
            
        }

        public async Task Initialise()
        {
            await GetData();
            _data.Seek(0, SeekOrigin.Begin);
        }


        public override int Read(object clientData, byte[] buffer, int offset, int count)
        {
            //var data = ReadData().Result;
            //var actual_bytes = (data.Length - _offset) < count ? data.Length - (int)_offset : count;
            //Buffer.BlockCopy(data, (int)_offset, buffer, offset, actual_bytes);
            //_offset = Interlocked.Add(ref _offset, actual_bytes);
            //return actual_bytes;
            var actual_bytes = _data.Read(buffer, offset, count);
            _offset += actual_bytes;
            return actual_bytes;

        }


        public override long Size(object clientData)
        {
            return _size;
        }

        public override long Seek(object clientData, long offset, SeekOrigin origin)
        {
            //Log.Debug("Seek offset={@Offset} origin={@Origin}", offset, origin);
            //Interlocked.Exchange(ref _offset, offset);
            //return offset;
            _offset = _data.Seek(offset, origin);
            return _offset;
            //return -1;
        }

        private async Task GetData()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, _imageUri);
            try
            {
                var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.PartialContent)
                    {
                        _size = response.Content.Headers.ContentLength.GetValueOrDefault();
                        _data = new MemoryStream((int)_size);
                        var data = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                        await data.CopyToAsync(_data).ConfigureAwait(false);
                        await data.FlushAsync();
                        response.Dispose();
                        return;
                    }
                    throw response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.NotFound => new FileNotFoundException("Unable to load source image", _imageUri.ToString()),
                        _ => new IOException("Unable to load source image"),
                    };
                }
            }
            catch (TaskCanceledException e)
            {
                if (e.CancellationToken.IsCancellationRequested)
                {
                    Log.LogError(e, "HTTP Request Cancelled");
                    throw;
                }
                else
                {
                    Log.LogError(e, "HTTP Request Failed");
                    throw e.InnerException;
                }
            }
        }
        public override void Close(object clientData)
        {
            _data.Dispose();
            _data = null;
        }
    }
}

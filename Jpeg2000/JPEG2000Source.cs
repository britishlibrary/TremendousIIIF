using System;
using System.IO;
using kdu_mni;
using System.Net.Http;
using Serilog;
using System.Threading.Tasks;

namespace Jpeg2000
{
    public class JPEG2000Source : Cjp2_family_src
    {
        const int JP2HeaderLength = 1135;
        private readonly string RequestId;
        private readonly ILogger Log;
        Ckdu_compressed_source_nonnative compSrc;

        public bool queueRequests = false;

        public JPEG2000Source(ILogger log, string requestId)
        {
            RequestId = requestId;
            Log = log;
        }

        public async ValueTask Initialise(HttpClient client, Uri imageUri, bool headerOnly)
        {
            if (imageUri.Scheme == "http" || imageUri.Scheme == "https")
            {
                var src = new HttpCompressedSource(client, Log, imageUri, RequestId, headerOnly);
                compSrc = src;
                await src.Initialise().ConfigureAwait(false);
            }
        }

        public void Open(Uri imageUri)
        {
            if (null != compSrc)
            {
                
                base.open(compSrc);
            }
            else
            {
                string filename = imageUri.LocalPath;
                base.open(GetFilePath(filename), true);
            }
        }

        public new void close()
        {
            if (compSrc != null)
            {
                compSrc.close();
                compSrc.Dispose();
                compSrc = null;
            }
            base.close();
        }

        private string GetFilePath(string filename)
        {
            if (File.Exists(filename))
                return filename;
            else
                throw new FileNotFoundException(filename + " not found");
        }
    }
}

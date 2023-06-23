using System;
using System.IO;
using kdu_mni;
using System.Net.Http;
using Serilog;

namespace Jpeg2000
{
    public class JPEG2000Source : Cjp2_family_src
    {
        const int JP2HeaderLength = 1135;
        string RequestId;
        ILogger Log;
        Ckdu_compressed_source_nonnative compSrc;

        public bool queueRequests = false;

        public JPEG2000Source(ILogger log, string requestId)
        {
            RequestId = requestId;
            Log = log;
        }

        public void Open(HttpClient client, Uri imageUri, bool headerOnly)
        {
            if (imageUri.Scheme == "http" || imageUri.Scheme == "https")
            {
                compSrc = new HttpCompressedSource(client, Log, imageUri, RequestId, headerOnly);
                base.open(compSrc);
            }
            else if (imageUri.IsFile)
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

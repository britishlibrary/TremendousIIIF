using System;
using System.IO;
using kdu_mni;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace Jpeg2000
{
    public class JPEG2000Source : Cjp2_family_src
    {
        const int JP2HeaderLength = 1135;
        private readonly ILogger Log;
        public Ckdu_compressed_source_nonnative compSrc;
        Ckdu_codestream codestream = new Ckdu_codestream();
        
        public bool queueRequests = false;

        public JPEG2000Source(ILogger log)
        {
            Log = log;
        }

        public async ValueTask Initialise(HttpClient client, Uri imageUri, bool headerOnly, CancellationToken token = default)
        {
            if (imageUri.Scheme == "http" || imageUri.Scheme == "https")
            {
                var src = new HttpCompressedSource(client, Log, imageUri, headerOnly);
                compSrc = src;

                await src.Initialise(token).ConfigureAwait(false);
                //codestream.create(compSrc);
            }
        }

        public void Initialise(Stream stream)
        {
            if (null != stream)
            {
                var src = new StreamCompressedSource(stream);
                compSrc = src;
            }
            else
            {
                
            }

        }

        public Ckdu_codestream GetCodestream()
        {
            if (!codestream.exists() && compSrc != null)
            //Ckdu_codestream codestream = new Ckdu_codestream();
                codestream.create(compSrc);
            return codestream;
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

#pragma warning disable IDE1006 // Naming Styles
        public new void close()
#pragma warning restore IDE1006 // Naming Styles
        {
            if (codestream.exists())
                codestream.destroy();
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

using kdu_mni;
using System.Text;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Jpeg2000
{
    public class KakaduMessage : Ckdu_message
    {
        private readonly bool ThrowException;
        readonly ILogger Log;
        StringBuilder message;
        public KakaduMessage(bool raise_exception, ILogger log)
        {
            ThrowException = raise_exception;
            Log = log;
        }
        public override void put_text(string text)
        {
            if (null == message)
            {
                message = new StringBuilder();
            }
            message.Append(text);
        }

        public override void flush(bool end_of_message)
        {
            if(null != message) {
                if (end_of_message && message.Length > 0)
                    Log.LogError("KDU ERROR {@KDU}", message.ToString());
                if (end_of_message && ThrowException)
                    throw new IOException(message.ToString());
            }
        }
    }
}

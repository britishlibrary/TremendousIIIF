using kdu_mni;
using Serilog;
using System.Text;
using System.IO;

namespace Jpeg2000
{
    public class KakaduMessage : Ckdu_message
    {
        private readonly bool ThrowException;
        ILogger Log;
        StringBuilder message = new StringBuilder();
        public KakaduMessage(bool raise_exception, ILogger log)
        {
            ThrowException = raise_exception;
            Log = log;
        }
        public override void put_text(string text)
        {
            message.Append(text);
        }

        public override void flush(bool end_of_message)
        {
            Log.Error("KDU ERROR {@KDU}", message.ToString());
            if (end_of_message && ThrowException)
                throw new IOException(message.ToString());
        }
    }
}

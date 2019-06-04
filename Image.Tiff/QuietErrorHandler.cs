using T = BitMiracle.LibTiff.Classic;

namespace Image.Tiff
{
    public class QuietErrorHandler : T.TiffErrorHandler
    {
        /// <summary>
        /// We don't care about warnings about tags being out of order at this stage!
        /// </summary>
        /// <param name="tif"></param>
        /// <param name="module"></param>
        /// <param name="fmt"></param>
        /// <param name="ap"></param>
        public override void WarningHandler(T.Tiff tif, string module, string fmt, params object[] ap)
        {
            
        }
    }
}

using System;

namespace TremendousIIIF.Common
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class ImageFormatMetadataAttribute : Attribute
    {
        public string MimeType { get; private set; }


        public ImageFormatMetadataAttribute(string mimeType)
        {
            MimeType = mimeType;
        }

    }
}

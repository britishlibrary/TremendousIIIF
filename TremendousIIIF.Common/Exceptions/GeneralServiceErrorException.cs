using System;
using System.Collections.Generic;
using System.Text;

namespace TremendousIIIF.Common.Exceptions
{
    [Serializable]
    public class GeneralServiceErrorException : Exception, IHttpServiceError
    {
        public GeneralServiceErrorException()
            : base() { }
        public GeneralServiceErrorException(string message)
            : base(message) { }
        public GeneralServiceErrorException(string message, Exception innerException)
            : base(message, innerException) { }
        public HttpServiceError HttpServiceError { get { return HttpServiceErrorDefinition.GeneralError; } }
    }
}

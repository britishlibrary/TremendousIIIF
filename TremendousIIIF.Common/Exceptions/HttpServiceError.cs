using System;
using System.Collections.Generic;
using System.Text;

namespace TremendousIIIF.Common.Exceptions
{
    public class HttpServiceError
    {
        public ServiceErrorModel ServiceErrorModel { get; set; }
        public System.Net.HttpStatusCode HttpStatusCode { get; set; }
    }
}

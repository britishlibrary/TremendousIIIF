using System;
using System.Collections.Generic;
using System.Text;

namespace TremendousIIIF.Common.Exceptions
{
    public class ServiceErrorModel
    {
        public ServiceErrorCode Code { get; set; }

        public string Details { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace TremendousIIIF.Common.Exceptions
{
    public enum ServiceErrorCode
    {
        GeneralError = 0,
        NotFound = 10,
        InternalServerError = 20,
        InvalidToken = 30
    }
}

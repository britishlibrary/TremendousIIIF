using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TremendousIIIF.Common;

namespace TremendousIIIF.Test
{
    [ExcludeFromCodeCoverage]
    public class AcceptHeaderTests
    {
        [Theory]
        [InlineData("application/ld+json;profile=\"http://iiif.io/api/image/3/context.json\"", ApiVersion.v3_0, ApiVersion.v2_1)]
        [InlineData("application/ld+json", ApiVersion.v2_1, ApiVersion.v2_1)]
        [InlineData("application/json", ApiVersion.v2_1, ApiVersion.v2_1)]
        public void Accept_v2_1_Header(string accept, ApiVersion expected, ApiVersion defaultVersion)
        {
            var apiVersion = Controllers.IIIFController.ParseAccept(accept, defaultVersion);
            Assert.Equal(expected, apiVersion);

        }

        [Theory]
        [InlineData("application/ld+json;profile=\"http://iiif.io/api/image/3/context.json\"", ApiVersion.v3_0, ApiVersion.v3_0)]
        [InlineData("application/ld+json", ApiVersion.v3_0, ApiVersion.v3_0)]
        [InlineData("application/json", ApiVersion.v3_0, ApiVersion.v3_0)]
        public void Accept_v3_0_Header(string accept, ApiVersion expected, ApiVersion defaultVersion)
        {
            var apiVersion = Controllers.IIIFController.ParseAccept(accept, defaultVersion);
            Assert.Equal(expected, apiVersion);

        }
    }
}

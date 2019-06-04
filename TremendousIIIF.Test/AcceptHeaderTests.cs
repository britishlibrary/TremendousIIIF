using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TremendousIIIF.Common;

namespace TremendousIIIF.Test
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class AcceptHeaderTests
    {
        [TestMethod]
        [DataTestMethod]
        [DataRow("application/ld+json;profile=\"http://iiif.io/api/image/3/context.json\"", ApiVersion.v3_0, ApiVersion.v2_1)]
        [DataRow("application/ld+json", ApiVersion.v2_1, ApiVersion.v2_1)]
        [DataRow("application/json", ApiVersion.v2_1, ApiVersion.v2_1)]
        public void Blah(string accept, ApiVersion expected, ApiVersion defaultVersion)
        {
            var apiVersion = Controllers.IIIFController.ParseAccept(accept, defaultVersion);
            Assert.AreEqual(expected, apiVersion);

        }

    }
}

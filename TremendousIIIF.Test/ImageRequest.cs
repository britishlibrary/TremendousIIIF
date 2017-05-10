using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TremendousIIIF.Validation;
using Image.Common;
using System.Diagnostics.CodeAnalysis;

namespace TremendousIIIF.Test
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class ImageRequest
    {
        [TestMethod]
        public void CaluclateRegion_Full()
        {
            var result = ImageRequestValidator.CalculateRegionCustom("full");
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Mode, ImageRegionMode.Full);
            Assert.AreEqual(0f, result.X);
            Assert.AreEqual(0f, result.Y);
            Assert.AreEqual(0f, result.Width);
            Assert.AreEqual(0f, result.Height);
        }
        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void CaluclateRegion_Full_Invalid()
        {
            var result = ImageRequestValidator.CalculateRegionCustom("fullish");
        }
        [TestMethod]
        public void CaluclateRegion_Square()
        {
            var result = ImageRequestValidator.CalculateRegionCustom("square");
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Mode, ImageRegionMode.Square);
            Assert.AreEqual(0f, result.X);
            Assert.AreEqual(0f, result.Y);
            Assert.AreEqual(0f, result.Width);
            Assert.AreEqual(0f, result.Height);
        }
        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void CaluclateRegion_Square_Invalid()
        {
            var result = ImageRequestValidator.CalculateRegionCustom("squared");
        }
        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void CaluclateRegion_Invalid()
        {
            var result = ImageRequestValidator.CalculateRegionCustom("blah");
        }
        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void CaluclateRegion_Invalid_With_4_comma()
        {
            var result = ImageRequestValidator.CalculateRegionCustom("12334265346yu,,,,");
        }
        [TestMethod]
        public void CaluclateRegion_Pct()
        {
            var result = ImageRequestValidator.CalculateRegionCustom("pct:41.6,7.5,40,70");
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Mode, ImageRegionMode.PercentageRegion);
            Assert.AreEqual(41.6f, result.X);
            Assert.AreEqual(7.5f, result.Y);
            Assert.AreEqual(40f, result.Width);
            Assert.AreEqual(70f, result.Height);
        }
        [TestMethod]
        public void CaluclateRegion_Region()
        {
            var result = ImageRequestValidator.CalculateRegionCustom("256,256,256,256");
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Mode, ImageRegionMode.Region);
            Assert.AreEqual(256, result.X);
            Assert.AreEqual(256, result.Y);
            Assert.AreEqual(256, result.Width);
            Assert.AreEqual(256, result.Height);
        }
        [ExpectedException(typeof(FormatException))]
        public void CaluclateRegion_Pct_Inavlid()
        {
            var result = ImageRequestValidator.CalculateRegionCustom("pct:blah,blah,blah,blah");
        }
        // size
        [TestMethod]
        public void CalculateSize_Pct_Valid_Int()
        {
            var result = ImageRequestValidator.CalculateSizeCustom("pct:50");
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Mode, ImageSizeMode.PercentageScaled);
            Assert.AreEqual(0f, result.Width);
            Assert.AreEqual(0f, result.Height);
            Assert.AreEqual(0.5f, result.Percent);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void CalculateSize_Pct_Invalid_Int()
        {
            var result = ImageRequestValidator.CalculateSizeCustom("pct:50f");
        }
        public void CalculateSize_Pct_Valid_Float()
        {
            var result = ImageRequestValidator.CalculateSizeCustom("pct:50.0");
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Mode, ImageSizeMode.PercentageScaled);
            Assert.AreEqual(0f, result.Width);
            Assert.AreEqual(0f, result.Height);
            Assert.AreEqual(0.5f, result.Percent);
        }

        [TestMethod]
        public void CalculateSize_Full()
        {
            var result = ImageRequestValidator.CalculateSizeCustom("full");
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Mode, ImageSizeMode.Max);
            Assert.AreEqual(0f, result.Width);
            Assert.AreEqual(0f, result.Height);
        }
        [TestMethod]
        public void CalculateSize_Max()
        {
            var result = ImageRequestValidator.CalculateSizeCustom("max");
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Mode, ImageSizeMode.Max);
            Assert.AreEqual(0f, result.Width);
            Assert.AreEqual(0f, result.Height);
        }
        [TestMethod]
        public void CalculateSize_Width()
        {
            var result = ImageRequestValidator.CalculateSizeCustom("256,");
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Mode, ImageSizeMode.Exact);
            Assert.AreEqual(256, result.Width);
            Assert.AreEqual(0, result.Height);
        }
        [TestMethod]
        public void CalculateSize_Height()
        {
            var result = ImageRequestValidator.CalculateSizeCustom(",256");
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Mode, ImageSizeMode.Exact);
            Assert.AreEqual(0, result.Width);
            Assert.AreEqual(256, result.Height);
        }

        [TestMethod]
        public void CalculateSize_Width_Height()
        {
            var result = ImageRequestValidator.CalculateSizeCustom("256,512");
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Mode, ImageSizeMode.Exact);
            Assert.AreEqual(256, result.Width);
            Assert.AreEqual(512, result.Height);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void CalculateSize_Empty_Width_Height()
        {
            var result = ImageRequestValidator.CalculateSizeCustom(",");
        }
        [TestMethod]
        public void CalculateSize_Best()
        {
            var result = ImageRequestValidator.CalculateSizeCustom("!256,100");
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Mode, ImageSizeMode.SpecifiedFit);
            Assert.AreEqual(256, result.Width);
            Assert.AreEqual(100, result.Height);
        }
    }
}

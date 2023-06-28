using Image.Common;
using LanguageExt.Pipes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using TremendousIIIF.Validation;
using XunitAssertMessages;

namespace TremendousIIIF.Test
{
    [ExcludeFromCodeCoverage]
    public class ImageRequest
    {
        [Fact]
        public void CaluclateRegion_Full()
        {
            var result = ImageRequestValidator.CalculateRegion("full");
            Assert.NotNull(result);
            Assert.True(result.IsSome);
            foreach (var r in result)
            {
                AssertM.Equal(r.Mode, ImageRegionMode.Full);
                Assert.Equal(0f, r.X);
                Assert.Equal(0f, r.Y);
                Assert.Equal(0f, r.Width);
                Assert.Equal(0f, r.Height);
            }
        }
        [Fact]
        public void CaluclateRegion_Full_Invalid()
        {
            var result = ImageRequestValidator.CalculateRegion("fullish");
            Assert.True(result.IsNone);
        }
        [Fact]
        public void CaluclateRegion_Square()
        {
            var r = ImageRequestValidator.CalculateRegion("square");
            Assert.NotNull(r);
            Assert.True(r.IsSome);
            foreach (var result in r)
            {
                AssertM.Equal(result.Mode, ImageRegionMode.Square);
                Assert.Equal(0f, result.X);
                Assert.Equal(0f, result.Y);
                Assert.Equal(0f, result.Width);
                Assert.Equal(0f, result.Height);
            }
        }
        [Fact]
        public void CaluclateRegion_Square_Invalid()
        {
            var result = ImageRequestValidator.CalculateRegion("squared");
            Assert.True(result.IsNone);
        }
        [Fact]
        public void CaluclateRegion_Invalid()
        {
            var result = ImageRequestValidator.CalculateRegion("blah");
            Assert.True(result.IsNone);
        }
        [Fact]
        public void CaluclateRegion_Invalid_With_4_comma()
        {
            var result = ImageRequestValidator.CalculateRegion("12334265346yu,,,,");
            Assert.True(result.IsNone);

        }
        [Fact]
        public void CaluclateRegion_Pct()
        {
            var r = ImageRequestValidator.CalculateRegion("pct:41.6,7.5,40,70");
            Assert.NotNull(r);
            Assert.True(r.IsSome);
            foreach (var result in r)
            {
                AssertM.Equal(result.Mode, ImageRegionMode.PercentageRegion);
                Assert.Equal(41.6f, result.X);
                Assert.Equal(7.5f, result.Y);
                Assert.Equal(40f, result.Width);
                Assert.Equal(70f, result.Height);
            }
        }
        [Fact]
        public void CaluclateRegion_Region()
        {
            var r = ImageRequestValidator.CalculateRegion("256,256,256,256");
            Assert.NotNull(r);
            Assert.True(r.IsSome);
            foreach (var result in r)
            {
                AssertM.Equal(result.Mode, ImageRegionMode.Region);
                Assert.Equal(256, result.X);
                Assert.Equal(256, result.Y);
                Assert.Equal(256, result.Width);
                Assert.Equal(256, result.Height);
            }

        }

        [Fact]
        public void CaluclateRegion_Pct_Inavlid()
        { 
            var result = ImageRequestValidator.CalculateRegion("pct:blah,blah,blah,blah");
            Assert.Equal(result, Option<ImageRegion>.None);
        }
        // size
        [Fact]
        public void CalculateSize_Pct_Valid_Int()
        {
            var r = ImageRequestValidator.CalculateSize("pct:50");
            Assert.NotNull(r);
            Assert.True(r.IsSome);
            foreach (var result in r)
            {
                AssertM.Equal(result.Mode, ImageSizeMode.PercentageScaled);
                Assert.Equal(0f, result.Width);
                Assert.Equal(0f, result.Height);
                Assert.Equal(0.5f, result.Percent);
                Assert.False(result.Upscale);
            }
        }

        [Fact]
        public void CalculateSize_Pct_Valid_Int_Upscale()
        {
            var r = ImageRequestValidator.CalculateSize("^pct:50");
            Assert.NotNull(r);
            Assert.True(r.IsSome);
            foreach (var result in r)
            {
                Assert.Equal(result.Mode, ImageSizeMode.PercentageScaled);
                Assert.Equal(0f, result.Width);
                Assert.Equal(0f, result.Height);
                Assert.Equal(0.5f, result.Percent);
                Assert.True(result.Upscale);
            }
        }

        [Fact]
        public void CalculateSize_Pct_Valid_Int_Upscale_Big()
        {
            var r = ImageRequestValidator.CalculateSize("^pct:110");
            Assert.NotNull(r);
            Assert.True(r.IsSome);
            foreach (var result in r)
            {
                Assert.Equal(result.Mode, ImageSizeMode.PercentageScaled);
                Assert.Equal(0f, result.Width);
                Assert.Equal(0f, result.Height);
                Assert.Equal(1.1f, result.Percent);
                Assert.True(result.Upscale);
            }
        }

        [Fact]
        public void CalculateSize_Pct_Invalid_Int()
        {
            Assert.Throws<FormatException>(() => ImageRequestValidator.CalculateSize("pct:50f"));
        }

        [Fact]
        public void CalculateSize_Pct_Valid_Float()
        {
            var r = ImageRequestValidator.CalculateSize("pct:50.0");
            Assert.NotNull(r);
            Assert.True(r.IsSome);
            foreach (var result in r)
            {
                Assert.Equal(result.Mode, ImageSizeMode.PercentageScaled);
                Assert.Equal(0f, result.Width);
                Assert.Equal(0f, result.Height);
                Assert.Equal(0.5f, result.Percent);
                Assert.False(result.Upscale);
            }
        }

        [Fact]
        public void CalculateSize_Pct_Valid_Big_Float()
        {
            var r = ImageRequestValidator.CalculateSize("pct:50.0000000000000000000000000000000000000000000000000001");
            Assert.NotNull(r);
            Assert.True(r.IsSome);
            foreach (var result in r)
            {
                Assert.Equal(result.Mode, ImageSizeMode.PercentageScaled);
                Assert.Equal(0f, result.Width);
                Assert.Equal(0f, result.Height);
                Assert.Equal(0.5f, result.Percent);
                Assert.False(result.Upscale);
            }
        }

        [Fact]
        public void CalculateSize_Max()
        {
            var r = ImageRequestValidator.CalculateSize("max");
            Assert.NotNull(r);
            Assert.True(r.IsSome);
            foreach (var result in r)
            {
                Assert.Equal(result.Mode, ImageSizeMode.Max);
                Assert.Equal(0f, result.Width);
                Assert.Equal(0f, result.Height);
                Assert.False(result.Upscale);
            }
        }
        [Fact]
        public void CalculateSize_Max_Upscaled()
        {
            var r = ImageRequestValidator.CalculateSize("^max");
            Assert.NotNull(r);
            Assert.True(r.IsSome);
            foreach (var result in r)
            {
                Assert.Equal(result.Mode, ImageSizeMode.Max);
                Assert.Equal(0f, result.Width);
                Assert.Equal(0f, result.Height);
                Assert.True(result.Upscale);
            }
        }
        [Fact]
        public void CalculateSize_Width()
        {
            var r = ImageRequestValidator.CalculateSize("256,");
            Assert.NotNull(r);
            Assert.True(r.IsSome);
            foreach (var result in r)
            {
                Assert.Equal(result.Mode, ImageSizeMode.MaintainAspectRatio);
                Assert.Equal(256, result.Width);
                Assert.Equal(0, result.Height);
                Assert.False(result.Upscale);
            }
        }
        [Fact]
        public void CalculateSize_Width_Upscaled()
        {
            var r = ImageRequestValidator.CalculateSize("^256,");
            Assert.NotNull(r);
            Assert.True(r.IsSome);
            foreach (var result in r)
            {
                Assert.Equal(result.Mode, ImageSizeMode.MaintainAspectRatio);
                Assert.Equal(256, result.Width);
                Assert.Equal(0, result.Height);
                Assert.True(result.Upscale);
            }
        }
        [Fact]
        public void CalculateSize_Height()
        {
            var r = ImageRequestValidator.CalculateSize(",256");
            Assert.NotNull(r);
            Assert.True(r.IsSome);
            foreach (var result in r)
            {
                Assert.Equal(result.Mode, ImageSizeMode.MaintainAspectRatio);
                Assert.Equal(0, result.Width);
                Assert.Equal(256, result.Height);
                Assert.False(result.Upscale);
            }
        }
        [Fact]
        public void CalculateSize_Height_Upscale()
        {
            var r = ImageRequestValidator.CalculateSize("^,256");
            Assert.NotNull(r);
            Assert.True(r.IsSome);
            foreach (var result in r)
            {
                Assert.Equal(result.Mode, ImageSizeMode.MaintainAspectRatio);
                Assert.Equal(0, result.Width);
                Assert.Equal(256, result.Height);
                Assert.True(result.Upscale);
            }
        }

        [Fact]
        public void CalculateSize_Width_Height()
        {
            var r = ImageRequestValidator.CalculateSize("256,512");
            Assert.NotNull(r);
            Assert.True(r.IsSome);
            foreach (var result in r)
            {
                Assert.Equal(result.Mode, ImageSizeMode.Distort);
                Assert.Equal(256, result.Width);
                Assert.Equal(512, result.Height);
                Assert.False(result.Upscale);
            }
        }

        [Fact]
        public void CalculateSize_Width_Height_Upscale()
        {
            var r = ImageRequestValidator.CalculateSize("^256,512");
            Assert.NotNull(r);
            Assert.True(r.IsSome);
            foreach (var result in r)
            {
                Assert.Equal(result.Mode, ImageSizeMode.Distort);
                Assert.Equal(256, result.Width);
                Assert.Equal(512, result.Height);
                Assert.True(result.Upscale);
            }
        }

        [Fact]
        public void CalculateSize_Empty_Width_Height()
        {
            var result = ImageRequestValidator.CalculateSize(",");
            Assert.True(result.IsNone);
        }
        [Fact]
        public void CalculateSize_Best()
        {
            var r = ImageRequestValidator.CalculateSize("!256,100");
            Assert.NotNull(r);
            Assert.True(r.IsSome);
            foreach (var result in r)
            {
                Assert.Equal(result.Mode, ImageSizeMode.MaintainAspectRatio);
                Assert.Equal(256, result.Width);
                Assert.Equal(100, result.Height);
                Assert.False(result.Upscale);
            }

        }
        [Fact]
        public void CalculateSize_Best_Upscale()
        {
            var r = ImageRequestValidator.CalculateSize("^!256,100");
            Assert.NotNull(r);
            Assert.True(r.IsSome);
            foreach (var result in r)
            {
                Assert.Equal(result.Mode, ImageSizeMode.MaintainAspectRatio);
                Assert.Equal(256, result.Width);
                Assert.Equal(100, result.Height);
                Assert.True(result.Upscale);
            }

        }

        [Fact]
        public void CalculateSize_Best_UpscaleInvalid()
        {
            Assert.Throws<FormatException>(() => ImageRequestValidator.CalculateSize("!^256,100"));
        }

        [Fact]
        public void CalculateSize_Invalid()
        {
            Assert.Throws<FormatException>(() => ImageRequestValidator.CalculateSize("!256,a"));
        }
    }
}

using Image.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TremendousIIIF.Common;
using Xunit.Sdk;
using XunitAssertMessages;

namespace TremendousIIIF.Test.Request
{
    public class ImageRequestInterpreterTests
    {
        const int width = 300;
        const int height = 200;

        [Fact]
        [Description("/full/full/0/")]
        public void Full_Full()
        {
            var request = new Image.Common.ImageRequest(new ImageRegion(ImageRegionMode.Full), new ImageSize(ImageSizeMode.Max, 1), new ImageRotation(0, false), ImageQuality.@default, ImageFormat.jpg);
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            
            AssertM.Equal(0, result.StartX, "Expected x offset does not match calculated x offset");
            AssertM.Equal(0, result.StartY, "Expected y offset does not match calculated y offset");
            AssertM.Equal(width, result.RegionWidth, "Expected region width does not match calculated region width");
            AssertM.Equal(height, result.RegionHeight, "Expected region height does not match calculated region height");
        }

        [Fact]
        [Description("/square/full/0/")]
        public void Square_Full()
        {
            var request = new Image.Common.ImageRequest
            (
                new ImageRegion(ImageRegionMode.Square),
                new ImageSize(ImageSizeMode.Max, 1),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            AssertM.Equal(50, result.StartX, "Expected x offset does not match calculated x offset");
            AssertM.Equal(0, result.StartY, "Expected y offset does not match calculated y offset");
            AssertM.Equal(height, result.RegionWidth, "Expected region width does not match calculated region width");
            AssertM.Equal(height, result.RegionHeight, "Expected region height does not match calculated region height");
            AssertM.Equal(height, result.OutputWidth, "Expected width does not match calculated width");
            AssertM.Equal(height, result.OutputHeight, "Expected height does not match calculated width");
        }

        [Fact]
        [Description("/square/100,/0/")]
        public void Square_Width()
        {
            var request = new Image.Common.ImageRequest
            (
                new ImageRegion(ImageRegionMode.Square),
                new ImageSize(ImageSizeMode.MaintainAspectRatio, 1, 0, 100),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            AssertM.Equal(50, result.StartX, "Expected x offset does not match calculated x offset");
            AssertM.Equal(0, result.StartY, "Expected y offset does not match calculated y offset");
            AssertM.Equal(200, result.RegionWidth, "Expected region width does not match calculated region width");
            AssertM.Equal(200, result.RegionHeight, "Expected region height does not match calculated region height");
            AssertM.Equal(100, result.OutputWidth, "Expected width does not match calculated width");
            AssertM.Equal(100, result.OutputHeight, "Expected height does not match calculated width");
        }

        [Fact]
        [Description("/square/,100/0/")]
        public void Square_Height()
        {
            var request = new Image.Common.ImageRequest
            (
                new ImageRegion(ImageRegionMode.Square),
                new ImageSize(ImageSizeMode.MaintainAspectRatio, 1, 0, 100),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.Equal(50, result.StartX);
            Assert.Equal(0, result.StartY);
            Assert.Equal(height, result.RegionWidth);
            Assert.Equal(height, result.RegionHeight);
            Assert.Equal(100, result.OutputWidth);
            Assert.Equal(100, result.OutputHeight);
        }

        [Fact]
        [Description("/square/!105,100/0/")]
        public void Square_Rectangular_Width_Height_Aspect()
        {
            var request = new Image.Common.ImageRequest
            (
                new ImageRegion(ImageRegionMode.Square),
                new ImageSize(ImageSizeMode.MaintainAspectRatio, 1, 105, 100),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            AssertM.Equal(50, result.StartX, "Expected x position does not match calculated x position");
            AssertM.Equal(0, result.StartY, "Expected y position does not match calculated y position");
            AssertM.Equal(height, result.RegionWidth, "Expected region width does not match calculated region width");
            AssertM.Equal(height, result.RegionHeight, "Expected region height does not match calculated region height");
            AssertM.Equal(100, result.OutputWidth, "Expected width does not match calculated width");
            AssertM.Equal(100, result.OutputHeight, "Expected height does not match calculated height");
        }

        [Fact]
        [Description("/square/50,50/0/")]
        public void Square_Width_Height()
        {
            var request = new Image.Common.ImageRequest
            (
                new ImageRegion(ImageRegionMode.Square),
                new ImageSize(ImageSizeMode.Distort, 1, 50, 50),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);

            AssertM.Equal(50, result.StartX, "Expected x position does not match calculated x position");
            AssertM.Equal(0, result.StartY, "Expected y position does not match calculated y position");
            AssertM.Equal(200, result.RegionWidth, "Expected region width does not match calculated region width");
            AssertM.Equal(200, result.RegionHeight, "Expected region height does not match calculated region height");
            AssertM.Equal(50, result.OutputWidth, "Expected width does not match calculated width");
            AssertM.Equal(50, result.OutputHeight, "Expected height does not match calculated height");
        }

        [Fact]
        [Description("/square/150,50/0/")]
        public void Square_Width_Height_Distorted()
        {
            var request = new Image.Common.ImageRequest
            (
                new ImageRegion(ImageRegionMode.Square),
                new ImageSize(ImageSizeMode.Distort, 1, 150, 50),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            AssertM.Equal(50, result.StartX, "Expected x position does not match calculated x position");
            AssertM.Equal(0, result.StartY, "Expected y position does not match calculated y position");
            AssertM.Equal(200, result.RegionWidth, "Expected region width does not match calculated region width");
            AssertM.Equal(200, result.RegionHeight, "Expected region height does not match calculated region height");
            AssertM.Equal(150, result.OutputWidth, "Expected width does not match calculated width");
            AssertM.Equal(50, result.OutputHeight, "Expected height does not match calculated height");
        }

        [Fact]
        [Description("/full/150,/0/")]
        public void Full_Width()
        {
            var request = new Image.Common.ImageRequest
            (
                new ImageRegion(ImageRegionMode.Full),
                new ImageSize(ImageSizeMode.MaintainAspectRatio, 1, 150, 0),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            AssertM.Equal(0, result.StartX, "Expected x position does not match calculated x position");
            AssertM.Equal(0, result.StartY, "Expected y position does not match calculated y position");
            AssertM.Equal(width, result.RegionWidth, "Expected region width does not match calculated region width");
            AssertM.Equal(height, result.RegionHeight, "Expected region height does not match calculated region height");
            AssertM.Equal(150, result.OutputWidth, "Expected width does not match calculated width");
            AssertM.Equal(100, result.OutputHeight, "Expected height does not match calculated height");
        }
        [Fact]
        [Description("/full/,150/0/")]
        public void Full_Height()
        {
            var request = new Image.Common.ImageRequest
            (
                new ImageRegion(ImageRegionMode.Full),
                new ImageSize(ImageSizeMode.MaintainAspectRatio, 1, 0, 150),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            AssertM.Equal(0, result.StartX, "Expected x position does not match calculated x position");
            AssertM.Equal(0, result.StartY, "Expected y position does not match calculated y position");
            AssertM.Equal(width, result.RegionWidth, "Expected region width does not match calculated region width");
            AssertM.Equal(height, result.RegionHeight, "Expected region height does not match calculated region height");
            AssertM.Equal(225, result.OutputWidth, "Expected width does not match calculated width");
            AssertM.Equal(150, result.OutputHeight, "Expected height does not match calculated height");
        }
        [Fact]
        [Description("/full/pct:50/0/")]
        public void Full_Pct()
        {
            var request = new Image.Common.ImageRequest
            (
                new ImageRegion(ImageRegionMode.Full),
                new ImageSize(ImageSizeMode.PercentageScaled, 0.5f),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            AssertM.Equal(0, result.StartX);
            AssertM.Equal(0, result.StartY);
            AssertM.Equal(width, result.RegionWidth);
            AssertM.Equal(height, result.RegionHeight);
            AssertM.Equal(150, result.OutputWidth, "Expected width does not match calculated width");
            AssertM.Equal(100, result.OutputHeight, "Expected height does not match calculated height");
        }
        [Fact]
        [Description("/full/225,100/0/")]
        public void Full_Distort()
        {
            var request = new Image.Common.ImageRequest
            (
                new ImageRegion(ImageRegionMode.Full),
                new ImageSize(ImageSizeMode.Distort, 1, 225, 100),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            AssertM.Equal(0, result.StartX, "Expected x position does not match calculated x position");
            AssertM.Equal(0, result.StartY, "Expected y position does not match calculated y position");
            AssertM.Equal(width, result.RegionWidth, "Expected region width does not match calculated region width");
            AssertM.Equal(height, result.RegionHeight, "Expected region height does not match calculated region height");
            AssertM.Equal(225, result.OutputWidth, "Expected width does not match calculated width");
            AssertM.Equal(100, result.OutputHeight, "Expected height does not match calculated height");
        }
        [Fact]
        [Description("/full/!225,100/0/")]
        public void Full_Scaled()
        {
            var request = new Image.Common.ImageRequest
            (
                new ImageRegion(ImageRegionMode.Full),
                new ImageSize(ImageSizeMode.MaintainAspectRatio, 1, 225, 100),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            AssertM.Equal(0, result.StartX, "Expected x position does not match calculated x position");
            AssertM.Equal(0, result.StartY, "Expected y position does not match calculated y position");
            AssertM.Equal(width, result.RegionWidth, "Expected region width does not match calculated region width");
            AssertM.Equal(height, result.RegionHeight, "Expected region height does not match calculated region height");
            AssertM.Equal(150, result.OutputWidth, "Expected width does not match calculated width");
            AssertM.Equal(100, result.OutputHeight, "Expected height does not match calculated height");
        }
        [Fact]
        [Description("/0,0,1024,1024/full/0/")]
        public void Region_Cropped_to_Image_Boundaries()
        {
            var request = new Image.Common.ImageRequest
            (
                new ImageRegion(ImageRegionMode.Region, 0, 0, 1024, 1024),
                new ImageSize(ImageSizeMode.Full),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Console.Write("heeeeellloooooo");
            AssertM.Equal(0, result.StartX, "Expected x position does not match calculated x position");
            AssertM.Equal(0, result.StartY, "Expected y position does not match calculated y position");
            AssertM.Equal(1024, result.RegionWidth, "Expected region width does not match calculated region width");
            AssertM.Equal(1024, result.RegionHeight, "Expected region height does not match calculated region height");
            AssertM.Equal(1024, result.OutputWidth, "Expected width does not match calculated width");
            AssertM.Equal(1024, result.OutputHeight, "Expected height does not match calculated height");
        }
        [Fact]
        [Description("/full/0,0/0/")]
        public void CheckBounds_Width_Height()
        {
            var request = new Image.Common.ImageRequest
            (
                new ImageRegion(ImageRegionMode.Full),
                new ImageSize(ImageSizeMode.Distort, 1, 0, 0),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );
            Assert.Throws<ArgumentException>(() => ImageRequestInterpreter.GetInterpretedValues(request, width, height, false));
        }

        [Fact]
        [Description("/-1,-1,100,100/100,100/0/")]
        public void CheckBounds_StartX_StartY()
        {
            var request = new Image.Common.ImageRequest
            (
                new ImageRegion(ImageRegionMode.Region, -1, -1, 100, 100),
                new ImageSize(ImageSizeMode.Distort, 1, 100, 100),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );
            Assert.Throws<ArgumentException>(() => ImageRequestInterpreter.GetInterpretedValues(request, width, height, false));
        }

        [Fact]
        [Description("/0,0,0,100/100,100/0/")]
        public void CheckBounds_InvalidRegion()
        {
            var request = new Image.Common.ImageRequest
            (
                new ImageRegion(ImageRegionMode.Region, 0, 0, 0, 100),
                new ImageSize(ImageSizeMode.Distort, 1, 100, 100),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );
            Assert.Throws<ArgumentException>(() => ImageRequestInterpreter.GetInterpretedValues(request, width, height, false));
        }

        [Fact]
        [Description("/600,900,100,100/54,54/0/")]
        public void Region_And_Size()
        {
            var request = new Image.Common.ImageRequest
            (
                new ImageRegion(ImageRegionMode.Region, 600, 900, 100, 100),
                new ImageSize(ImageSizeMode.Distort, 1, 54, 54),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );
            var result = ImageRequestInterpreter.GetInterpretedValues(request, 1000, 1000, false);
            Assert.Equal(600, result.StartX);
            Assert.Equal(900, result.StartY);
            Assert.Equal(100, result.RegionWidth);
            Assert.Equal(100, result.RegionHeight);
            Assert.Equal(54, result.OutputWidth);
            Assert.Equal(54, result.OutputHeight);
            Assert.Equal(0.54f, result.OutputScale);
        }

        [Fact]
        [Description("/full/744,501/0/")]
        public void Full_With_Specific_Size()
        {
            var request = new Image.Common.ImageRequest
            (
                new ImageRegion(ImageRegionMode.Full),
                new ImageSize(ImageSizeMode.Distort, 1, 744, 501),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );
            var result = ImageRequestInterpreter.GetInterpretedValues(request, 1000, 1000, false);
            Assert.Equal(0, result.StartX);
            Assert.Equal(0, result.StartY);
            Assert.Equal(1000, result.RegionWidth);
            Assert.Equal(1000, result.RegionHeight);
            AssertM.Equal(744, result.OutputWidth, "Expected width does not match calculated width");
            AssertM.Equal(501, result.OutputHeight, "Expected height does not match calculated height");
            //AssertM.Equal(0.54f, result.OutputScale);
        }

        [Fact]
        [Description("/full/744,501/90/")]
        public void Full_Rotated_90()
        {
            var request = new Image.Common.ImageRequest
            (
                new ImageRegion(ImageRegionMode.Full),
                new ImageSize(ImageSizeMode.Max, 1, 0, 0),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.Equal(0, result.StartX);
            Assert.Equal(0, result.StartY);
            Assert.Equal(width, result.RegionWidth);
            Assert.Equal(height, result.RegionHeight);
        }

        [Fact]
        [Description("/full/600,400/0/")]
        public void Full_Expanded_No_size_Above_Full()
        {
            var request = new Image.Common.ImageRequest
            (
                new ImageRegion(ImageRegionMode.Full),
                new ImageSize(ImageSizeMode.Distort, 1, width * 2, height * 2),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.Equal(0, result.StartX);
            Assert.Equal(0, result.StartY);
            Assert.Equal(width, result.RegionWidth);
            Assert.Equal(height, result.RegionHeight);
        }

        [Fact]
        public void Full_Expanded_Yes_size_Above_Full()
        {
            var request = new Image.Common.ImageRequest
            (
                new ImageRegion(ImageRegionMode.Full),
                new ImageSize(ImageSizeMode.Distort, 1, width * 2, height * 2),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, true);
            Assert.Equal(0, result.StartX);
            Assert.Equal(0, result.StartY);
            AssertM.Equal(width, result.RegionWidth, "Expected region width does not match calculated region width");
            AssertM.Equal(height, result.RegionHeight, "Expected region height does not match calculated region height");
            AssertM.Equal(width * 2, result.OutputWidth, "Expected width does not match calculated width");
            AssertM.Equal(height * 2, result.OutputHeight, "Expected height does not match calculated height");
        }

        [Fact]
        public void Full_Pct_Expanded_No_size_Above_Full()
        {
            var request = new Image.Common.ImageRequest
            (
                new ImageRegion(ImageRegionMode.Full),
                new ImageSize(ImageSizeMode.PercentageScaled, 2),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.Equal(0, result.StartX);
            Assert.Equal(0, result.StartY);
            Assert.Equal(width, result.RegionWidth);
            Assert.Equal(height, result.RegionHeight);
        }

        [Fact]
        public void Full_Max_MaxWidth()
        {
            var request = new Image.Common.ImageRequest
            (
                new ImageRegion(ImageRegionMode.Full),
                new ImageSize(ImageSizeMode.Max, 1),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg,
                 10,
                10,
                int.MaxValue
            );
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.Equal(0, result.StartX);
            Assert.Equal(0, result.StartY);
            AssertM.Equal(width, result.RegionWidth, "Expected region width does not match calculated region width");
            AssertM.Equal(height, result.RegionHeight, "Expected region height does not match calculated region height");
            AssertM.Equal(10, result.OutputWidth, "Expected region width does not match calculated region width");
            AssertM.Equal(7, result.OutputHeight, "Expected region height does not match calculated region height");
        }

        [Fact]
        [Description("/full/,2000/0/default.jpg maxWidth = 1000, maxHeight = 1000, sizeAboveFull = false")]
        public void Full_Max_MaxWidth_MaxHeight_Exact()
        {
            var request = new Image.Common.ImageRequest(new ImageRegion(ImageRegionMode.Full), new ImageSize(ImageSizeMode.MaintainAspectRatio, 1, 0, 2000), new ImageRotation(0, false), ImageQuality.@default, ImageFormat.jpg, 1000, 1000, int.MaxValue);
            var result = ImageRequestInterpreter.GetInterpretedValues(request, 6640, 4007, false);
            Assert.Equal(0, result.StartX);
            Assert.Equal(0, result.StartY);
            AssertM.Equal(6640, result.RegionWidth, "Expected region width does not match calculated region width");
            AssertM.Equal(4007, result.RegionHeight, "Expected region height does not match calculated region height");
            AssertM.Equal(1000, result.OutputWidth, "Expected region width does not match calculated region width");
            AssertM.Equal(604, result.OutputHeight, "Expected region height does not match calculated region height");
        }

        [Fact]
        public void Full_Pct_Expanded_Yes_size_Above_Full()
        {
            var request = new Image.Common.ImageRequest(new ImageRegion(ImageRegionMode.Full), new ImageSize(ImageSizeMode.PercentageScaled, 2), new ImageRotation(90, false), ImageQuality.@default, ImageFormat.jpg);
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, true);
            Assert.Equal(0, result.StartX);
            Assert.Equal(0, result.StartY);
            AssertM.Equal(width, result.RegionWidth);
            AssertM.Equal(height, result.RegionHeight);
            AssertM.Equal(width * 2, result.OutputWidth, "Expected width does not match calculated width");
            AssertM.Equal(height * 2, result.OutputHeight, "Expected height does not match calculated height");
        }

        [Fact]
        public void Pct_region_full_No_size_Above_Full_maxWidth()
        {
            var request = new Image.Common.ImageRequest(new ImageRegion(ImageRegionMode.PercentageRegion, 0, 0, 100, 100), new ImageSize(ImageSizeMode.Max, 1), new ImageRotation(0, false), ImageQuality.@default, ImageFormat.jpg, 150, int.MaxValue, int.MaxValue);
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.Equal(0, result.StartX);
            Assert.Equal(0, result.StartY);
            AssertM.Equal(width, result.RegionWidth, "Expected width does not match calculated width");
            AssertM.Equal(height, result.RegionHeight, "Expected height does not match calculated height");
            AssertM.Equal(150, result.OutputWidth, "Expected width does not match calculated width");
            AssertM.Equal(100, result.OutputHeight, "Expected height does not match calculated height");
        }

        [Fact]
        public void Full_Expanded_Yes_size_Above_Full_Max_Width()
        {
            var request = new Image.Common.ImageRequest(new ImageRegion(ImageRegionMode.Full), new ImageSize(ImageSizeMode.Distort, 1, width * 2, height * 2), new ImageRotation(90, false), ImageQuality.@default, ImageFormat.jpg, 500, 300, int.MaxValue);
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, true);
            Assert.Equal(0, result.StartX);
            Assert.Equal(0, result.StartY);
            AssertM.Equal(width, result.RegionWidth, "Expected width does not match calculated width");
            AssertM.Equal(height, result.RegionHeight, "Expected height does not match calculated height");
            AssertM.Equal(450, result.OutputWidth, "Expected width does not match calculated width");
            AssertM.Equal(300, result.OutputHeight, "Expected height does not match calculated height");
        }

        [Fact]
        public void Full_Expanded_Yes_size_Above_Full_Max_Height()
        {
            var request = new Image.Common.ImageRequest(new ImageRegion(ImageRegionMode.Full), new ImageSize(ImageSizeMode.Distort, 1, width * 2, height * 4), new ImageRotation(90, false), ImageQuality.@default, ImageFormat.jpg, int.MaxValue, 300, int.MaxValue);
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, true);
            Assert.Equal(0, result.StartX);
            Assert.Equal(0, result.StartY);
            AssertM.Equal(width, result.RegionWidth, "Expected region width does not match calculated width");
            AssertM.Equal(height, result.RegionHeight, "Expected region height does not match calculated height");
            AssertM.Equal(225, result.OutputWidth, "Expected width does not match calculated width");
            AssertM.Equal(300, result.OutputHeight, "Expected height does not match calculated height");
        }


        [Fact]
        public void Full_Expanded_No_size_Above_Full_Max_Width()
        {
            var request = new Image.Common.ImageRequest(new ImageRegion(ImageRegionMode.Full), new ImageSize(ImageSizeMode.MaintainAspectRatio, 1, width, height), new ImageRotation(90, false), ImageQuality.@default, ImageFormat.jpg, 200, int.MaxValue, int.MaxValue);
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.Equal(0, result.StartX);
            Assert.Equal(0, result.StartY);
            AssertM.Equal(width, result.RegionWidth, "Expected width does not match calculated width");
            AssertM.Equal(height, result.RegionHeight, "Expected height does not match calculated height");
            AssertM.Equal(200, result.OutputWidth, "Expected width does not match calculated width");
            AssertM.Equal(133, result.OutputHeight, "Expected height does not match calculated height");
        }

        [Fact]
        public void ExactSizeShouldEqualExactSize()
        {
            var request = new Image.Common.ImageRequest(new ImageRegion(ImageRegionMode.Region, 500, 500, 500, 500), new ImageSize(ImageSizeMode.Distort, 1, 500, 500), new ImageRotation(0, false), ImageQuality.@default, ImageFormat.jpg);
            var result = ImageRequestInterpreter.GetInterpretedValues(request, 2000, 2000, false);
            Assert.Equal(500, result.StartX);
            Assert.Equal(500, result.StartY);
            Assert.Equal(500, result.RegionWidth);
            Assert.Equal(500, result.RegionHeight);
        }

        [Fact]
        public void RegionShouldNotAllowUnsigned()
        {
            var request = new Image.Common.ImageRequest(new ImageRegion(ImageRegionMode.Region, -1, 0, 500, 500), new ImageSize(ImageSizeMode.Max, 1), new ImageRotation(0, false));
            Assert.Throws<ArgumentException>(() => ImageRequestInterpreter.GetInterpretedValues(request, 2000, 2000, false));
        }

        [Fact]
        public void PercentageRegionFullSize()
        {
            var request = new Image.Common.ImageRequest(new ImageRegion(ImageRegionMode.PercentageRegion, 50, 50, 50, 50), new ImageSize(ImageSizeMode.Max, 1), new ImageRotation(0, false));
            var result = ImageRequestInterpreter.GetInterpretedValues(request, 2000, 2000, false);
            Assert.Equal(1000, result.StartX);
            Assert.Equal(1000, result.StartY);
            Assert.Equal(1000, result.RegionWidth);
            Assert.Equal(1000, result.RegionHeight);
        }
    }
}

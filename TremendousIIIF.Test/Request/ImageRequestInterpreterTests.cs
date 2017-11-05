using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Image.Common;
using TremendousIIIF.Common;
using System.Diagnostics.CodeAnalysis;

namespace TremendousIIIF.Test.Request
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    [TestCategory("Request Interpretor")]
    public class ImageRequestInterpreterTests
    {
        const int width = 300;
        const int height = 200;

        [TestMethod]
        [Description("/full/full/0/")]
        public void Full_Full()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion {Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize {Mode = ImageSizeMode.Max, Percent = 1 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(0, result.StartX, "Expected x offset does not match calculated x offset");
            Assert.AreEqual(0, result.StartY, "Expected y offset does not match calculated y offset");
            Assert.AreEqual(width, result.RegionWidth, "Expected region width does not match calculated region width");
            Assert.AreEqual(height, result.RegionHeight, "Expected region height does not match calculated region height");
        }

        [TestMethod]
        [Description("/square/full/0/")]
        public void Square_Full()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Square, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.Max, Percent = 1 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(50, result.StartX, "Expected x offset does not match calculated x offset");
            Assert.AreEqual(0, result.StartY, "Expected y offset does not match calculated y offset");
            Assert.AreEqual(height, result.RegionWidth, "Expected region width does not match calculated region width");
            Assert.AreEqual(height, result.RegionHeight, "Expected region height does not match calculated region height");
            Assert.AreEqual(height, result.OutputWidth, "Expected width does not match calculated width");
            Assert.AreEqual(height, result.OutputHeight, "Expected height does not match calculated width");
        }

        [TestMethod]
        [Description("/square/100,/0/")]
        public void Square_Width()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Square, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.MaintainAspectRatio, Percent = 1, Width = 100 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(50, result.StartX, "Expected x offset does not match calculated x offset");
            Assert.AreEqual(0, result.StartY, "Expected y offset does not match calculated y offset");
            Assert.AreEqual(200, result.RegionWidth, "Expected region width does not match calculated region width");
            Assert.AreEqual(200, result.RegionHeight, "Expected region height does not match calculated region height");
            Assert.AreEqual(100, result.OutputWidth, "Expected width does not match calculated width");
            Assert.AreEqual(100, result.OutputHeight, "Expected height does not match calculated width");
        }

        [TestMethod]
        [Description("/square/,100/0/")]
        public void Square_Height()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Square, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.MaintainAspectRatio, Percent = 1, Height = 100 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(50, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(height, result.RegionWidth);
            Assert.AreEqual(height, result.RegionHeight);
            Assert.AreEqual(100, result.OutputWidth);
            Assert.AreEqual(100, result.OutputHeight);
        }

        [TestMethod]
        [Description("/square/!105,100/0/")]
        public void Square_Rectangular_Width_Height_Aspect()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Square, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.MaintainAspectRatio, Percent = 1, Height = 100, Width=105 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(50, result.StartX, "Expected x position does not match calculated x position");
            Assert.AreEqual(0, result.StartY, "Expected y position does not match calculated y position");
            Assert.AreEqual(height, result.RegionWidth, "Expected region width does not match calculated region width");
            Assert.AreEqual(height, result.RegionHeight, "Expected region height does not match calculated region height");
            Assert.AreEqual(100, result.OutputWidth, "Expected width does not match calculated width");
            Assert.AreEqual(100, result.OutputHeight, "Expected height does not match calculated height");
        }

        [TestMethod]
        [Description("/square/50,50/0/")]
        public void Square_Width_Height()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Square, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.Distort, Percent = 1, Height = 50, Width = 50 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);

            Assert.AreEqual(50, result.StartX, "Expected x position does not match calculated x position");
            Assert.AreEqual(0, result.StartY, "Expected y position does not match calculated y position");
            Assert.AreEqual(200, result.RegionWidth, "Expected region width does not match calculated region width");
            Assert.AreEqual(200, result.RegionHeight, "Expected region height does not match calculated region height");
            Assert.AreEqual(50, result.OutputWidth, "Expected width does not match calculated width");
            Assert.AreEqual(50, result.OutputHeight, "Expected height does not match calculated height");
        }

        [TestMethod]
        [Description("/square/150,50/0/")]
        public void Square_Width_Height_Distorted()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Square, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.Distort, Percent = 1, Height = 50, Width = 150 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(50, result.StartX, "Expected x position does not match calculated x position");
            Assert.AreEqual(0, result.StartY, "Expected y position does not match calculated y position");
            Assert.AreEqual(200, result.RegionWidth, "Expected region width does not match calculated region width");
            Assert.AreEqual(200, result.RegionHeight, "Expected region height does not match calculated region height");
            Assert.AreEqual(150, result.OutputWidth, "Expected width does not match calculated width");
            Assert.AreEqual(50, result.OutputHeight, "Expected height does not match calculated height");
        }

        [TestMethod]
        [Description("/full/150,/0/")]
        public void Full_Width()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.MaintainAspectRatio, Percent = 1, Width = 150 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(0, result.StartX, "Expected x position does not match calculated x position");
            Assert.AreEqual(0, result.StartY, "Expected y position does not match calculated y position");
            Assert.AreEqual(width, result.RegionWidth, "Expected region width does not match calculated region width");
            Assert.AreEqual(height, result.RegionHeight, "Expected region height does not match calculated region height");
            Assert.AreEqual(150, result.OutputWidth, "Expected width does not match calculated width");
            Assert.AreEqual(100, result.OutputHeight, "Expected height does not match calculated height");
        }
        [TestMethod]
        [Description("/full/,150/0/")]
        public void Full_Height()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.MaintainAspectRatio, Percent = 1, Height = 150 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(0, result.StartX, "Expected x position does not match calculated x position");
            Assert.AreEqual(0, result.StartY, "Expected y position does not match calculated y position");
            Assert.AreEqual(width, result.RegionWidth, "Expected region width does not match calculated region width");
            Assert.AreEqual(height, result.RegionHeight, "Expected region height does not match calculated region height");
            Assert.AreEqual(225, result.OutputWidth, "Expected width does not match calculated width");
            Assert.AreEqual(150, result.OutputHeight, "Expected height does not match calculated height");
        }
        [TestMethod]
        [Description("/full/pct:50/0/")]
        public void Full_Pct()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.PercentageScaled, Percent = 0.5f },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(width, result.RegionWidth);
            Assert.AreEqual(height, result.RegionHeight);
            Assert.AreEqual(150, result.OutputWidth, "Expected width does not match calculated width");
            Assert.AreEqual(100, result.OutputHeight, "Expected height does not match calculated height");
        }
        [TestMethod]
        [Description("/full/225,100/0/")]
        public void Full_Distort()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.Distort, Percent = 1, Width=225, Height=100 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(0, result.StartX, "Expected x position does not match calculated x position");
            Assert.AreEqual(0, result.StartY, "Expected y position does not match calculated y position");
            Assert.AreEqual(width, result.RegionWidth, "Expected region width does not match calculated region width");
            Assert.AreEqual(height, result.RegionHeight, "Expected region height does not match calculated region height");
            Assert.AreEqual(225, result.OutputWidth, "Expected width does not match calculated width");
            Assert.AreEqual(100, result.OutputHeight, "Expected height does not match calculated height");
        }
        [TestMethod]
        [Description("/full/!225,100/0/")]
        public void Full_Scaled()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.MaintainAspectRatio, Percent = 1, Width = 225, Height = 100 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(0, result.StartX, "Expected x position does not match calculated x position");
            Assert.AreEqual(0, result.StartY, "Expected y position does not match calculated y position");
            Assert.AreEqual(width, result.RegionWidth, "Expected region width does not match calculated region width");
            Assert.AreEqual(height, result.RegionHeight, "Expected region height does not match calculated region height");
            Assert.AreEqual(150, result.OutputWidth, "Expected width does not match calculated width");
            Assert.AreEqual(100, result.OutputHeight, "Expected height does not match calculated height");
        }
        [TestMethod]
        [Description("/full/0,0/0/")]
        [ExpectedException(typeof(ArgumentException))]
        public void CheckBounds_Width_Height()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.Distort, Percent = 1, Width = 0, Height = 0 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
        }
        [TestMethod]
        [Description("/-1,-1,100,100/100,100/0/")]
        [ExpectedException(typeof(ArgumentException))]
        public void CheckBounds_StartX_StartY()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Region, X = -1, Y = -1, Width = 100, Height = 100 },
                Size = new ImageSize { Mode = ImageSizeMode.Distort, Percent = 1, Width = 100, Height = 100 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
        }
        [TestMethod]
        [Description("/0,0,0,100/100,100/0/")]
        [ExpectedException(typeof(ArgumentException))]
        public void CheckBounds_InvalidRegion()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Region, X = 0, Y = 0, Width=0, Height=100 },
                Size = new ImageSize { Mode = ImageSizeMode.Distort, Percent = 1, Width = 100, Height = 100 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
        }

        [TestMethod]
        [Description("/600,900,100,100/54,54/0/")]
        public void Region_And_Size()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Region, X = 600, Y = 900, Width = 100, Height = 100 },
                Size = new ImageSize { Mode = ImageSizeMode.Distort, Percent = 1, Width = 54, Height = 54 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, 1000, 1000, false);
            Assert.AreEqual(600, result.StartX);
            Assert.AreEqual(900, result.StartY);
            Assert.AreEqual(100, result.RegionWidth);
            Assert.AreEqual(100, result.RegionHeight);
            Assert.AreEqual(54, result.OutputWidth);
            Assert.AreEqual(54, result.OutputHeight);
            Assert.AreEqual(0.54f, result.OutputScale);
        }

        [TestMethod]
        [Description("/full/744,501/0/")]
        public void Full_With_Specific_Size()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full },
                Size = new ImageSize { Mode = ImageSizeMode.Distort, Percent = 1, Width = 744, Height = 501 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, 1000, 1000, false);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(1000, result.RegionWidth);
            Assert.AreEqual(1000, result.RegionHeight);
            Assert.AreEqual(744, result.OutputWidth, "Expected width does not match calculated width");
            Assert.AreEqual(501, result.OutputHeight, "Expected height does not match calculated height");
            //Assert.AreEqual(0.54f, result.OutputScale);
        }

        [TestMethod]
        [Description("/full/744,501/90/")]
        public void Full_Rotated_90()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y= 0 },
                Size = new ImageSize { Mode = ImageSizeMode.Max, Percent = 1 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 90 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(width, result.RegionWidth);
            Assert.AreEqual(height, result.RegionHeight);
        }

        [TestMethod]
        [Description("/full/600,400/0/")]
        public void Full_Expanded_No_size_Above_Full()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.Distort, Percent = 1, Width = width * 2, Height= height * 2 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(width, result.RegionWidth);
            Assert.AreEqual(height, result.RegionHeight);
        }

        [TestMethod]
        public void Full_Expanded_Yes_size_Above_Full()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.Distort, Percent = 1, Width = width * 2, Height = height * 2 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 90 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, true);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(width, result.RegionWidth, "Expected region width does not match calculated region width");
            Assert.AreEqual(height, result.RegionHeight, "Expected region height does not match calculated region height");
            Assert.AreEqual(width * 2, result.OutputWidth, "Expected width does not match calculated width");
            Assert.AreEqual(height * 2, result.OutputHeight, "Expected height does not match calculated height");
        }

        [TestMethod]
        public void Full_Pct_Expanded_No_size_Above_Full()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.PercentageScaled, Percent = 2 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 90 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(width, result.RegionWidth);
            Assert.AreEqual(height, result.RegionHeight);
        }

        [TestMethod]
        public void Full_Max_MaxWidth()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.Max, Percent = 1 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg,
                MaxWidth = 10,
                MaxHeight = 10
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(width, result.RegionWidth, "Expected region width does not match calculated region width");
            Assert.AreEqual(height, result.RegionHeight, "Expected region height does not match calculated region height");
            Assert.AreEqual(10, result.OutputWidth, "Expected region width does not match calculated region width");
            Assert.AreEqual(7, result.OutputHeight, "Expected region height does not match calculated region height");
        }

        [TestMethod]
        [Description("/full/,2000/0/default.jpg maxWidth = 1000, maxHeight = 1000, sizeAboveFull = false")]
        public void Full_Max_MaxWidth_MaxHeight_Exact()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.MaintainAspectRatio, Percent = 1, Height=2000 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg,
                MaxWidth = 1000,
                MaxHeight = 1000
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, 6640, 4007, false);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(6640, result.RegionWidth, "Expected region width does not match calculated region width");
            Assert.AreEqual(4007, result.RegionHeight, "Expected region height does not match calculated region height");
            Assert.AreEqual(1000, result.OutputWidth, "Expected region width does not match calculated region width");
            Assert.AreEqual(604, result.OutputHeight, "Expected region height does not match calculated region height");
        }

        [TestMethod]
        public void Full_Pct_Expanded_Yes_size_Above_Full()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.PercentageScaled, Percent = 2 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 90 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, true);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(width, result.RegionWidth);
            Assert.AreEqual(height, result.RegionHeight);
            Assert.AreEqual(width * 2, result.OutputWidth, "Expected width does not match calculated width");
            Assert.AreEqual(height * 2, result.OutputHeight, "Expected height does not match calculated height");
        }

        [TestMethod]
        public void Pct_region_full_No_size_Above_Full_maxWidth()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.PercentageRegion, X = 0, Y = 0, Width=100, Height=100 },
                Size = new ImageSize { Mode = ImageSizeMode.Max, Percent = 1 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg,
                MaxWidth = 150
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(width, result.RegionWidth, "Expected width does not match calculated width");
            Assert.AreEqual(height, result.RegionHeight, "Expected height does not match calculated height");
            Assert.AreEqual(150, result.OutputWidth, "Expected width does not match calculated width");
            Assert.AreEqual(100, result.OutputHeight, "Expected height does not match calculated height");
        }

        [TestMethod]
        public void Full_Expanded_Yes_size_Above_Full_Max_Width()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.Distort, Percent = 1, Width = width * 2, Height = height * 2 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 90 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg,
                MaxWidth = 500,
                MaxHeight = 300
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, true);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(width, result.RegionWidth, "Expected width does not match calculated width");
            Assert.AreEqual(height, result.RegionHeight, "Expected height does not match calculated height");
            Assert.AreEqual(450, result.OutputWidth, "Expected width does not match calculated width");
            Assert.AreEqual(300, result.OutputHeight, "Expected height does not match calculated height");
        }

        [TestMethod]
        public void Full_Expanded_Yes_size_Above_Full_Max_Height()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.Distort, Percent = 1, Width = width * 2, Height = height * 4 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 90 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg,
                MaxHeight = 300
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, true);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(width, result.RegionWidth, "Expected region width does not match calculated width");
            Assert.AreEqual(height, result.RegionHeight, "Expected region height does not match calculated height");
            Assert.AreEqual(225, result.OutputWidth, "Expected width does not match calculated width");
            Assert.AreEqual(300, result.OutputHeight, "Expected height does not match calculated height");
        }


        [TestMethod]
        public void Full_Expanded_No_size_Above_Full_Max_Width()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.MaintainAspectRatio, Percent = 1, Width = width, Height = height },
                Rotation = new ImageRotation { Mirror = false, Degrees = 90 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg,
                MaxWidth = 200
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(width, result.RegionWidth, "Expected width does not match calculated width");
            Assert.AreEqual(height, result.RegionHeight, "Expected height does not match calculated height");
            Assert.AreEqual(200, result.OutputWidth, "Expected width does not match calculated width");
            Assert.AreEqual(133, result.OutputHeight, "Expected height does not match calculated height");
        }

        [TestMethod]
        public void ExactSizeShouldEqualExactSize()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Region, X = 500, Y = 500, Width=500, Height = 500 },
                Size = new ImageSize { Mode = ImageSizeMode.Distort, Percent = 1, Width = 500, Height = 500 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, 2000, 2000, false);
            Assert.AreEqual(500, result.StartX);
            Assert.AreEqual(500, result.StartY);
            Assert.AreEqual(500, result.RegionWidth);
            Assert.AreEqual(500, result.RegionHeight);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void RegionShouldNotAllowUnsigned()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Region, X = -1, Y = 0, Width = 500, Height = 500 },
                Size = new ImageSize { Mode = ImageSizeMode.Max, Percent = 1 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, 2000, 2000, false);
        }

        [TestMethod]
        public void PercentageRegionFullSize()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.PercentageRegion, X = 50, Y = 50, Width = 50, Height = 50 },
                Size = new ImageSize { Mode = ImageSizeMode.Max, Percent = 1 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, 2000, 2000, false);
            Assert.AreEqual(1000, result.StartX);
            Assert.AreEqual(1000, result.StartY);
            Assert.AreEqual(1000, result.RegionWidth);
            Assert.AreEqual(1000, result.RegionHeight);
        }
    }
}

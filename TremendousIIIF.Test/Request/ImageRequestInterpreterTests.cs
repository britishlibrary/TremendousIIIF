using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Image.Common;
using TremendousIIIF.Common;
using System.Diagnostics.CodeAnalysis;

namespace TremendousIIIF.Test.Request
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class ImageRequestInterpreterTests
    {
        const int width = 300;
        const int height = 200;
        [TestMethod]
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
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(width, result.TileWidth);
            Assert.AreEqual(height, result.TileHeight);
        }

        [TestMethod]
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
            Assert.AreEqual(50, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(height, result.TileWidth);
            Assert.AreEqual(height, result.TileHeight);
        }

        [TestMethod]
        public void Square_Width()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Square, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.SpecifiedFit, Percent = 1, Width = 100 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(50, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(height, result.TileWidth);
            Assert.AreEqual(height, result.TileHeight);
            Assert.AreEqual(100, result.Width);
            Assert.AreEqual(100, result.Height);
        }

        [TestMethod]
        public void Square_Height()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Square, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.SpecifiedFit, Percent = 1, Height = 100 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(50, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(height, result.TileWidth);
            Assert.AreEqual(height, result.TileHeight);
            Assert.AreEqual(100, result.Width);
            Assert.AreEqual(100, result.Height);
        }

        [TestMethod]
        public void Square_WidthHeight()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Square, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.SpecifiedFit, Percent = 1, Height = 100, Width=105 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(50, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(height, result.TileWidth);
            Assert.AreEqual(height, result.TileHeight);
            Assert.AreEqual(105, result.Width);
            Assert.AreEqual(100, result.Height);
        }

        [TestMethod]
        public void Square_Width_Height()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Square, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.Exact, Percent = 1, Height = 50, Width = 50 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(50, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(height, result.TileWidth);
            Assert.AreEqual(height, result.TileHeight);
            Assert.AreEqual(50, result.Width);
            Assert.AreEqual(50, result.Height);
        }

        [TestMethod]
        public void Square_Width_Height_Distorted()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Square, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.Exact, Percent = 1, Height = 50, Width = 150 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(50, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(height, result.TileWidth);
            Assert.AreEqual(height, result.TileHeight);
            Assert.AreEqual(50, result.Width);
            Assert.AreEqual(50, result.Height);
        }

        [TestMethod]
        public void Full_Width()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.Exact, Percent = 1, Width = 150 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(150, result.TileWidth);
            Assert.AreEqual(100, result.TileHeight);
        }
        [TestMethod]
        public void Full_Height()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.Exact, Percent = 1, Height = 150 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(225, result.TileWidth);
            Assert.AreEqual(150, result.TileHeight);
        }
        [TestMethod]
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
            Assert.AreEqual(150, result.TileWidth);
            Assert.AreEqual(100, result.TileHeight);
        }
        [TestMethod]
        public void Full_Exact()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.Exact, Percent = 1, Width=225, Height=100 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(225, result.TileWidth);
            Assert.AreEqual(100, result.TileHeight);
        }
        [TestMethod]
        public void Full_Scaled()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.SpecifiedFit, Percent = 1, Width = 225, Height = 100 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(150, result.TileWidth);
            Assert.AreEqual(100, result.TileHeight);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CheckBounds_Width_Height()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.Exact, Percent = 1, Width = 0, Height = 0 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CheckBounds_StartX_StartY()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Region, X = -1, Y = -1 },
                Size = new ImageSize { Mode = ImageSizeMode.Exact, Percent = 1, Width = 100, Height = 100 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CheckBounds_InvalidRegion()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Region, X = 0, Y = 0, Width=0, Height=100 },
                Size = new ImageSize { Mode = ImageSizeMode.Exact, Percent = 1, Width = 100, Height = 100 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
        }

        [TestMethod]
        public void Region_And_Size()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Region, X = 600, Y = 900, Width = 100, Height = 100 },
                Size = new ImageSize { Mode = ImageSizeMode.Exact, Percent = 1, Width = 54, Height = 54 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, 1000, 1000, false);
            Assert.AreEqual(600, result.StartX);
            Assert.AreEqual(900, result.StartY);
            Assert.AreEqual(100, result.TileWidth);
            Assert.AreEqual(100, result.TileHeight);
            Assert.AreEqual(0.54f, result.OutputScale);
        }

        [TestMethod]
        public void Full_With_Specific_Size()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full },
                Size = new ImageSize { Mode = ImageSizeMode.Exact, Percent = 1, Width = 744, Height = 501 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, 1000, 1000, false);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(744, result.TileWidth);
            Assert.AreEqual(501, result.TileHeight);
            //Assert.AreEqual(0.54f, result.OutputScale);
        }

        [TestMethod]
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
            Assert.AreEqual(width, result.TileWidth);
            Assert.AreEqual(height, result.TileHeight);
        }

        [TestMethod]
        public void Full_Expanded_No_size_Above_Full()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.Exact, Percent = 1, Width = width * 2, Height= height * 2 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(width, result.TileWidth);
            Assert.AreEqual(height, result.TileHeight);
        }

        [TestMethod]
        public void Full_Expanded_Yes_size_Above_Full()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.Exact, Percent = 1, Width = width * 2, Height = height * 2 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 90 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, true);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(width * 2, result.TileWidth);
            Assert.AreEqual(height * 2, result.TileHeight);
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
            Assert.AreEqual(width, result.TileWidth);
            Assert.AreEqual(height, result.TileHeight);
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
            Assert.AreEqual(width * 2, result.TileWidth);
            Assert.AreEqual(height * 2, result.TileHeight);
        }

        [TestMethod]
        public void Full_Expanded_Yes_size_Above_Full_Max_Width()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.Exact, Percent = 1, Width = width * 2, Height = height * 2 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 90 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg,
                MaxWidth = 500,
                MaxHeight = 300
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, true);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(500, result.TileWidth);
            Assert.AreEqual(300, result.TileHeight);
        }

        [TestMethod]
        public void Full_Expanded_Yes_size_Above_Full_Max_Height()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.Exact, Percent = 1, Width = width * 2, Height = height * 4 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 90 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg,
                MaxHeight = 300
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, true);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(600, result.TileWidth);
            Assert.AreEqual(300, result.TileHeight);
        }


        [TestMethod]
        public void Full_Expanded_No_size_Above_Full_Max_Height()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Full, X = 0, Y = 0 },
                Size = new ImageSize { Mode = ImageSizeMode.Exact, Percent = 1, Width = width, Height = height },
                Rotation = new ImageRotation { Mirror = false, Degrees = 90 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg,
                MaxWidth = 200
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, width, height, false);
            Assert.AreEqual(0, result.StartX);
            Assert.AreEqual(0, result.StartY);
            Assert.AreEqual(300, result.TileWidth);
            Assert.AreEqual(200, result.TileHeight);
        }

        [TestMethod]
        public void ExactSizeShouldEqualExactSize()
        {
            var request = new Image.Common.ImageRequest
            {
                ID = "",
                Region = new ImageRegion { Mode = ImageRegionMode.Region, X = 500, Y = 500, Width=500, Height = 500 },
                Size = new ImageSize { Mode = ImageSizeMode.Exact, Percent = 1, Width = 500, Height = 500 },
                Rotation = new ImageRotation { Mirror = false, Degrees = 0 },
                Quality = ImageQuality.@default,
                Format = ImageFormat.jpg
            };
            var result = ImageRequestInterpreter.GetInterpretedValues(request, 2000, 2000, false);
            Assert.AreEqual(500, result.StartX);
            Assert.AreEqual(500, result.StartY);
            Assert.AreEqual(500, result.TileWidth);
            Assert.AreEqual(500, result.TileHeight);
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
            Assert.AreEqual(1000, result.TileWidth);
            Assert.AreEqual(1000, result.TileHeight);
        }
    }
}

using Image.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using TremendousIIIF.Common.Configuration;

namespace TremendousIIIF.Types.v3_1
{
    public class ImageInfo
    {
        [JsonConstructor]
        public ImageInfo()
        {
            Context = "http://iiif.io/api/image/3/context.json";
            Protocol = "http://iiif.io/api/image";
            Profile = "level2";
            Type = "ImageService3";
            ExtraFeatures = new List<string> { "rotationArbitrary", "mirroring", "profileLinkHeader" };
        }

        public ImageInfo(Metadata metadata, ImageServer conf, int maxWidth, int maxHeight, int maxArea) : this()
        {
            Height = metadata.Height;
            Width = metadata.Width;

            var tile = new Tile()
            {
                Width = Math.Min(metadata.TileWidth, maxWidth),
                Height = Math.Min(metadata.TileHeight, maxHeight),
                ScaleFactors = new List<int>()
            };
            for (int i = 0; i < metadata.ScalingLevels; i++)
            {
                tile.ScaleFactors.Add(Convert.ToInt32(Math.Pow(2, i)));
            }
            Tiles = new List<Tile> { tile };

            MaxWidth = maxWidth == int.MaxValue ? default(int) : maxWidth;
            MaxHeight = maxHeight == int.MaxValue ? default(int) : maxHeight;
            MaxArea = maxArea == int.MaxValue ? default(int) : maxArea;

            ExtraFormats = conf.AdditionalOutputFormats.Count == 0 ? null : conf.AdditionalOutputFormats;
            if (conf.AllowSizeAboveFull)
                ExtraFeatures.Add("sizeUpscaling");
        }
        [JsonProperty("@context", Order = 1, Required = Required.Always)]
        public string Context { get; set; }

        [JsonProperty("id", Order = 2, Required = Required.Always)]
        public string ID { get; set; }
        [JsonProperty("type", Order = 3, Required = Required.Always)]
        public string Type { get; set; }


        [JsonProperty("protocol", Order = 4, Required = Required.Always)]
        public string Protocol { get; set; }

        [JsonProperty("profile", Order = 5, Required = Required.Always)]
        public string Profile { get; set; }

        [JsonProperty("width", Order = 6, Required = Required.Always)]
        public int Width { get; set; }

        [JsonProperty("height", Order = 7, Required = Required.Always)]
        public int Height { get; set; }

        [JsonProperty("maxArea", Order = 8, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int MaxArea { get; set; }
        [JsonProperty("maxHeight", Order = 9, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int MaxHeight { get; set; }
        [JsonProperty("maxWidth", Order = 10, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int MaxWidth { get; set; }

        [JsonProperty("extraFormats", Order = 20, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<string> ExtraFormats { get; set; }



        [JsonProperty("extraFeatures", Order = 22, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<string> ExtraFeatures { get; set; }



        [JsonProperty("tiles", Order = 11, NullValueHandling = NullValueHandling.Ignore)]
        public List<Tile> Tiles { get; set; }
    }


    public class Tile
    {

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height", NullValueHandling = NullValueHandling.Ignore)]
        public int Height { get; set; }

        [JsonProperty("scaleFactors")]
        public List<int> ScaleFactors { get; set; }
    }
}
using Image.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using TremendousIIIF.Common.Configuration;
using TremendousIIIF.Common;
using System.Linq;

namespace TremendousIIIF.Types.v3_0
{
    public readonly struct ImageInfo : IImageInfo
    {
        [JsonConstructor]
        public ImageInfo(string id, Metadata metadata, ImageServer conf, int maxWidth, int maxHeight, int maxArea, bool enableGeoService, string manifestId, Uri licenceUri)
        {
            Protocol = "http://iiif.io/api/image";
            Profile = "level2";
            Type = "ImageService3";
            ExtraFeatures = new List<string> { "rotationArbitrary", "mirroring", "profileLinkHeader" };
            // bitonal now optional level 2
            // see https://github.com/IIIF/api/pull/1809
            ExtraQualities = new List<string> { "gray" };
            ID = id;
            Context = ApiVersion.v3_0.GetAttribute<ContextUriAttribute>().ContextUri;
            Height = metadata.Height;
            Width = metadata.Width;

            Sizes = null != metadata.Sizes && metadata.Sizes.Any() ? metadata.Sizes.Select(wh => new Size(wh.Item1, wh.Item2)) : null;
            PartOf = null;

            var tile = new Tile(Math.Min(metadata.TileWidth, maxWidth), Math.Min(metadata.TileHeight, maxHeight), metadata.ScalingLevels);

            Tiles = new List<Tile> { tile };

            MaxWidth = maxWidth == int.MaxValue ? default : maxWidth;
            MaxHeight = maxHeight == int.MaxValue ? default : maxHeight;
            MaxArea = maxArea == int.MaxValue ? default : maxArea;

            ExtraFormats = conf.AdditionalOutputFormats.Count == 0 ? null : conf.AdditionalOutputFormats;
            if (conf.AllowSizeAboveFull)
                ExtraFeatures.Add("sizeUpscaling");
            if (metadata.HasGeoData && enableGeoService)
                ExtraFeatures.Add("GeoJSON");
            if (metadata.Qualities == 3)
            {
                ExtraQualities.Add("color");
            }
            if (conf.AllowBitonal)
            {
                ExtraQualities.Add("bitonal");
            }
            if (!string.IsNullOrEmpty(manifestId))
            {
                PartOf = new List<LinkedObject> { new LinkedObject { Id = manifestId, Type = "Manifest" } };
            }

            Rights = licenceUri;
        }
        [JsonProperty("@context", Order = 1, Required = Required.Always)]
        public string Context { get; }

        [JsonProperty("id", Order = 2, Required = Required.Always)]
        public string ID { get; }
        [JsonProperty("type", Order = 3, Required = Required.Always)]
        public string Type { get; }


        [JsonProperty("protocol", Order = 4, Required = Required.Always)]
        public string Protocol { get; }

        [JsonProperty("profile", Order = 5, Required = Required.Always)]
        public string Profile { get; }

        [JsonProperty("width", Order = 6, Required = Required.Always)]
        public int Width { get; }

        [JsonProperty("height", Order = 7, Required = Required.Always)]
        public int Height { get; }

        [JsonProperty("maxArea", Order = 8, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int MaxArea { get; }
        [JsonProperty("maxHeight", Order = 9, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int MaxHeight { get; }
        [JsonProperty("maxWidth", Order = 10, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int MaxWidth { get; }

        [JsonProperty("sizes", Order = 11, DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<Size> Sizes { get; }

        [JsonProperty("extraFormats", Order = 20, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<string> ExtraFormats { get; }

        [JsonProperty("extraFeatures", Order = 22, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<string> ExtraFeatures { get; }

        [JsonProperty("extraQualities", Order = 23, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<string> ExtraQualities { get; }

        [JsonProperty("partOf", Order = 24, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<LinkedObject> PartOf { get; }

        [JsonProperty("rights", Order = 25, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Uri Rights { get; }

        [JsonProperty("tiles", Order = 11, NullValueHandling = NullValueHandling.Ignore)]
        public List<Tile> Tiles { get; }
    }

    public readonly struct Size
    {
        [JsonProperty("width")]
        public int Width { get; }

        [JsonProperty("height")]
        public int Height { get; }

        public Size(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }


    public readonly struct Tile
    {
        [JsonProperty("type", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Type => "Tile";

        [JsonProperty("width")]
        public int Width { get; }

        [JsonProperty("height", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Height { get; }

        [JsonProperty("scaleFactors")]
        public List<int> ScaleFactors { get; }

        public Tile(int width, int height, int scalingLevels)
        {
            Width = width;
            Height = height;
            ScaleFactors = new List<int>();
            for (int i = 0; i < scalingLevels; i++)
                ScaleFactors.Add(Convert.ToInt32(Math.Pow(2, i)));

        }
    }

    public class LinkedObject
    {
        public string Id { get; set; }
        public string Type { get; set; }
    }
}
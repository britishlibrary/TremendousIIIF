using Newtonsoft.Json;
using System.Collections.Generic;
namespace TremendousIIIF.Types
{
    public class ImageInfo
    {
        [JsonConstructor]
        public ImageInfo()
        {
            Context = "http://iiif.io/api/image/2/context.json";
            Protocol = "http://iiif.io/api/image";
            Profile = new List<object> { "http://iiif.io/api/image/2/level2.json" };
        }
        [JsonProperty("@context", Order = 1, Required = Required.Always)]
        public string Context { get; set; }

        [JsonProperty("@id", Order = 2, Required = Required.Always)]
        public string ID { get; set; }

        [JsonProperty("protocol", Order = 3, Required = Required.Always)]
        public string Protocol { get; set; }

        [JsonProperty("width", Order = 4, Required = Required.Always)]
        public int Width { get; set; }

        [JsonProperty("height", Order = 5, Required = Required.Always)]
        public int Height { get; set; }

        [JsonProperty("profile", Order = 7, Required = Required.Always)]
        public List<object> Profile { get; set; }

        [JsonProperty("tiles", Order = 6, NullValueHandling = NullValueHandling.Ignore)]
        public List<Tile> Tiles { get; set; }
    }

    public class ServiceProfile
    {
        [JsonProperty("maxArea", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int MaxArea { get; set; }
        [JsonProperty("maxHeight", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int MaxHeight { get; set; }
        [JsonProperty("maxWidth", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int MaxWidth { get; set; }
        [JsonProperty("qualities")]
        public List<string> Qualities { get; set; }
        [JsonProperty("supports")]
        public List<string> Support { get; set; }

        public ServiceProfile(bool allowSizeAboveFull = false)
        {
            Qualities = new List<string> { "gray", "color", "bitonal" };
            Support = new List<string> { "rotationArbitrary", "mirroring", "regionSquare", "profileLinkHeader" };

            if (allowSizeAboveFull)
                Support.Add("sizeAboveFull");
        }
    }

    public class Tile
    {
        [JsonProperty("@type")]
        public string Type { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height", NullValueHandling = NullValueHandling.Ignore)]
        public int Height { get; set; }

        [JsonProperty("scaleFactors")]
        public List<int> ScaleFactors { get; set; } = new List<int>();
    }
}
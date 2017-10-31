namespace Image.Common
{
    public class ProcessState
    {
        public ProcessState(string id)
        {
            ID = id;
        }
        public string ID { get; set; }
        public float OutputScale { get; set; }
        public float ImageScale { get; set; }
        public int TileHeight { get; set; }
        public int TileWidth { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}

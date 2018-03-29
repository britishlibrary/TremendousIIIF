namespace Image.Common
{
    public class ProcessState
    {
        public ProcessState()
        {
        }
        /// <summary>
        /// The scale of the output image
        /// </summary>
        public float OutputScale { get; set; }
        /// <summary>
        /// The scale needed to transform <see cref="RegionWidth"/> and <see cref="RegionHeight"/> to <see cref="OutputWidth"/> and <see cref="OutputHeight"/>
        /// </summary>
        public float ImageScale { get; set; }
        /// <summary>
        /// The height (in pixels) of the source region
        /// </summary>
        public int RegionHeight { get; set; }
        /// <summary>
        /// The width (in pixels) of the source region
        /// </summary>
        public int RegionWidth { get; set; }
        /// <summary>
        /// The x offset (in pixels) of the source region
        /// </summary>
        public int StartX { get; set; }
        /// <summary>
        /// The y offset (in pixels) of the source region
        /// </summary>
        public int StartY { get; set; }
        /// <summary>
        /// The width (in pixels) of the output image
        /// </summary>
        public int OutputWidth { get; set; }
        /// <summary>
        /// The height (in pixels) of the output image
        /// </summary>
        public int OutputHeight { get; set; }
    }
}

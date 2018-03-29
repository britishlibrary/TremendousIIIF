namespace Image.Common
{
    public readonly struct ImageRotation
    {
        public ImageRotation(float degrees, bool mirror)
        {
            Degrees = degrees;
            Mirror = mirror;
        }
        public float Degrees { get; }
        public bool Mirror { get; }
    }
}

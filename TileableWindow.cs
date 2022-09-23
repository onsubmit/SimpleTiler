namespace SimpleTiler
{
    using System;
    using System.Drawing;

    public class TileableWindow
    {
        public IntPtr Handle;
        public Point TopLeftCorner;
        public Size Size;

        public override string ToString()
        {
            return $"Window {Handle.ToInt64():X}: {TopLeftCorner} : {Size}";
        }
    }
}

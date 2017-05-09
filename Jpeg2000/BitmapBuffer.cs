using System;
using kdu_mni;
using SkiaSharp;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Jpeg2000
{
    public class BitmapBuffer : Ckdu_compositor_buf
    {
        private SKBitmap bitmap;
        public IntPtr buffer_handle;
        private Ckdu_coords size;

        public BitmapBuffer(Ckdu_coords size)
        {
            size.y = (size.y < 1) ? 1 : size.y;
            size.x = (size.x < 1) ? 1 : size.x;
            this.size = size; // Note: this does not copy dimensions; it just
                              // creates a new reference to the same internal object.  This
                              // is OK, since the `Ckdu_coords' object was created originally
                              // by passing a `kdu_coords' object by value through
                              // `Ckdu_bitmap_compositor::allocate_buffer', which causes
                              // a new managed instance of the object to be instantiated.
            bitmap = new SKBitmap(size.x, size.y);
            bitmap.LockPixels();
            buffer_handle = bitmap.GetPixels();
            this.init(buffer_handle, bitmap.RowBytes / 4);
            this.set_read_accessibility(true);
        }

        /// <summary>
        /// This function unlocks the internal `Bitmap' object and returns it
        /// for use by the application (typically for GID drawing activities).
        /// Be sure to call `release_bitmap' afterwards.  Between these two
        /// calls, you should be doubly sure not to call
        /// `Ckdu_bitmap_compositor.process', since it requires access to a locked
        /// `Bitmap' buffer.
        /// </summary>
        public SKBitmap AcquireBitmap()
        {
            if (buffer_handle != IntPtr.Zero)
                bitmap.UnlockPixels();
            buffer_handle = IntPtr.Zero;
            this.init(IntPtr.Zero, 0);
            return bitmap;
        }

        /// <summary>
        /// Call this function after you have finished with the `Bitmap' object
        /// obtained using `acquire_bitmap'.
        /// </summary>
        public void ReleaseBitmap()
        {
            Debug.Assert(buffer_handle == IntPtr.Zero);
            if (buffer_handle != IntPtr.Zero) return;
            bitmap.LockPixels();
            buffer_handle = bitmap.GetPixels();
            this.init(buffer_handle, bitmap.RowBytes / 4);
        }


        /// <summary>
        /// Implements the logic required for disposing the object's resources
        /// as soon as possible -- typically when the base object's `Dispose'
        /// function is called, but otherwise when the garbage collector calls
        /// the object's finalization code.  This function also unlinks the
        /// object from any list to which it belongs, if necessary.
        /// </summary>
        [HandleProcessCorruptedStateExceptions]
        protected override void Dispose(bool in_dispose)
        {
            if (in_dispose)
            {
                if (buffer_handle != IntPtr.Zero)
                    bitmap.UnlockPixels();
                if (bitmap != null)
                    bitmap.Dispose();
                if (size != null)
                    size.Dispose();
            }
            bitmap = null;
            buffer_handle = IntPtr.Zero;
            size = null;
            init(IntPtr.Zero, 0); // Make sure no attempt is made by internal native
                                  // object to delete the buffer we gave it.
            base.Dispose(in_dispose);
        }
    }
}

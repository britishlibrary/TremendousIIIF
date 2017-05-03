using System;
using kdu_mni;
using SkiaSharp;
using System.Diagnostics;

namespace Jpeg2000
{
    public class BitmapBuffer : Ckdu_compositor_buf
    {
        private SKBitmap bitmap;
        private IntPtr bitmap_data;
        private IntPtr buffer_handle;
        private BitmapBuffer next; // For building linked lists
        private BitmapBuffer prev; // For building doubly linked lists
        private Ckdu_coords size;

        public BitmapBuffer(Ckdu_coords size)
        {
            next = null; prev = null;
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
            bitmap_data = bitmap.GetPixels();
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
            if (bitmap_data != IntPtr.Zero)
                bitmap.UnlockPixels();
            bitmap_data = IntPtr.Zero;
            this.init(IntPtr.Zero, 0);
            return bitmap;
        }

        /// <summary>
        /// Call this function after you have finished with the `Bitmap' object
        /// obtained using `acquire_bitmap'.
        /// </summary>
        public void ReleaseBitmap()
        {
            Debug.Assert(bitmap_data == IntPtr.Zero);
            if (bitmap_data != IntPtr.Zero) return;
            bitmap.LockPixels();
            buffer_handle = bitmap.GetPixels();
            bitmap_data = bitmap.GetPixels();
            this.init(buffer_handle, bitmap.RowBytes / 4);
        }

        /// <summary>
        /// Insert the current object into the list headed by `head', returning
        /// the new head of the list.  Note: this function does not check to
        /// see whether the object already belongs to a list, but assumes it
        /// does not.
        /// </summary>
        public BitmapBuffer Insert(BitmapBuffer head)
        {
            Debug.Assert((next == null) && (prev == null));
            next = head;
            prev = null;
            if (head != null)
                head.prev = this;
            return this;
        }

        /// <summary>
        /// Searches in the list to which `this' belongs for the derived
        /// object whose base is aliased to `tgt', returning the result (or
        /// `null', if none can be found).  Normally, `tgt' will be obtained from
        /// one of the `Ckdu_region_compositor' object's functions which supplies
        /// a compositor buffer, but is necessarily unaware of the lineage of
        /// that buffer as an instance of the derived `Ckdu_bitmap_buf' class.
        /// </summary>
        public BitmapBuffer Find(Ckdu_compositor_buf tgt)
        {
            int tgt_row_gap = 0;
            IntPtr tgt_handle = tgt.get_buf(ref tgt_row_gap, true);
            BitmapBuffer scan;
            for (scan = this; scan != null; scan = scan.prev)
                if (scan.buffer_handle == tgt_handle)
                    return scan;
            for (scan = next; scan != null; scan = scan.next)
                if (scan.buffer_handle == tgt_handle)
                    return scan;
            return null;
        }

        /// <summary>
        /// Unlinks the object from any list to which it belongs, returning
        /// the new head of the list.  This function does not call `Dispose'
        /// itself -- you may well want to do this immediately after unlinking.
        /// </summary>
        public BitmapBuffer Unlink()
        {
            if (next != null)
                next.prev = prev;
            if (prev != null)
                prev.next = next;
            BitmapBuffer result = prev;
            if (result == null)
                result = next;
            else
                while (result.prev != null)
                    result = result.prev;
            prev = next = null;
            return result;
        }

        /// <summary>
        /// Implements the logic required for disposing the object's resources
        /// as soon as possible -- typically when the base object's `Dispose'
        /// function is called, but otherwise when the garbage collector calls
        /// the object's finalization code.  This function also unlinks the
        /// object from any list to which it belongs, if necessary.
        /// </summary>
        protected override void Dispose(bool in_dispose)
        {
            if (in_dispose)
            {
                if ((next != null) || (prev != null))
                    Unlink();
                if (bitmap_data != IntPtr.Zero)
                    bitmap.UnlockPixels();
                if (bitmap != null)
                    bitmap.Dispose();
                if (size != null)
                    size.Dispose();
            }
            next = prev = null;
            bitmap = null;
            bitmap_data = IntPtr.Zero;
            size = null;
            init(IntPtr.Zero, 0); // Make sure no attempt is made by internal native
                                  // object to delete the buffer we gave it.
            base.Dispose(in_dispose);
        }
    }
}

using kdu_mni;
using System.Diagnostics;

namespace Jpeg2000
{
    // TODO: replace this bad linked list implementation with a .net built in one
    public class BitmapCompositor : Ckdu_region_compositor
    {
        private BitmapBuffer buffers; // Points to the head of a list of buffers
        private bool disposed;

        public BitmapCompositor() : base()
        { buffers = null; disposed = false; }

        /// <summary>
        /// Use this function instead of the base object's `get_composition_buffer'
        /// function to access the buffer in its derived form, as the
        /// `Ckdu_bitmap_buf' object which was allocated via `allocate_buffer'.
        /// </summary>
        public BitmapBuffer GetCompositionBitmap(Ckdu_dims region)
        {
            Ckdu_compositor_buf res = get_composition_buffer(region);
            if (res == null)
                return null;
            Debug.Assert(buffers != null);
            return buffers.Find(res);
        }

        /// <summary>
        /// Overrides the base callback function to allocate a derived
        /// `Ckdu_bitmap_buf' object for use in composited image buffering.  This
        /// allows direct access to the composited `Bitmap' data for efficient
        /// rendering.
        /// </summary>
        public override Ckdu_compositor_buf
          allocate_buffer(Ckdu_coords min_size, Ckdu_coords actual_size,
                          bool read_access_required)
        {
            BitmapBuffer result = new BitmapBuffer(min_size);
            actual_size.assign(min_size);
            buffers = result.Insert(buffers);
            return result;
        }

        /// <summary>
        /// Overrides the base callback function to correctly dispose of
        /// buffers which were allocated using the `allocate_buffer' function.
        /// </summary>
        public override void delete_buffer(Ckdu_compositor_buf buf)
        {
            BitmapBuffer equiv = null;
            if (buffers != null)
                equiv = buffers.Find(buf);
            //Debug.Assert(equiv != null);
            if (equiv == null)
                return;
            buffers = equiv.Unlink();
            equiv.Dispose();
        }

        /// <summary>
        /// This override is required to invoke the base object's `pre_destroy'
        /// function, which is required of classes which derive from
        /// `Ckdu_region_compositor' -- see docs.
        /// </summary>
        protected override void Dispose(bool in_dispose)
        {
            if (!disposed)
            {
                disposed = true;
                pre_destroy(); // This call should delete all the buffers
                //Debug.Assert(buffers == null);
            }
            base.Dispose(in_dispose);
        }
    }
}

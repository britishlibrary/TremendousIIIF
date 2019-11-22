using kdu_mni;
using System;
using System.Collections.Generic;

namespace Jpeg2000
{
    public class BitmapCompositor : Ckdu_region_compositor
    {
        private readonly LinkedList<BitmapBuffer> bufferList = new LinkedList<BitmapBuffer>();
        private bool disposed;

        public BitmapCompositor() : base()
        { disposed = false; }

        /// <summary>
        /// Use this function instead of the base object's `get_composition_buffer'
        /// function to access the buffer in its derived form, as the
        /// `Ckdu_bitmap_buf' object which was allocated via `allocate_buffer'.
        /// </summary>
        public BitmapBuffer GetCompositionBitmap(Ckdu_dims region)
        {
            var res = get_composition_buffer(region);
            if (res == null)
                return null;
            return Find(res)?.Value;
        }

        private LinkedListNode<BitmapBuffer> Find(Ckdu_compositor_buf tgt)
        {
            int tgt_row_gap = 0;
            IntPtr tgt_handle = tgt.get_buf(ref tgt_row_gap, true);
            for (var buf = bufferList.First; buf != null; buf = buf.Next)
            {
                if (buf.Value.BufferHandle == tgt_handle)
                {
                    return buf;
                }
            }
            return null;
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
            bufferList.AddFirst(result);
            return result;
        }

        /// <summary>
        /// Overrides the base callback function to correctly dispose of
        /// buffers which were allocated using the `allocate_buffer' function.
        /// </summary>
        public override void delete_buffer(Ckdu_compositor_buf buf)
        {
            if (bufferList.Count > 0)
            {
                var buffer = Find(buf);
                if(buffer == null)
                {
                    return;
                }
                BitmapBuffer equiv = buffer.Value;
                bufferList.Remove(buffer);
                equiv.Dispose();
            }
            
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
            }
            base.Dispose(in_dispose);
        }
    }
}

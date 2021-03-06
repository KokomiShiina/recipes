/*
 Copyright (c) 2010, Sungjin Han <meinside@gmail.com>
 All rights reserved.
 
 Redistribution and use in source and binary forms, with or without
 modification, are permitted provided that the following conditions are met:

  * Redistributions of source code must retain the above copyright notice,
    this list of conditions and the following disclaimer.
  * Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.
  * Neither the name of meinside nor the names of its contributors may be
    used to endorse or promote products derived from this software without
    specific prior written permission.

 THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 POSSIBILITY OF SUCH DAMAGE.
*/

using System;

using Android.Graphics;

namespace Xample.BlurImage
{
    /// <summary>
    ///   This class is a variety of a Gaussian blur. It's slow - use RenderScript if you can.
    /// </summary>
    /// <remarks>Stack Blur Algorithm by Mario Klingemann <mario@quasimondo.com></remarks>
    class StackBlur : IBlurImage
    {
        // This code is a C# port of the Java code which can be found at:
        // http://www.java2s.com/Code/Android/2D-Graphics/Generateablurredbitmapfromgivenone.htm
        //
        // The following code is another example (Java based):
        // http://incubator.quasimondo.com/processing/stackblur.pde

        public Bitmap GetBlurredBitmap(Bitmap original, int radius)
        {
            if (radius < 1)
            {
                throw new ArgumentOutOfRangeException("radius", "Radius must be > =1.");
            }

            int width = original.Width;
            int height = original.Height;
            int wm = width - 1;
            int hm = height - 1;
            int wh = width * height;
            int div = radius + radius + 1;
            int[] r = new int[wh];
            int[] g = new int[wh];
            int[] b = new int[wh];
            int rsum, gsum, bsum, x, y, i;
            int p1, p2;
            int[] vmin = new int[Math.Max(width, height)];
            int[] vmax = new int[Math.Max(width, height)];
            int[] dv = new int[256 * div];
            for (i = 0; i < 256 * div; i++)
            {
                dv[i] = i / div;
            }

            int[] blurredBitmap = new int[wh];
            original.GetPixels(blurredBitmap, 0, width, 0, 0, width, height);
            int yw = 0;
            int yi = 0;

            for (y = 0; y < height; y++)
            {
                rsum = 0;
                gsum = 0;
                bsum = 0;
                for (i = -radius; i <= radius; i++)
                {
                    int p = blurredBitmap[yi + Math.Min(wm, Math.Max(i, 0))];
                    rsum += (p & 0xff0000) >> 16;
                    gsum += (p & 0x00ff00) >> 8;
                    bsum += p & 0x0000ff;
                }
                for (x = 0; x < width; x++)
                {
                    r[yi] = dv[rsum];
                    g[yi] = dv[gsum];
                    b[yi] = dv[bsum];

                    if (y == 0)
                    {
                        vmin[x] = Math.Min(x + radius + 1, wm);
                        vmax[x] = Math.Max(x - radius, 0);
                    }
                    p1 = blurredBitmap[yw + vmin[x]];
                    p2 = blurredBitmap[yw + vmax[x]];

                    rsum += ((p1 & 0xff0000) - (p2 & 0xff0000)) >> 16;
                    gsum += ((p1 & 0x00ff00) - (p2 & 0x00ff00)) >> 8;
                    bsum += (p1 & 0x0000ff) - (p2 & 0x0000ff);
                    yi++;
                }
                yw += width;
            }

            for (x = 0; x < width; x++)
            {
                rsum = gsum = bsum = 0;
                int yp = -radius * width;
                for (i = -radius; i <= radius; i++)
                {
                    yi = Math.Max(0, yp) + x;
                    rsum += r[yi];
                    gsum += g[yi];
                    bsum += b[yi];
                    yp += width;
                }
                yi = x;
                for (y = 0; y < height; y++)
                {
                    blurredBitmap[yi] = (int)(0xff000000 | (dv[rsum] << 16) | (dv[gsum] << 8) | dv[bsum]);
                    if (x == 0)
                    {
                        vmin[y] = Math.Min(y + radius + 1, hm) * width;
                        vmax[y] = Math.Max(y - radius, 0) * width;
                    }
                    p1 = x + vmin[y];
                    p2 = x + vmax[y];

                    rsum += r[p1] - r[p2];
                    gsum += g[p1] - g[p2];
                    bsum += b[p1] - b[p2];

                    yi += width;
                }
            }

            Bitmap.Config config = Bitmap.Config.Rgb565;
            return Bitmap.CreateBitmap(blurredBitmap, width, height, config);
        }
    }
}

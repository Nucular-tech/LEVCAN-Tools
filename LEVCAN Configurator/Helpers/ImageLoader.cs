using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace LEVCAN_Configurator.Helpers
{
    internal class ImageLoader
    {
        //thanks chatgpt for first working piece of code
        public static byte[] ConvertBitmapToRGBA(Bitmap bitmap)
        {
            System.Drawing.Imaging.BitmapData bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            int bytesPerPixel = 4; // RGBA
            int byteCount = bitmapData.Stride * bitmap.Height;
            byte[] pixelData = new byte[byteCount];
            IntPtr ptrFirstPixel = bitmapData.Scan0;

            // Copy the pixel data from the bitmap to the byte array
            System.Runtime.InteropServices.Marshal.Copy(ptrFirstPixel, pixelData, 0, byteCount);

            // Unlock the bitmap data
            bitmap.UnlockBits(bitmapData);

            return pixelData;
        }

        public static IntPtr GetImGUITexture(Bitmap img, GraphicsDevice _gd, ImGuiController _controller)
        {
            var bytelogo = ConvertBitmapToRGBA(img);
            var logoTexture = _gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D((uint)img.Width, (uint)img.Height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
            _gd.UpdateTexture(logoTexture, bytelogo, 0, 0, 0, (uint)img.Width, (uint)img.Height, 1, 0, 0);
            return _controller.GetOrCreateImGuiBinding(_gd.ResourceFactory, logoTexture);
        }
    }
}

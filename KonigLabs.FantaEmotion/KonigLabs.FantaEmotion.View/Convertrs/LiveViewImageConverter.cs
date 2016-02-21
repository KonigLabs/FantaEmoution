﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace KonigLabs.FantaEmotion.View.Convertrs
{
    public class LiveViewImageConverter : IMultiValueConverter
    {
        [HandleProcessCorruptedStateExceptions]
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values.Length < 4)
                    return null;

                var buffer = values[0] as byte[];
                var wb = values[1] as WriteableBitmap;

                var width = 640;

                int.TryParse(values[2].ToString(), out width);

                var height = 480;

                int.TryParse(values[3].ToString(), out height);

                var isNew = false;
                if (wb == null)
                {
                    isNew = true;
                    wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr24, null);
                }
                else
                {
                    if (Math.Abs(wb.Width - width) > 0 || Math.Abs(wb.Height - height) > 0)
                    {
                        wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr24, null);
                    }
                }

                if (buffer == null || buffer.Length < 1)
                {
                    return isNew ? wb : Binding.DoNothing;
                }

                //stream.Seek(0, SeekOrigin.Begin);
                using (var stream = new MemoryStream(buffer))
                {
                    using (var bmp = new Bitmap(stream))
                    {
                        // In my situation, the images are always 640 x 480.
                        var data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                        wb.WritePixels(new Int32Rect(0, 0, width, height), data.Scan0, data.Stride * data.Height, data.Stride);
                        bmp.UnlockBits(data);
                    }
                }

                return isNew ? wb : Binding.DoNothing;
            }
            catch (Exception)
            {
                //Debug.WriteLine("message: {0}; stacktrace: {1}", e.Message, e.StackTrace);
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

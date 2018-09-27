using System.Collections.Generic;
using System.IO;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Drawing.Imaging;

namespace ImageProcessing
{
    public class ProfileGenerator
    {
        private List<string> _BackgroundColours = new List<string> { "339966", "3366CC", "CC33FF", "FF5050" };

        public MemoryStream GenerateProfile(string firstName, string lastName, int width, int height, string color = "")
        {
            var avatarString = string.Empty;

            if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName))
                throw new ArgumentNullException("firstname && lastname");

            if (string.IsNullOrEmpty(lastName))
            {
                if (firstName.Length > 1)
                {
                    avatarString = $"{firstName[0]}{firstName[firstName.Length - 1]}";
                }
                else
                {
                    avatarString = firstName[0].ToString();
                }
            }
            else
            {
                avatarString = string.Format("{0}{1}", firstName[0], lastName[0]);
            }

            avatarString = avatarString.ToUpper();

            var randomIndex = 0;

            if (!string.IsNullOrEmpty(color))
            {
                randomIndex = _BackgroundColours.IndexOf(color);

                if (randomIndex < 0)
                    randomIndex = 0;
            }
            else
            {
                randomIndex = new Random().Next(0, _BackgroundColours.Count - 1);
            }

            var bgColour = _BackgroundColours[randomIndex];
            var bmp = new Bitmap(width, height);
            var sf = new StringFormat();
            var font = new Font("Arial", width / 2, FontStyle.Bold, GraphicsUnit.Pixel);
            var graphics = Graphics.FromImage(bmp);

            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;

            graphics.Clear((Color)new ColorConverter().ConvertFromString("#" + bgColour));
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            graphics.DrawString(
                avatarString, 
                font, 
                new SolidBrush(Color.WhiteSmoke), 
                new RectangleF(0, 0, width, height), 
                sf
            );

            graphics.Flush();

            var ms = new MemoryStream();

            bmp.Save(ms, ImageFormat.Jpeg);

            return ms;
        }
    }
}

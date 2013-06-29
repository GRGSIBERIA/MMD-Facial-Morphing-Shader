using UnityEngine;
using System.Collections;
using System.Drawing;

namespace MMDMorphing
{
	/// <summary>
	/// Texture2Dを各種画像形式に変換する
	/// System.Drawing.DLLは必須
	/// </summary>
	public class Texture2DConverter
	{
		Bitmap bitmap;

		public Texture2DConverter(Texture2D texture)
		{
			bitmap = InstanceBitmap(texture);
		}

		Bitmap InstanceBitmap(Texture2D texture)
		{
			Bitmap bmp = new Bitmap(texture.width, texture.height);
			var pixels = texture.GetPixels(0, 0, texture.width - 1, texture.height - 1);

			for (int line = 0; line < texture.height; line++)
			{
				for (int colum = 0; colum < texture.width; colum++)
				{
					var pixel = pixels[line * texture.height + colum];
					var color = System.Drawing.Color.FromArgb(
						(int)(pixel.a * 255), (int)(pixel.r * 255), (int)(pixel.g * 255), (int)(pixel.b * 255));
					bmp.SetPixel(colum, line, color);
				}
			}
			return bmp;
		}

		void SavePng(string path)
		{
			bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
		}

		void SaveBmp(string path)
		{
			bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Bmp);
		}
	}
}
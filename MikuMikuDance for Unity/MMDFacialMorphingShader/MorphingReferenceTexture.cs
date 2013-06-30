using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace MMDMorphing
{
	/// <summary>
	/// モーフテクスチャのどこを参照しているかUVで表すためのクラス
	/// </summary>
	public class MorphingReferenceTexture
	{
		List<MMDSkinsScript> skins;
		MMDSkinsScript base_skin;

		public MorphingReferenceTexture(MMDSkinsScript base_skin, List<MMDSkinsScript> excluded_base_skins)
		{
			this.base_skin = base_skin;
			this.skins = excluded_base_skins;
		}

		public MorphTexture[] BakeTextures(int texture_size)
		{
			MorphTexture[] textures = new MorphTexture[skins.Count];
			for (int i = 0; i < skins.Count; i++)
			{
				textures[i] = BakeTexture(skins[i], texture_size);
			}
			return textures;
		}

		class MorphColor
		{
			public Color morph;
			public Color magnitude;

			public MorphColor(Vector3 target)
			{
				Vector3 morph = target;
				float m = morph.magnitude;
				morph.Normalize();
				this.morph = new Color(morph.x, morph.y, morph.z);	// rgb = xyz
				var bits = BitConverter.GetBytes(m);
				float buf = 1f / 255f;
				magnitude = new Color(buf * bits[0], buf * bits[1], buf * bits[2], buf * bits[3]);	// rgba = 0123
			}
		}

		public class MorphTexture
		{
			public string name;
			public Texture2D morph;
			public Texture2D magnitude;

			public MorphTexture(string name, int texture_size)
			{
				this.name = name;
				morph = new Texture2D(texture_size, texture_size, TextureFormat.RGB24, false);
				magnitude = new Texture2D(texture_size, texture_size, TextureFormat.RGBA32, false);
			}
		}

		class MorphColors
		{
			public Color[] morph;
			public Color[] magnitude;

			public MorphColors(int texture_size)
			{
				morph = PrepareColors(texture_size);
				magnitude = PrepareColors(texture_size);
			}
		}

		MorphTexture BakeTexture(MMDSkinsScript skin, int texture_size)
		{
			MorphTexture texture = new MorphTexture(skin.gameObject.name, texture_size);
			MorphColors colors = ConvertSkinToMorphColors(skin, texture_size);

			texture.morph.SetPixels(colors.morph);
			texture.magnitude.SetPixels(colors.magnitude);
			texture.morph.name = skin.gameObject.name + "_morph";
			texture.magnitude.name = skin.gameObject.name + "_magnitude";
			return texture;
		}

		MorphColors ConvertSkinToMorphColors(MMDSkinsScript skin, int texture_size)
		{
			MorphColors colors = new MorphColors(texture_size);
			for (int i = 0; i < skin.targetIndices.Length; i++)
			{
				int index = skin.targetIndices[i];
				int base_index = base_skin.targetIndices[index];

				MorphColor m = new MorphColor(skin.morphTarget[i]);
				colors.morph[base_index] = m.morph;
				colors.magnitude[base_index] = m.magnitude;
			}
			return colors;
		}

		static Color[] PrepareColors(int texture_size)
		{
			Color[] colors = new Color[texture_size * texture_size];
			for (int i = 0; i < colors.Length; i++)
				colors[i] = new Color(0, 0, 0);
			return colors;
		}
	}
}
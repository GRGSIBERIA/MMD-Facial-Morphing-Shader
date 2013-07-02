using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using MMD.PMD;


namespace MMDMorphing
{
	/// <summary>
	/// モーフテクスチャのどこを参照しているかUVで表すためのクラス
	/// </summary>
	public class MorphingReferenceTexture
	{
		
		List<PMDFormat.SkinData> skins;
		PMDFormat.SkinData base_skin;

		public MorphingReferenceTexture(PMDFormat.SkinData base_skin, List<PMDFormat.SkinData> excluded_base_skins)
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

		MorphTexture BakeTexture(PMDFormat.SkinData skin, int texture_size)
		{
			MorphTexture texture = new MorphTexture(skin.skin_name, texture_size);
			MorphColors colors = ConvertSkinToMorphColors(skin, texture_size);

			texture.morph.SetPixels(colors.morph);
			texture.magnitude.SetPixels(colors.magnitude);
			texture.morph.name = skin.skin_name + "_morph";
			texture.magnitude.name = skin.skin_name + "_magnitude";
			return texture;
		}

		MorphColors ConvertSkinToMorphColors(PMDFormat.SkinData skin, int texture_size)
		{
			MorphColors colors = new MorphColors(texture_size);
			for (int i = 0; i < skin.skin_vert_count; i++)
			{
				int index = (int)skin.skin_vert_data[i].skin_vert_index;
				int base_index = (int)base_skin.skin_vert_data[index].skin_vert_index;

				MorphColor m = new MorphColor(skin.skin_vert_data[i].skin_vert_pos);
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
#endif
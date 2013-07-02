using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;


namespace MMDMorphing
{
	public class MaterialMaker
	{
		MorphingReferenceTexture.MorphTexture[] textures;

		public MaterialMaker(MorphingReferenceTexture.MorphTexture[] textures)
		{
			this.textures = textures;
		}

		public Material SaveMaterial(string material_path, Material material)
		{
			SetTextures(material);
			AssetDatabase.CreateAsset(material, material_path);
			return material;
		}

		void SetTextures(Material material)
		{
			for (int i = 0; i < textures.Length; i++)
			{
				material.SetTexture("_MorphingMap" + i.ToString(), textures[i].morph);
				material.SetTexture("_MagnitudeMap" + i.ToString(), textures[i].magnitude);
			}
		}
	}
}
#endif
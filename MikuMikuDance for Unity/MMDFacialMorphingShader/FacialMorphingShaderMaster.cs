using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MMDMorphing
{
	public class FacialMorphingShaderMaster
	{
		GameObject pmd_asset;
		List<MMDSkinsScript> skins;
		MMDSkinsScript base_skin;
		string morphing_folder;

		public FacialMorphingShaderMaster(GameObject pmd_asset, GameObject[] expression_children)
		{
			this.pmd_asset = pmd_asset;
			string asset_name = pmd_asset.name;
			string path = AssetDatabase.GetAssetPath(pmd_asset);
			morphing_folder = AssetDatabase.CreateFolder(path, "Morphing");
			skins = GatherScriptsFromGameObjects(expression_children);
			base_skin = ExtractBaseAsExcludeBaseFromExpression(skins);
		}

		public void MakeMorphingShader(GameObject[] expression_children)
		{
			var textures = BakingTexture(pmd_asset);
			var textures_instances = MakeTextures(morphing_folder, textures);	// Materialにアサインするために利用
		}

		Dictionary<string, Texture2D> MakeTextures(string morphing_folder, MorphingReferenceTexture.MorphTexture[] textures)
		{
			Dictionary<string, Texture2D> texture_pathes = new Dictionary<string, Texture2D>();
			for (int i = 0; i < textures.Length; i++)
			{
				string morph_path = CombinePNGPathes(textures[i], "morph_");
				string magnitude_path = CombinePNGPathes(textures[i], "magnitude_");
				
				texture_pathes["morph_" + textures[i].name] = BurnTextureToFolder(textures[i].morph, morph_path);
				texture_pathes["magnitude_" + textures[i].name] = BurnTextureToFolder(textures[i].magnitude, magnitude_path);
			}
			return texture_pathes;
		}

		string CombinePNGPathes(MorphingReferenceTexture.MorphTexture texture, string prefix)
		{
			return morphing_folder + "/" + prefix + texture.name + ".png";
		}

		Texture2D BurnTextureToFolder(Texture2D texture, string full_path)
		{
			Texture2DConverter converter = new Texture2DConverter(texture);
			return converter.SavePng(full_path);
		}

		MorphingReferenceTexture.MorphTexture[] BakingTexture(GameObject pmd_asset)
		{
			MorphingReferenceTexture morphing_reference = new MorphingReferenceTexture(base_skin, skins);
			int texture_size = ExtractTextureSize(pmd_asset);
			return morphing_reference.BakeTextures(texture_size);
		}

		int ExtractTextureSize(GameObject pmd_asset)
		{
			int vtx_count = pmd_asset.GetComponent<SkinnedMeshRenderer>().sharedMesh.vertexCount;
			float square = Mathf.Sqrt(vtx_count);
			int bit_digit = (int)Math.Log(square, 2) + 1;
			return (int)Mathf.Pow(2f, bit_digit);	// 2のべき乗サイズのテクスチャ
		}

		List<MMDSkinsScript> GatherScriptsFromGameObjects(GameObject[] children)
		{
			MMDSkinsScript[] scripts = new MMDSkinsScript[children.Length - 1];
			for (int i = 0; i < scripts.Length; i++)
			{
				scripts[i] = children[i].GetComponent<MMDSkinsScript>();
			}
			return new List<MMDSkinsScript>(scripts);
		}

		MMDSkinsScript ExtractBaseAsExcludeBaseFromExpression(List<MMDSkinsScript> scripts)
		{
			int index = scripts.FindIndex(s => s.gameObject.name == "base");
			MMDSkinsScript base_expression = scripts[index];
			scripts.RemoveAt(index);
			return base_expression;
		}
	}
}
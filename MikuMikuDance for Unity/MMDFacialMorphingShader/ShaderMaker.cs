using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;


namespace MMDMorphing
{
	/// <summary>
	/// シェーダの自動生成をするためのクラス
	/// </summary>
	public class ShaderMaker
	{
		MorphingReferenceTexture.MorphTexture[] textures;

		public ShaderMaker(MorphingReferenceTexture.MorphTexture[] textures)
		{
			this.textures = textures;
		}

		public void SaveShader(string path, string name)
		{
			string full_path = path[path.Length - 1] == '/' ? path + name : path + "/" + name + ".shader";
			string properties = MakeProperties();
			string summation = MakeSummation();
			string shader_text = ShaderText.GetLiteral();

			shader_text = shader_text.Replace("%properties%", properties);
			shader_text = shader_text.Replace("%summate%", summation);
			shader_text = shader_text.Replace("%name%", name);
		}

		string MakeSummation()
		{
			string summation = "        ";	// インデントマシマシ
			summation += textures.Length > 0 ? ReplaceExpression(0) : "";
			
			for (int i = 1; i < textures.Length; i++)
			{
				summation += ReplaceExpression(i);
			}

			return "";
		}

		string ReplaceExpression(int number)
		{
			const string expression = "tex2D(_MorphingMap%number%, u, v) * dot(tex2D(_MagnitudeMap%number%, u, v).xyzw, bitShifts) * _Weight%number%";
			return expression.Replace("%number%", number.ToString());
		}

		string MakeProperties()
		{
			
			const string magnitude =	"    _MagnitudeMap%number% (\"Magnitude Map %name%\", 2D) = \"black\" {}\n";
			const string weight =		"    _Weight%number% (\"Weight %name%\", Range(0, 1)) = 0\"\n";
			const string morphing =		"    _MorphingMap%number% (\"Morphing Map %name%\", 2D) = \"black\" {} \n";

			string some_properties = "";
			some_properties += MakePropertyLines(weight);
			some_properties += MakePropertyLines(morphing);
			some_properties += MakePropertyLines(magnitude);
			return some_properties;
		}

		string MakePropertyLines(string property)
		{
			string properties = "";
			for (int i = 0; i < textures.Length; i++)
			{
				properties += ReplaceProperty(property, i, textures[i].name);
			}
			return properties;
		}

		string ReplaceProperty(string property, int num, string name)
		{
			return property.Replace("%number%", num.ToString()).Replace("%name%", name);
		}
	}
}
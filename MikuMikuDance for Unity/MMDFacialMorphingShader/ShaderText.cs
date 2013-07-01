using UnityEngine;
using System.Collections;
using System;

namespace MMDMorphing
{
	public class ShaderText
	{
		static public string GetLiteral()
		{
			return @"
Shader ""MMD/Morph/%name%"" {
  Properties {
    _MorphingMapSize (""Morphing Map Size"", int) = 512

    %properties%
  }
  SubShader {
    Pass {
      CGPROGRAM
      #pragma vertex SkinnedMesh
      #pragma frag fragment
      #include ""UnityCG.cginc""

      struct v2f {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
      };

      // �̐S�̕\��̕���
      v2f SkinnedMesh(appdata_base v) {
        v2f output;

        float v = 1.0 / int(v.pos[3] / _MorphingMapSize);
        float u = 1.0 / int(v.pos[3] % _MorphingMapSize);

        output.pos = v.pos + SummateMorphVertex(u, v);
        output.uv = v.uv;
        return output;
      }

      float4 SummateMorphVertex(float u, float v) {
        // RGBA����float�^�ɕϊ����邽�߂̂��܂��Ȃ�
        const float4 bitShifts = float4(1.0, 1/255.0, 1/65025.0, 1/160581375.0);
        return %summate%;

        /*
        �����@xy������������āCz�������v�Z���ċ��߂���@
        r^2 = x^2 + y^2 + z^2
        z^2 = r^2 - x^2 - y^2
        sqrt(z^2) = sqrt(r^2 - x^2 - y^2)
        */

      }

    // �t���O�����g�C�s�N�Z���V�F�[�_�̂悤�Ȃ���
    half4 fragment(v2f i) : COLOR {
      return half(i.color, 1)
    }

    ENDCG
    }
  }

  FallBack ""Diffuse""
}
";
		}
	}
}
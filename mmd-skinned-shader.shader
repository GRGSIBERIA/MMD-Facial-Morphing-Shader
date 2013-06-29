Shader "MMD-Skinned-Shader" {
  Properties {
  	_MorphingMapSize ("Morphing Map Size", int) = 512

    // 表情の数だけある
    _MorphingMap<number> ("Morphing Map <number>", 2D) = "black" {} 
    _MagnitudeMap<number> ("Magnitude Map <number>", 2D) = "black" {}
    _Weight<number> ("Weight <number>", Range(0, 1)) = 0
  }
  SubShader {
    Pass {
    	CGPROGRAM
    	#pragma vertex SkinnedMesh
    	#pragma frag fragment
    	#include "UnityCG.cginc"

    	struct v2f {
    		float4 pos : SV_POSITION;
    		float2 uv : TEXCOORD0;
    	};

    	// 肝心の表情の部分
    	v2f SkinnedMesh(appdata_base v) {
    		v2f output;
    		
        float v = 1.0 / int(v.pos[3] / _MorphingMapSize);
        float u = 1.0 / int(v.pos[3] % _MorphingMapSize);

        output.pos = v.pos + SummateMorphVertex(u, v);
        output.uv = v.uv;
        return output;
    	}

      float4 SummateMorphVertex(float u, float v) {
        return 
          tex2D(_MorphingMap<number>, u, v) * tex2D(_MagnitudeMap<number>, u, v) * _Weight<number> +
          tex2D(_MorphingMap<skinCount-1>, u, v) * float(tex2D(_MagnitudeMap<skinCount-1>, u, v)) * _Weight<skinCount-1>;
      }

    	// フラグメント，ピクセルシェーダのようなもの
    	half4 fragment(v2f i) : COLOR {
    		return half(i.color, 1)
    	}

    	ENDCG
    }
  }

  FallBack "Diffuse"
}
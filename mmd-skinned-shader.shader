Shader "MMD-Skinned-Shader" {
  Properties {
  	_MorphingMapSize ("Morphing Map Size", int) = 512
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
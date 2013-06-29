Shader "MMD-Skinned-Shader" {
  Properties {
  	_UtilizedMapSize ("Utilized Map Size", int) = 512
    _UtilizedMap ("Utilized Map", 2D) = "white" {}
    %for number in textureLength do
    	_SkinnedMap<number> ("Skinned Map <number>", 2D) = "black" {}
    	_Weight<number> ("Weight <number>", float) = 0.0
    %end
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
    		int index = int(output.pos[3]);	// できるかな？
    		float2 utilized_uv = CalcUtilizedUVCoordinate(index);

    		if (CheckToUtilizeSkin(utilized_uv)) {
    			v2f.pos = v.pos + SummateSkinnedVector(utilized_uv);
          v2f.uv = v.uv;
    		}
    	}

      bool CheckToUtilizeSkin(float4 utilized_uv) {
        return CalcReferencedIndex(utilized_uv) != 0xFFFFFFFF ? true : false;
      }

      // 全ての表情のベクトルを足し合わせる
      float4 SummateSkinnedVector(float2 utilized_uv) {
        return
          %if textureLength > 0
            %for number in 0..textureLength-2
              tex2D(_SkinnedMap<number>, utilized_uv) * tex2D(_WeightMap<number>, utilized_uv) * _Weight<number> + 
            %end
            tex2D(_SkinnedMap<textureLength-1>, utilized_uv) * tex2D(_WeightMap<textureLength-1>, utilized_uv) * _Weight<textureLength-1>;
          %else
            float4(0);
          %end
      }

    	// ちゃんと参照してるかチェックする
    	int CalcReferencedIndex(float2 utilized_uv) {
    		float4 referenced_index = tex2D(_UtilizedMap, utilized_uv);

    		// フラグが全部立ってたらエンディアンとかは気にしなくても大丈夫だと思う
    		return int(referenced_index[0]) +
    					 int(referenced_index[1]) << 8 +
    					 int(referenced_index[2]) << 16 +
    					 int(referenced_index[3]) << 24;
    	}

    	// indexからある頂点の参照用マップのUV座標を求める
    	float2 CalcUtilizedUVCoordinate(int index) {
    		return float2(
    			1.0 / _UtilizedMapSize * index,
    			1.0 / _UtilizedMapSize * (index % _UtilizedMapSize));
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
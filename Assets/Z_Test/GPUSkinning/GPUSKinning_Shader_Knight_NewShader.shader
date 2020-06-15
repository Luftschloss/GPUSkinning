Shader "GPUSkinning/GPUSkinning_Unlit_Knight_NewShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _ShadowColor("ShadowColor", Color) = (0,0,0,1)
        _LightDir("LightDirection", Vector) = (0,90,0,0)
        _ShadowFalloff("ShadowFalloff", Range(0, 1)) = 0

        [KeywordEnum(BONE1, BONE2, BONE4)]_QUALITY("Skinning Quality", float) = 3
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		Pass
		{
			CGPROGRAM
			#pragma vertex vert addshadow
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile ROOTON_BLENDOFF ROOTON_BLENDON_CROSSFADEROOTON ROOTON_BLENDON_CROSSFADEROOTOFF ROOTOFF_BLENDOFF ROOTOFF_BLENDON_CROSSFADEROOTON ROOTOFF_BLENDON_CROSSFADEROOTOFF

			#include "UnityCG.cginc"
			#include "Assets/GPUSkinning/Resources/GPUSkinningInclude.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 uv2 : TEXCOORD1;
				float4 uv3 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			v2f vert(appdata v)
			{
				UNITY_SETUP_INSTANCE_ID(v);

				v2f o;

				float4 pos = skin4(v.vertex, v.uv2, v.uv3);

				o.vertex = UnityObjectToClipPos(pos);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				return col;
			}
			ENDCG
		}

		UsePass "GPUSkinning/GPUSkinningPlaneShadow/PlaneShadow"
	}
}

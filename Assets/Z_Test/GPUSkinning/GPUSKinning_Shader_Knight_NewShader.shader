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
			#pragma vertex vert
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

		Pass
		{
			Name "ShadowTest"

            Stencil
            {
                Ref 0
                Comp equal
                Pass incrWrap
                Fail keep
                ZFail keep
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Offset -1, 0

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _QUALITY_BONE1 _QUALITY_BONE2 _QUALITY_BONE4
            #pragma multi_compile ROOTON_BLENDOFF ROOTON_BLENDON_CROSSFADEROOTON ROOTON_BLENDON_CROSSFADEROOTOFF ROOTOFF_BLENDOFF ROOTOFF_BLENDON_CROSSFADEROOTON ROOTOFF_BLENDON_CROSSFADEROOTOFF

            #include "UnityCG.cginc"
            #include "Assets/GPUSkinning/Resources/GPUSkinningInclude.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                //阴影计算的Pass不需要通过UV来采样贴图，所以不需要
                //float2 uv : TEXCOORD0;
                //TEXCOORD1和TEXCOORD2需要通过skin2方法来解析贴图中存放的数据信息，顺序必须对应
                float4 uv2 : TEXCOORD1;
                float4 uv3 : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                //float2 uv : TEXCOORD0;
            };

            float4 _LightDir;
            float4 _ShadowColor;
            fixed _ShadowFalloff;

            float3 ShadowProjectPos(float4 vertPos)
            {
                float3 shadowPos;
                float3 worldPos = mul(unity_ObjectToWorld, vertPos).xyz;
                float3 lightDir = normalize(_LightDir.xyz);
                shadowPos.y = min(worldPos.y, _LightDir.w);
                shadowPos.xz = worldPos.xz - lightDir.xz * max(0, worldPos.y - _LightDir.w) / lightDir.y;
                return shadowPos;
            }

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                //通过skin得到顶点在物体自身坐标系的位置信息
                float4 position;

                #if _QUALITY_BONE1
                position = skin1(v.vertex, v.uv2, v.uv3);
                #elif _QUALITY_BONE2
                position = skin2(v.vertex, v.uv2, v.uv3);
                #elif _QUALITY_BONE4
                position = skin4(v.vertex, v.uv2, v.uv3);
                #endif

                float3 shadowPos = ShadowProjectPos(position);
                o.pos = UnityWorldToClipPos(shadowPos);

                float3 center = float3(unity_ObjectToWorld[0].w, _LightDir.w, unity_ObjectToWorld[2].w);
                fixed falloff = 1 - saturate(distance(shadowPos, center) * _ShadowFalloff);

                o.color = _ShadowColor;
                o.color.a *= falloff;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
		}
	}
}

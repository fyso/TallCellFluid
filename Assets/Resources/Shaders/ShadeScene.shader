Shader "Custom/ShadeScene"
{
	Properties
	{
		_BaseColor("Base Color", Color) = (0.5, 0.5, 0.5)
	}

	SubShader
	{
		Pass
		{
			Tags
			{
				"LightMode" = "Diffuse"
			}
			HLSLPROGRAM
			#pragma vertex DiffuseVert
			#pragma fragment DiffuseFrag

			#include "../ShaderLibrary/Common.hlsl"

			struct Attributes
			{
				float3 positionOS : POSITION;
				float3 normalOS : NORMAL;
			};

			struct Varyings
			{
				float3 normalWS: NORMAL;
				float4 positionCS : SV_POSITION;
			};

			Varyings DiffuseVert(Attributes input)
			{
				Varyings output;
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				output.positionCS = TransformObjectToHClip(input.positionOS);

				return output;
			}

			float3 _BaseColor;
			float4 DiffuseFrag(Varyings input) : SV_Target
			{
				float NdotL = dot(normalize(input.normalWS), _WorldSpaceLightDir0.xyz);
				return float4(_BaseColor * _LightColor0.rgb * (NdotL * 0.5 + 0.5), 1);
			}
			ENDHLSL
		}

	}

}

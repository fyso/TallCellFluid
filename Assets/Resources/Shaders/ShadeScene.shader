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

		Pass
		{
			Name "SceneHit"

			HLSLPROGRAM

			#pragma raytracing test

			#include "../ShaderLibrary/Common.hlsl"
			#include "../ShaderLibrary/RayTracing.hlsl"
			float3 _BaseColor;

			[shader("closesthit")]
			void ClosestHitShader(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
			{
				Vertex intersectionVertex = GetIntersectionVertex(attributeData.barycentrics);
				float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - intersectionVertex.positionWS);
				rayIntersection.positionWS = intersectionVertex.positionWS.xyz;

				float NdotL = dot(normalize(intersectionVertex.normalWS), _WorldSpaceLightDir0.xyz);
				rayIntersection.color = _BaseColor * _LightColor0.rgb * (NdotL * 0.5 + 0.5);
			}

			ENDHLSL
		}
	}

}

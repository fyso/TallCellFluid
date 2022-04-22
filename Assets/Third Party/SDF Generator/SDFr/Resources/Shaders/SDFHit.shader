Shader "SDF/SDFHit"
{
    Properties
    {

    }
    SubShader
    {
		Pass
		{
			Name "SDFHit"

			HLSLPROGRAM

			#pragma raytracing test
			#include "../../../../../Resources/ShaderLibrary/Common.hlsl"
			#include "../../../../../Resources/ShaderLibrary/RayTracing.hlsl"

			[shader("closesthit")]
			void ClosestHitShader(inout SDFRayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
			{
				rayIntersection.positionWS = HitWorldPosition();
				rayIntersection.hitKind = HitKind();
			}

			ENDHLSL
		}
    }
}

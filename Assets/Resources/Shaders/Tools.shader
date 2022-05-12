Shader "Custom/Tools"
{
    SubShader
    {
        Pass
        {
            Tags
            {
                "LightMode" = "CopyDepth"
            }
            Blend Off
            ZTest Off
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #include "../ShaderLibrary/Common.hlsl"
            #include "../ShaderLibrary/FullScreenPassVS.hlsl"
            #pragma fragment CopyDepthPassFrag

            Texture2D _SrcDepth;
            SamplerState _point_clamp_sampler;
            float CopyDepthPassFrag(Varyings input) : SV_Depth
            {
                return _SrcDepth.Sample(_point_clamp_sampler, input.uv).x;
            }
            ENDHLSL
        }
    }
}
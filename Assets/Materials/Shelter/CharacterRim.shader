Shader "UnityGameJam/Character Rim 2D"
{
    Properties
    {
        [PerRendererData] _MainTex("Character", 2D) = "white" {}
        [PerRendererData] _MaskTex("Rim mask (R)", 2D) = "black" {}
        _BodyShadowStrength("Body shadow strength", Range(0, 1)) = 0.75
        _RimStrength("Rim strength", Range(0, 2)) = 0.65
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

            struct Attributes { COMMON_2D_INPUTS half4 color : COLOR; };
            struct Varyings { COMMON_2D_LIT_OUTPUTS half4 color : COLOR; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_MaskTex); SAMPLER(sampler_MaskTex);
            CBUFFER_START(UnityPerMaterial)
                half _BodyShadowStrength;
                half _RimStrength;
            CBUFFER_END

            Varyings Vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                SetUpSpriteInstanceProperties();
                o.positionCS = TransformObjectToHClip(UnityFlipSprite(v.positionOS, unity_SpriteProps.xy));
                o.lightingUV = ComputeScreenPos(o.positionCS / o.positionCS.w).xy;
                o.uv = v.uv;
                o.color = v.color * unity_SpriteColor;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half4 pixel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half3 illumination = 0;
                #if USE_SHAPE_LIGHT_TYPE_0
                    illumination = SAMPLE_TEXTURE2D(_ShapeLightTexture0, sampler_ShapeLightTexture0, i.lightingUV).rgb;
                #endif
                half3 rim = 0;
                #if USE_SHAPE_LIGHT_TYPE_2
                    half3 light = SAMPLE_TEXTURE2D(_ShapeLightTexture2, sampler_ShapeLightTexture2, i.lightingUV).rgb;
                    illumination += light;
                    half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv);
                    rim = light * dot(mask, _ShapeLightMaskFilter2) * _RimStrength;
                #endif
                half3 body = pixel.rgb * lerp(1.0h, saturate(illumination), _BodyShadowStrength);
                return half4(body + rim, pixel.a) * i.color;
            }
            ENDHLSL
        }
    }
}

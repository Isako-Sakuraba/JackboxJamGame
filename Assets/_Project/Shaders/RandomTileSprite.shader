Shader "Game/Sprites/Random Tile Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _TileTex ("Tile Sprite Sheet", 2D) = "white" {}
        _Columns ("Columns", Range(1, 32)) = 4
        _Rows ("Rows", Range(1, 32)) = 4
        _PixelsPerUnit ("Pixels Per Unit", Range(1, 1024)) = 100
        _TileTextureSize ("Tile Texture Size Pixels", Vector) = (256, 256, 0, 0)
        _TileOffset ("Tile Offset", Vector) = (0, 0, 0, 0)
        _MaskTex ("Mask", 2D) = "white" {}
        [MaterialToggle] _ZWrite ("ZWrite", Float) = 0
        [HideInInspector] _SpriteRendererColor ("Sprite Renderer Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex LitVertex
            #pragma fragment LitFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color : COLOR;
                float2 worldPosition : TEXCOORD4;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_TileTex);
            SAMPLER(sampler_TileTex);
            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _SpriteRendererColor;
                half _Columns;
                half _Rows;
                half _PixelsPerUnit;
                float4 _TileTextureSize;
                float4 _TileOffset;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            Varyings LitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.lightingUV = half2(ComputeScreenPos(output.positionCS / output.positionCS.w).xy);
                output.color = input.color * _Color * unity_SpriteColor * _SpriteRendererColor;
                output.worldPosition = TransformObjectToWorld(input.positionOS).xy;

                #if defined(DEBUG_DISPLAY)
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.normalWS = TransformObjectToWorldDir(input.normal);
                #endif

                return output;
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                half4 spriteMask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);

                float columns = max(floor(_Columns + 0.5), 1.0);
                float rows = max(floor(_Rows + 0.5), 1.0);
                float totalTiles = columns * rows;
                float2 tilePixelSize = max(_TileTextureSize.xy, 1.0) / float2(columns, rows);
                float2 tileWorldSize = tilePixelSize / max(_PixelsPerUnit, 1.0);

                float2 tilePosition = (input.worldPosition + _TileOffset.xy) / max(tileWorldSize, 0.001);
                float2 tileCell = floor(tilePosition);
                float2 tileUv = frac(tilePosition);

                float tileIndex = floor(Hash21(tileCell) * totalTiles);
                tileIndex = min(tileIndex, totalTiles - 1.0);

                float column = fmod(tileIndex, columns);
                float row = floor(tileIndex / columns);
                float2 atlasUv = (float2(column, row) + tileUv) / float2(columns, rows);

                half4 color = SAMPLE_TEXTURE2D(_TileTex, sampler_TileTex, atlasUv) * input.color;
                color.a *= spriteMask.a;

                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(color.rgb, color.a, mask, half3(0.0, 0.0, 1.0), surfaceData);
                InitializeInputData(input.uv, input.lightingUV, inputData);

                #if defined(DEBUG_DISPLAY)
                SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, input.positionWS, input.positionCS, _MainTex);
                surfaceData.normalWS = input.normalWS;
                #endif

                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }
    }
}

Shader "Game/Sprites/Graffiti Noise Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _SecondaryColor ("Secondary Color", Color) = (0, 0, 0, 1)
        _NoisePixelSize ("Noise Pixel Size World Units", Range(0.001, 10)) = 0.08
        _GlobalNoiseOffset ("Global Noise Offset", Vector) = (0, 0, 0, 0)
        _NoiseScale ("Region Noise Scale", Range(0.01, 100)) = 8
        _NoiseOffset ("Region Noise Offset", Vector) = (0, 0, 0, 0)
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 1
        [HideInInspector] _SpriteRendererColor ("Sprite Renderer Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _Flip ("Flip", Vector) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
            "PreviewType" = "Plane"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "SpriteUnlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            CGPROGRAM
            #pragma vertex GraffitiNoiseVert
            #pragma fragment GraffitiNoiseFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "UnitySprites.cginc"

            struct graffiti_v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _BaseColor;
            fixed4 _SecondaryColor;
            half _NoisePixelSize;
            float4 _GlobalNoiseOffset;
            half _NoiseScale;
            float4 _NoiseOffset;
            half _NoiseStrength;
            fixed4 _SpriteRendererColor;

            graffiti_v2f GraffitiNoiseVert(appdata_t input)
            {
                graffiti_v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 vertex = UnityFlipSprite(input.vertex, _Flip);

                output.vertex = UnityObjectToClipPos(vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color * _RendererColor;
                output.worldPosition = mul(unity_ObjectToWorld, vertex).xy;

                #ifdef PIXELSNAP_ON
                output.vertex = UnityPixelSnap(output.vertex);
                #endif

                return output;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float SmoothValueNoise(float2 uv)
            {
                float2 cell = floor(uv);
                float2 local = frac(uv);
                float2 smoothLocal = local * local * (3.0 - 2.0 * local);

                float bottomLeft = Hash21(cell);
                float bottomRight = Hash21(cell + float2(1.0, 0.0));
                float topLeft = Hash21(cell + float2(0.0, 1.0));
                float topRight = Hash21(cell + float2(1.0, 1.0));

                float bottom = lerp(bottomLeft, bottomRight, smoothLocal.x);
                float top = lerp(topLeft, topRight, smoothLocal.x);
                return lerp(bottom, top, smoothLocal.y);
            }

            fixed4 GraffitiNoiseFrag(graffiti_v2f input) : SV_Target
            {
                fixed4 spriteColor = SampleSpriteTexture(input.texcoord);
                float2 noisePosition = input.worldPosition + _GlobalNoiseOffset.xy;
                float2 noiseCell = floor(noisePosition / max(_NoisePixelSize, 0.001));

                half pixelNoise = Hash21(noiseCell);
                half regionNoise = SmoothValueNoise(noisePosition * _NoiseScale + _NoiseOffset.xy);
                half colorLerp = saturate(pixelNoise * regionNoise * _NoiseStrength);

                fixed4 color = lerp(_BaseColor, _SecondaryColor, colorLerp);
                color.rgb *= _SpriteRendererColor.rgb;
                color.a *= spriteColor.a * _SpriteRendererColor.a;
                return color;
            }
            ENDCG
        }
    }
}

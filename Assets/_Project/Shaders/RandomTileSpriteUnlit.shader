Shader "Game/Sprites/Random Tile Sprite Unlit"
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
        [MaterialToggle] _ZWrite ("ZWrite", Float) = 0
        [HideInInspector] _SpriteRendererColor ("Sprite Renderer Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _Flip ("Flip", Vector) = (1, 1, 1, 1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
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

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            Name "SpriteUnlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            CGPROGRAM
            #pragma vertex RandomTileUnlitVert
            #pragma fragment RandomTileUnlitFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "UnitySprites.cginc"

            struct random_tile_unlit_v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _TileTex;
            half _Columns;
            half _Rows;
            half _PixelsPerUnit;
            float4 _TileTextureSize;
            float4 _TileOffset;
            fixed4 _SpriteRendererColor;

            random_tile_unlit_v2f RandomTileUnlitVert(appdata_t input)
            {
                random_tile_unlit_v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 vertex = UnityFlipSprite(input.vertex, _Flip);

                output.vertex = UnityObjectToClipPos(vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color * _RendererColor * _SpriteRendererColor;
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

            fixed4 RandomTileUnlitFrag(random_tile_unlit_v2f input) : SV_Target
            {
                fixed4 spriteMask = SampleSpriteTexture(input.texcoord);

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

                fixed4 color = tex2D(_TileTex, atlasUv) * input.color;
                color.a *= spriteMask.a;
                return color;
            }
            ENDCG
        }
    }
}

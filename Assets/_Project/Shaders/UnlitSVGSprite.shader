Shader "Game/Sprites/Unlit SVG Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
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

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "SpriteUnlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            CGPROGRAM
            #pragma vertex SvgSpriteVert
            #pragma fragment SvgSpriteFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "UnitySprites.cginc"

            fixed4 _SpriteRendererColor;

            struct svg_sprite_v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            svg_sprite_v2f SvgSpriteVert(appdata_t input)
            {
                svg_sprite_v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.vertex = UnityObjectToClipPos(UnityFlipSprite(input.vertex, _Flip));
                output.texcoord = input.texcoord;
                output.color = _SpriteRendererColor * _Color * _RendererColor;

                #ifdef PIXELSNAP_ON
                output.vertex = UnityPixelSnap(output.vertex);
                #endif

                return output;
            }

            fixed4 SvgSpriteFrag(svg_sprite_v2f input) : SV_Target
            {
                fixed4 spriteColor = SampleSpriteTexture(input.texcoord);
                spriteColor *= input.color;
                return spriteColor;
            }
            ENDCG
        }
    }
}

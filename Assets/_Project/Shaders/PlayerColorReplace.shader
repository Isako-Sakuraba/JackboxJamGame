Shader "Game/Player/Color Replace Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _PlayerColor ("Player Color", Color) = (0, 1, 0, 1)
        _KeyColor ("Color To Replace", Color) = (1, 0, 0, 1)
        _Tolerance ("Tolerance", Range(0, 1)) = 0.25
        _Softness ("Softness", Range(0.0001, 1)) = 0.08
        _Color ("Tint", Color) = (1, 1, 1, 1)
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
            #pragma vertex SpriteVert
            #pragma fragment ColorReplaceFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "UnitySprites.cginc"

            fixed4 _PlayerColor;
            fixed4 _KeyColor;
            half _Tolerance;
            half _Softness;

            fixed4 ColorReplaceFrag(v2f input) : SV_Target
            {
                fixed4 spriteColor = SampleSpriteTexture(input.texcoord) * input.color;
                fixed3 sourceRgb = spriteColor.rgb;

                half distanceToKey = distance(sourceRgb, _KeyColor.rgb);
                half replaceAmount = 1.0 - smoothstep(_Tolerance, _Tolerance + _Softness, distanceToKey);
                fixed3 replacedRgb = _PlayerColor.rgb * sourceRgb.r;

                spriteColor.rgb = lerp(sourceRgb, replacedRgb, replaceAmount);
                return spriteColor;
            }
            ENDCG
        }
    }
}

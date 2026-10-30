// Sombra plana do aviao: a propria malha achatada no chao (Mundo.Patio.cs), preta e translucida. O stencil pinta cada pixel uma vez:
// asa sobre fuselagem nao escurece em dobro. Antes dos sprites, para quem passa por cima da sombra ficar inteiro.
Shader "TA/Sombra"
{
    Properties
    {
        _Color ("Cor", Color) = (0, 0, 0, 0.26)
    }
    SubShader
    {
        Tags { "Queue"="Transparent-1" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Stencil { Ref 1 Comp NotEqual Pass Replace }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;

            float4 vert(float4 v : POSITION) : SV_POSITION { return UnityObjectToClipPos(v); }
            fixed4 frag() : SV_Target { return _Color; }
            ENDCG
        }
    }
}

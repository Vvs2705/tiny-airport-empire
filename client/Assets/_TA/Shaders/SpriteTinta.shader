// Folha de pessoa do TA (client/tools/render_sprites.py --mask): cor na metade de cima da textura, mascara do uniforme (ou da mala)
// na de baixo. Onde a mascara acende, a cor ja vem em cinza neutro (luminancia media 0,5) e sai 2 x cinza x a cor do vertice
// (SpriteRenderer.color = cor do setor ou do voo); fora dela, a cor da folha. O alfa do vertice esmaece o boneco inteiro.
// Sem MaterialPropertyBlock: a tinta vai no vertice, entao todos os bonecos da mesma folha entram no mesmo lote.
Shader "TA/SpriteTinta"
{
    Properties
    {
        [PerRendererData] _MainTex ("Folha", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                fixed m = tex2D(_MainTex, i.uv - float2(0, 0.5)).r;
                c.rgb = lerp(c.rgb, saturate(2 * c.rgb * i.color.rgb), m);
                c.a *= i.color.a;
                return c;
            }
            ENDCG
        }
    }
}

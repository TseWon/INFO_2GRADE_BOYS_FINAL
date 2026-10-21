Shader "FacilityEscape/WorldText"
{
    Properties { _MainTex ("Font Atlas", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off ZTest LEqual
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct v2f { float4 position : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            v2f vert(appdata_full v)
            {
                v2f o; o.position = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord.xy; o.color = v.color; return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = i.color;
                color.a *= tex2D(_MainTex, i.uv).a;
                return color;
            }
            ENDCG
        }
    }
}

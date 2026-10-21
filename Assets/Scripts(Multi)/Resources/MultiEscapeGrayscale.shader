Shader "Hidden/FacilityEscape/Grayscale"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 frag(v2f_img input) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, input.uv);
                fixed gray = dot(color.rgb, fixed3(0.2126, 0.7152, 0.0722));
                return fixed4(gray, gray, gray, color.a);
            }
            ENDCG
        }
    }
    Fallback Off
}

Shader "Environment/Surface Station Snow PBR"
{
 Properties {
  _Color("Tint",Color)=(1,1,1,1)
  _MainTex("Base color",2D)="white"{}
  _BumpMap("Normal",2D)="bump"{}
  _BumpScale("Normal strength",Range(0,1))=.28
  _MetallicGlossMap("Metallic R / Smoothness A",2D)="white"{}
  _SnowAmount("Snow coverage",Range(0,1))=.65
  _SnowColor("Snow tint",Color)=(.78,.83,.87,1)
 }
 SubShader {
  Tags {"RenderType"="Opaque"} LOD 300
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows addshadow
  #pragma target 3.0
  #include "UnityStandardUtils.cginc"
  sampler2D _MainTex,_BumpMap,_MetallicGlossMap;
  fixed4 _Color,_SnowColor;half _BumpScale,_SnowAmount;
  struct Input {float2 uv_MainTex;float2 uv_BumpMap;float3 worldPos;float3 worldNormal;INTERNAL_DATA};
  float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
  float grain(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
  void surf(Input IN,inout SurfaceOutputStandard o){
   fixed3 base=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb;
   fixed4 surface=tex2D(_MetallicGlossMap,IN.uv_MainTex);
   float3 normal=UnpackScaleNormal(tex2D(_BumpMap,IN.uv_BumpMap),_BumpScale);
   float up=WorldNormalVector(IN,float3(0,0,1)).y;
   float breakup=grain(IN.worldPos.xz*2.4)*.2+grain(IN.worldPos.xz*.23)*.15;
   float snow=smoothstep(.35,.82,up+breakup-.15)*_SnowAmount;
   o.Albedo=lerp(base,_SnowColor.rgb*(.95+.05*grain(IN.worldPos.xz*14)),snow);
   o.Normal=normalize(lerp(normal,float3(0,0,1),snow*.85));
   o.Metallic=lerp(surface.r,0,snow);o.Smoothness=lerp(surface.a,.16,snow);o.Alpha=1;
  }
  ENDCG
 }
 FallBack "Standard"
}

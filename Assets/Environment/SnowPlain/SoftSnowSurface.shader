Shader "Environment/Soft Snow Surface"
{
 Properties {
  _Color("Tint",Color)=(1,1,1,1)
  _MainTex("Albedo",2D)="white"{}
  _BumpMap("Normal",2D)="bump"{}
  _BumpScale("Normal strength",Range(0,1))=.18
  _Metallic("Metallic",Range(0,1))=0
  _Glossiness("Smoothness",Range(0,1))=.25
  _SnowAmount("Snow coverage",Range(0,1))=.65
  _Camouflage("Camouflage",Range(0,1))=0
  _WeatherSoftness("Weather softness",Range(0,1))=.15
  _SnowColor("Snow tint",Color)=(.78,.83,.87,1)
 }
 SubShader {
  Tags {"RenderType"="Opaque"} LOD 300
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows addshadow
  #pragma target 3.0
  #include "UnityStandardUtils.cginc"
  sampler2D _MainTex,_BumpMap;
  fixed4 _Color,_SnowColor;
  half _BumpScale,_Metallic,_Glossiness,_SnowAmount,_Camouflage,_WeatherSoftness;
  struct Input {float2 uv_MainTex;float2 uv_BumpMap;float3 worldPos;float3 worldNormal;INTERNAL_DATA};
  float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
  float noise2(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
  void surf(Input IN,inout SurfaceOutputStandard o){
   fixed3 base=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb;
   float luminance=dot(base,float3(.2126,.7152,.0722));
   base=lerp(base,luminance.xxx,_WeatherSoftness*.4);
   base=lerp(base,sqrt(max(base,.001))*.65,_WeatherSoftness);
   float camo=noise2(IN.worldPos.xz*3+IN.worldPos.y*2);
   base=lerp(base,lerp(float3(.115,.14,.085),float3(.19,.175,.13),smoothstep(.42,.55,camo))*(.85+.15*noise2(IN.worldPos.xy*22)),_Camouflage);
   float3 n=UnpackScaleNormal(tex2D(_BumpMap,IN.uv_BumpMap),_BumpScale);
   float up=WorldNormalVector(IN,float3(0,0,1)).y;
   float variation=noise2(IN.worldPos.xz*2.4)*.2+noise2(IN.worldPos.xz*.23)*.15;
   float snow=smoothstep(.35,.82,up+variation-.15)*_SnowAmount;
   o.Albedo=lerp(base,_SnowColor.rgb*(.95+.05*noise2(IN.worldPos.xz*14)),snow);
   o.Normal=normalize(lerp(n,float3(0,0,1),snow*.85));
   o.Metallic=lerp(_Metallic,0,snow);o.Smoothness=lerp(_Glossiness,.16,snow);o.Alpha=1;
  }
  ENDCG
 }
 FallBack "Standard"
}

using UnityEngine;
using UnityEditor;
public static class PolishInteriorGlass {
 public static string Run(){
  var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environment/SurfaceResearchInterior/Materials/ClearObservationGlass.mat");
  m.color=new Color(.64f,.77f,.82f,.08f);m.SetFloat("_Glossiness",.60f);m.SetFloat("_SpecularHighlights",0);m.SetFloat("_GlossyReflections",0);m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");m.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
  EditorUtility.SetDirty(m);AssetDatabase.SaveAssets();return "Reduced glass glare; preserved clear visibility through windows and atrium skylight.";
 }
}

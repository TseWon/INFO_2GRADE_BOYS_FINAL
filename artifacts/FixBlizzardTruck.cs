using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class FixBlizzardTruck {
 static Bounds B(GameObject g){var rs=g.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 public static string Run(){var g=GameObject.Find("Military Truck - Forecourt");var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blender/military_cargo_truck.blend");g.transform.rotation=Quaternion.Euler(0,28,0)*asset.transform.rotation;g.transform.position+=Vector3.up*(.005f-B(g).min.y);var hull=g.GetComponent<BoxCollider>();var world=B(g);hull.center=g.transform.InverseTransformPoint(world.center);var size=g.transform.InverseTransformVector(new Vector3(0,world.size.y,0));hull.size=Mathf.Abs(size.z)>Mathf.Abs(size.y)?new Vector3(8,2.45f,3.2f):new Vector3(8,3.2f,2.45f);EditorSceneManager.MarkSceneDirty(g.scene);EditorSceneManager.SaveScene(g.scene);return "Imported rotation="+asset.transform.eulerAngles+"; corrected bounds="+B(g);}
}

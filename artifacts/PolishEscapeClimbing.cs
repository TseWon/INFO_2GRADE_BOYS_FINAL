using System;
using FacilityEscape;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PolishEscapeClimbing
{
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Stop Play Mode first");
        var c=UnityEngine.Object.FindAnyObjectByType<MultiEscapeCampaign>();
        if(c==null||c.gameObject.scene.path!="Assets/Scene/MultiScene1.unity")throw new Exception("Expected MultiScene1");
        var hall=c.transform.Find("03_Pursuit");
        var ceiling=hall.Find("Ceiling");
        if(hall.Find("ClimbHeadroom")!=null)return "Headroom already installed.";
        Undo.RecordObject(ceiling.gameObject,"Raise ceiling over climbing wall");
        ceiling.gameObject.SetActive(false);
        var root=new GameObject("ClimbHeadroom");root.transform.SetParent(hall,false);Undo.RegisterCreatedObjectUndo(root,"Climb headroom");
        var material=ceiling.GetComponent<Renderer>().sharedMaterial;
        Add(root.transform,"CeilingBefore",new Vector3(0,3.7f,34.5f),new Vector3(10,.2f,29),material);
        Add(root.transform,"CeilingAfter",new Vector3(0,3.7f,60.5f),new Vector3(10,.2f,9),material);
        Add(root.transform,"RaisedCeiling",new Vector3(0,5.8f,52.5f),new Vector3(10,.2f,7),material);
        Add(root.transform,"UpperFront",new Vector3(0,4.75f,49),new Vector3(10,2.1f,.2f),material);
        Add(root.transform,"UpperBack",new Vector3(0,4.75f,56),new Vector3(10,2.1f,.2f),material);
        foreach(int side in new[]{-1,1})Add(root.transform,"UpperSide",new Vector3(side*5,4.75f,52.5f),new Vector3(.2f,2.1f,7),material);
        EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);
        PrefabUtility.SaveAsPrefabAsset(c.transform.root.gameObject,"Assets/MultiplayerPrototype/Prefabs/FacilityEscapePrototype.prefab");
        return "Saved raised ceiling over assisted climb; original geometry retained disabled.";
    }
    static void Add(Transform parent,string name,Vector3 at,Vector3 size,Material material)
    {var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);o.transform.position=at;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=material;}
}

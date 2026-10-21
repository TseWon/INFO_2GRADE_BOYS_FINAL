using System.Collections.Generic;
using UnityEngine;

// Lightweight procedural gait for this custom equipment rig.
public class SoldierLocomotion : MonoBehaviour
{
    public CharacterController motor;
    readonly Dictionary<string,Transform> bones = new Dictionary<string,Transform>();
    readonly Dictionary<string,Quaternion> rest = new Dictionary<string,Quaternion>();
    float phase, amount;
    void Awake()
    {
        foreach (var t in GetComponentsInChildren<Transform>())
        { bones[t.name] = t; rest[t.name] = t.localRotation; }
    }
    void Bend(string name, float degrees)
    {
        if (bones.TryGetValue(name, out var t)) t.localRotation = rest[name] * Quaternion.AngleAxis(degrees, Vector3.right);
    }
    void LateUpdate()
    {
        if (motor == null) return;
        var v = motor.velocity; v.y = 0;
        amount = Mathf.MoveTowards(amount, Mathf.Clamp01(v.magnitude / 2.8f), Time.deltaTime * 6);
        phase += Time.deltaTime * Mathf.Lerp(3, 9, Mathf.Clamp01(v.magnitude / 4.5f));
        float wave = Mathf.Sin(phase) * amount;
        Bend("Thigh_L", wave * 24); Bend("Thigh_R", -wave * 24);
        Bend("Shin_L", Mathf.Max(0, -wave) * 30); Bend("Shin_R", Mathf.Max(0, wave) * 30);
        Bend("Foot_L", -wave * 7); Bend("Foot_R", wave * 7);
        Bend("UpperArm_L", -wave * 15); Bend("UpperArm_R", wave * 15);
        Bend("Forearm_L", -Mathf.Abs(wave) * 8); Bend("Forearm_R", -Mathf.Abs(wave) * 8);
    }
}

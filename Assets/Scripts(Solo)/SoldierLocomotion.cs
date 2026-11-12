using System.Collections.Generic;
using UnityEngine;

// Procedural locomotion for the skinned protagonist. The controller remains
// authoritative for movement; this component only poses the visual bones.
public class SoldierLocomotion : MonoBehaviour
{
    public CharacterController motor;
    [Header("Gait")]
    [Min(.5f)] public float walkStride = 1.35f;
    [Min(.5f)] public float runStride = 1.9f;
    [Min(1f)] public float motionSharpness = 10f;
    [Min(1f)] public float poseSharpness = 18f;

    readonly Dictionary<string,Transform> bones = new Dictionary<string,Transform>();
    readonly Dictionary<string,Quaternion> rest = new Dictionary<string,Quaternion>();
    float phase, amount, speed;
    Vector2 localMotion;
    Vector3 rootRestPosition;

    void Awake()
    {
        if (motor == null) motor = GetComponentInParent<CharacterController>();
        rootRestPosition = transform.localPosition;
        foreach (var t in GetComponentsInChildren<Transform>())
        { bones[t.name] = t; rest[t.name] = t.localRotation; }

        // Meshy humanoids use Unity-style bone names rather than the legacy
        // FittedSoldierRig names. Register aliases so the same procedural gait
        // drives both rigs without changing scene gameplay objects.
        Alias("Thigh_L", "LeftUpLeg");
        Alias("Thigh_R", "RightUpLeg");
        Alias("Shin_L", "LeftLeg");
        Alias("Shin_R", "RightLeg");
        Alias("Foot_L", "LeftFoot");
        Alias("Foot_R", "RightFoot");
        Alias("UpperArm_L", "LeftArm");
        Alias("UpperArm_R", "RightArm");
        Alias("Forearm_L", "LeftForeArm");
        Alias("Forearm_R", "RightForeArm");
        Alias("Chest", "Spine02");
    }

    void Alias(string expectedName, string importedName)
    {
        if (bones.ContainsKey(expectedName) || !bones.TryGetValue(importedName, out var imported)) return;
        bones[expectedName] = imported;
        rest[expectedName] = imported.localRotation;
    }

    void Pose(string name, Vector3 eulerOffset, float blend)
    {
        if (!bones.TryGetValue(name, out var t)) return;
        var target = rest[name] * Quaternion.Euler(eulerOffset);
        t.localRotation = Quaternion.Slerp(t.localRotation, target, blend);
    }

    void LateUpdate()
    {
        if (motor == null) return;
        float dt = Time.deltaTime;
        var worldVelocity = motor.velocity;
        worldVelocity.y = 0;
        var localVelocity = motor.transform.InverseTransformDirection(worldVelocity);
        float targetSpeed = worldVelocity.magnitude;
        float motionBlend = 1f - Mathf.Exp(-motionSharpness * dt);
        float poseBlend = 1f - Mathf.Exp(-poseSharpness * dt);

        speed = Mathf.Lerp(speed, targetSpeed, motionBlend);
        amount = Mathf.Lerp(amount, Mathf.Clamp01(speed / 2.5f), motionBlend);
        Vector2 targetMotion = targetSpeed > .05f
            ? new Vector2(localVelocity.x, localVelocity.z) / targetSpeed
            : Vector2.zero;
        localMotion = Vector2.Lerp(localMotion, targetMotion, motionBlend);

        // Drive the cycle by distance travelled. Feet therefore keep the same
        // cadence when changing between walking, sprinting and diagonal input.
        float run = Mathf.InverseLerp(2.8f, 4.5f, speed);
        float stride = Mathf.Lerp(walkStride, runStride, run);
        if (speed > .02f)
            phase += dt * (2f * Mathf.PI * speed / stride);

        ApplyPose(poseBlend);
    }

    void ApplyPose(float poseBlend)
    {
        float run = Mathf.InverseLerp(2.8f, 4.5f, speed);
        float wave = Mathf.Sin(phase) * amount;
        float step = Mathf.Cos(phase);
        float forward = localMotion.y;
        float side = localMotion.x;
        float travelSign = Mathf.Abs(forward) > .2f ? Mathf.Sign(forward) : 1f;
        float leg = Mathf.Lerp(25f, 34f, run);
        float lateralStep = side * wave * Mathf.Lerp(8f, 12f, run);
        float forwardStep = wave * leg * travelSign;

        Pose("Thigh_L", new Vector3(forwardStep, 0, -lateralStep), poseBlend);
        Pose("Thigh_R", new Vector3(-forwardStep, 0, lateralStep), poseBlend);
        Pose("Shin_L", new Vector3(Mathf.Max(0, -wave) * Mathf.Lerp(31f, 43f, run), 0, 0), poseBlend);
        Pose("Shin_R", new Vector3(Mathf.Max(0, wave) * Mathf.Lerp(31f, 43f, run), 0, 0), poseBlend);
        Pose("Foot_L", new Vector3(-wave * Mathf.Lerp(8f, 13f, run), 0, side * 3f * amount), poseBlend);
        Pose("Foot_R", new Vector3(wave * Mathf.Lerp(8f, 13f, run), 0, side * 3f * amount), poseBlend);

        float arm = Mathf.Lerp(13f, 22f, run) * Mathf.Lerp(1f, .72f, Mathf.Abs(side));
        // The source rig rests in an A-pose. Fold the arms toward the torso
        // before layering the stride so the character does not run spread-eagle.
        Pose("UpperArm_L", new Vector3(-wave * arm * travelSign, 0, 45f + side * 4f * amount), poseBlend);
        Pose("UpperArm_R", new Vector3(wave * arm * travelSign, 0, -45f + side * 4f * amount), poseBlend);
        Pose("Forearm_L", new Vector3(-8f - run * 8f - Mathf.Max(0, wave) * 10f, 0, 0), poseBlend);
        Pose("Forearm_R", new Vector3(-8f - run * 8f - Mathf.Max(0, -wave) * 10f, 0, 0), poseBlend);

        float hipYaw = wave * Mathf.Lerp(3f, 6f, run);
        float lean = run * 5f * amount;
        Pose("Hips", new Vector3(lean, hipYaw, -side * Mathf.Lerp(2f, 5f, run) * amount), poseBlend);
        Pose("Spine", new Vector3(-lean * .35f, -hipYaw * .55f, side * 2.5f * amount), poseBlend);
        float breath = Mathf.Sin(Time.time * 1.7f) * 0.75f * (1f - amount);
        Pose("Chest", new Vector3(breath - lean * .25f, -hipYaw * .35f, side * 1.5f * amount), poseBlend);
        Pose("Head", new Vector3(0, hipYaw * .15f, -side * amount), poseBlend);

        // A small whole-body drop at foot contact removes the rigid gliding
        // look without moving the CharacterController or introducing root motion.
        float bob = -Mathf.Abs(step) * Mathf.Lerp(.012f, .024f, run) * amount;
        float idleBreath = Mathf.Sin(Time.time * 1.7f) * .0025f * (1f - amount);
        var rootTarget = rootRestPosition + Vector3.up * (bob + idleBreath);
        transform.localPosition = Vector3.Lerp(transform.localPosition, rootTarget, poseBlend);
    }

#if UNITY_EDITOR
    // Uses the exact runtime pose path, but gives automated/editor validation a
    // deterministic mid-stride frame without shipping any preview behaviour.
    public void PreviewRunPose()
    {
        speed = 4.5f;
        amount = 1f;
        localMotion = Vector2.up;
        phase = Mathf.PI * .5f;
        ApplyPose(1f);
    }
#endif

    void OnDisable()
    {
        transform.localPosition = rootRestPosition;
        foreach (var pair in bones)
            if (pair.Value != null && rest.TryGetValue(pair.Key, out var rotation))
                pair.Value.localRotation = rotation;
    }
}

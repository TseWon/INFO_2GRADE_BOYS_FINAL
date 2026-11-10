using UnityEngine;

/// <summary>Keeps the outdoor snow emitter above the entire glazed atrium.</summary>
public sealed class SurfaceInteriorSnow : MonoBehaviour
{
    public Transform observer;
    public ParticleSystem snowfall;
    public float emissionHeight = 23f;
    public Transform building;
    ParticleSystem.Particle[] particles;

    void Start()
    {
        // Play mode can begin while this emitter is off-camera. Starting it explicitly and
        // keeping it simulated prevents the exterior snowfall from appearing empty on exit.
        if (snowfall && !snowfall.isPlaying)
            snowfall.Play(true);
    }

    static bool Rect(float x, float z, float x0, float x1, float z0, float z1)
        => x >= x0 && x <= x1 && z >= z0 && z <= z1;

    static bool InShelter(Vector3 p)
    {
        // Roof collision is primary. This closes occasional particle misses and wind drift
        // through the open entrance without testing thousands of detailed cage triangles.
        if (p.y < 0f || p.y > 9.6f) return false;
        float x = Mathf.Abs(p.x), z = p.z;
        float courtHalfWidth = z >= -4f ? 12f : Mathf.Lerp(8.5f, 12f, (z + 8.9f) / 4.9f);
        if (z >= -8.9f && z <= 13.9f && x <= courtHalfWidth) return true;
        if (p.y > 4.22f) return false;
        if (Rect(x,z,0,10.13f,14,25.13f) || Rect(x,z,0,8.13f,-27.13f,-9)) return true;
        if (Rect(x,z,8,21.13f,-23.13f,-13)) return true;
        if ((x-14f)*(x-14f)+(z-18.5f)*(z-18.5f) <= 25.3f) return true;
        if (Rect(x,z,12.07f,15.93f,-4,14.1f)) return true;
        Vector2 a = new Vector2(14,-4), d = new Vector2(-3.5f,-9);
        Vector2 v = new Vector2(x,z)-a;
        float t = Mathf.Clamp01(Vector2.Dot(v,d)/d.sqrMagnitude);
        return z >= -13.1f && z <= -3.9f && (v-d*t).sqrMagnitude < 3.65f;
    }

    void LateUpdate()
    {
        if (!snowfall || !observer) return;
        if (!snowfall.isPlaying) snowfall.Play(true);
        Vector3 p = observer.position;
        snowfall.transform.position = new Vector3(p.x - 5f, emissionHeight, p.z + 8f);
        snowfall.transform.rotation = Quaternion.identity;
        if (!building) return;
        if (particles == null || particles.Length < snowfall.main.maxParticles)
            particles = new ParticleSystem.Particle[snowfall.main.maxParticles];
        int count = snowfall.GetParticles(particles);
        Matrix4x4 local = building.worldToLocalMatrix;
        bool changed = false;
        for (int i=0; i<count; i++)
        {
            if (!InShelter(local.MultiplyPoint3x4(particles[i].position))) continue;
            particles[i].remainingLifetime = 0f;
            changed = true;
        }
        if (changed) snowfall.SetParticles(particles,count);
    }
}

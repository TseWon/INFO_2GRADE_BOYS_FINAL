using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(CharacterController))]
public class SnowFirstPerson : MonoBehaviour
{
    public Camera viewCamera;
    public Transform visual;
    public ParticleSystem snowfall;
    public float walkSpeed = 2.8f;
    public float runSpeed = 4.5f;
    public float mouseSensitivity = 1.8f;
    public float boundary = 188f;
    CharacterController motor;
    float verticalSpeed, pitch;
    Vector2 smoothLook;
    Vector3 spawn;
    Vector3 eyeOffset;
    public bool thirdPerson;

    void Awake()
    {
        motor = GetComponent<CharacterController>();
        spawn = transform.position;
        eyeOffset = viewCamera.transform.localPosition;
        if (visual != null)
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
    }
    void Start() { SetCapture(true); }
    void SetCapture(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
        smoothLook = Vector2.zero;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V)) SetThirdPerson(!thirdPerson);
        if (Input.GetKeyDown(KeyCode.Escape)) SetCapture(false);
        else if (Input.GetMouseButtonDown(0)) SetCapture(true);
        if (Cursor.lockState != CursorLockMode.Locked) { Move(Vector2.zero, false, Time.deltaTime); return; }
        Look(new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")), Time.deltaTime);
        float x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0)
                - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
        float z = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0)
                - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
        Move(new Vector2(x, z), Input.GetKey(KeyCode.LeftShift), Time.deltaTime);
    }
    public void Look(Vector2 delta, float dt)
    {
        smoothLook = Vector2.Lerp(smoothLook, delta * mouseSensitivity, 1 - Mathf.Exp(-25 * dt));
        transform.Rotate(0, smoothLook.x, 0);
        pitch = Mathf.Clamp(pitch - smoothLook.y, -80, 80);
        viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
    }
    public void Move(Vector2 input, bool sprint, float dt)
    {
        if (!motor.enabled) return;
        input = Vector2.ClampMagnitude(input, 1);
        if (motor.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
        verticalSpeed = Mathf.Max(verticalSpeed - 20 * dt, -35);
        var velocity = (transform.right * input.x + transform.forward * input.y) * (sprint ? runSpeed : walkSpeed);
        motor.Move((velocity + Vector3.up * verticalSpeed) * dt);
        // Physical perimeter walls are primary; this catches teleport/fall errors.
        if (Mathf.Abs(transform.position.x) > boundary + 4 || Mathf.Abs(transform.position.z) > boundary + 4 || transform.position.y < -10)
        { motor.enabled = false; transform.position = spawn; motor.enabled = true; verticalSpeed = 0; }
    }
    void LateUpdate()
    {
        if (thirdPerson)
        {
            Vector3 origin = transform.TransformPoint(eyeOffset);
            Vector3 backwards = -viewCamera.transform.forward;
            float distance = 2.8f;
            foreach (var hit in Physics.SphereCastAll(origin, .15f, backwards, distance, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(transform)) distance = Mathf.Min(distance, Mathf.Max(.1f, hit.distance - .08f));
            viewCamera.transform.position = origin + backwards * distance;
        }
        else viewCamera.transform.localPosition = eyeOffset;
        if (snowfall != null)
        {
            snowfall.transform.position = transform.position + new Vector3(-5, 9, 8);
            snowfall.transform.rotation = Quaternion.identity;
        }
    }
    public void SetThirdPerson(bool value)
    {
        thirdPerson = value;
        if (visual != null)
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = value ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
    }
    void OnDisable() { SetCapture(false); }
}

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
    public bool InputBlocked { get; set; }
    public float minimumY = -10f;
    public float jumpHeight = 1.1f;
    public bool IsCrawling { get; private set; }
    float standingHeight;
    Vector3 standingCenter;

    void Awake()
    {
        motor = GetComponent<CharacterController>();
        spawn = transform.position;
        eyeOffset = viewCamera.transform.localPosition;
        standingHeight = motor.height;
        standingCenter = motor.center;
        if (visual != null)
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
    }
    void Start() { if (!InputBlocked) SetCapture(true); }
    void SetCapture(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
        smoothLook = Vector2.zero;
    }
    void Update()
    {
        if (InputBlocked) return;
        if (Input.GetKeyDown(KeyCode.V)) SetThirdPerson(!thirdPerson);
        if (Input.GetKeyDown(KeyCode.Escape)) SetCapture(false);
        else if (Input.GetMouseButtonDown(0)) SetCapture(true);
        if (Cursor.lockState != CursorLockMode.Locked) { Move(Vector2.zero, false, Time.deltaTime); return; }
        Look(new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")), Time.deltaTime);
        float x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0)
                - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
        float z = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0)
                - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
        if (Input.GetKeyDown(KeyCode.Space)) Jump();
        Move(new Vector2(x, z), Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift), Time.deltaTime);
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
        var velocity = (transform.right * input.x + transform.forward * input.y) * (IsCrawling ? 2.4f : sprint ? runSpeed : walkSpeed);
        motor.Move((velocity + Vector3.up * verticalSpeed) * dt);
        // Physical perimeter walls are primary; this catches teleport/fall errors.
        if (Mathf.Abs(transform.position.x) > boundary + 4 || Mathf.Abs(transform.position.z) > boundary + 4 || transform.position.y < minimumY)
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
        else viewCamera.transform.localPosition = eyeOffset + (IsCrawling ? Vector3.down * .8f : Vector3.zero);
        if (snowfall != null)
        {
            snowfall.transform.position = transform.position + new Vector3(-5, 9, 8);
            snowfall.transform.rotation = Quaternion.identity;
        }
    }
    public void SetThirdPerson(bool value)
    {
        if (value && IsCrawling) return;
        thirdPerson = value;
        if (visual != null)
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = value ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
    }
    public bool Jump()
    {
        if (InputBlocked || IsCrawling || !motor.isGrounded) return false;
        verticalSpeed = Mathf.Sqrt(40f * jumpHeight);
        return true;
    }
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        motor.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        spawn = position;
        verticalSpeed = pitch = 0;
        smoothLook = Vector2.zero;
        viewCamera.transform.localRotation = Quaternion.identity;
        motor.enabled = true;
    }
    public void SetCrawling(bool value)
    {
        IsCrawling = value;
        motor.height = value ? 1.05f : standingHeight;
        motor.center = value ? new Vector3(standingCenter.x, .525f, standingCenter.z) : standingCenter;
        if (value) SetThirdPerson(false);
    }
    void OnDisable() { SetCapture(false); }
}

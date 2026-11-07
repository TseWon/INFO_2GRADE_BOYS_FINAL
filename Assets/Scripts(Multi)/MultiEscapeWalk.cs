using UnityEngine;

namespace FacilityEscape
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class MultiEscapeWalk : MonoBehaviour
    {
        [SerializeField] private MultiEscapeGame game;
        [SerializeField] private MultiEscapePlayer player;
        [SerializeField] private Transform standUpPoint;
        [SerializeField] private float speed = 2.5f;
        private CharacterController motor;
        private Vector3 seatedPosition;
        private EscapeStage previous = EscapeStage.Ready;
        private float verticalSpeed;
        public float Stamina { get; private set; } = 1;
        public bool Sprinting { get; private set; }
        public bool Exhausted { get; private set; }
        public bool IsClimbing { get; private set; }
        public const float ClimbDuration = 1.4f;
        private Vector3 climbStart, climbEnd;
        private float climbTime;

        private void Awake()
        {
            motor = GetComponent<CharacterController>();
            seatedPosition = transform.position;
            motor.enabled = false;
        }
        private void OnEnable()
        {
            if (game != null)
            {
                game.StateChanged += Refresh;
                game.onRoundStarted.AddListener(ResetPose);
            }
        }
        private void OnDisable()
        {
            if (game != null)
            {
                game.StateChanged -= Refresh;
                game.onRoundStarted.RemoveListener(ResetPose);
            }
            if (motor != null) motor.enabled = false;
            IsClimbing = false;
        }
        private void Refresh()
        {
            if (game == null || game.Rules == null) return;
            var stage = game.Rules.Stage;
            if (stage == EscapeStage.Escaped && previous != EscapeStage.Escaped)
            {
                motor.enabled = false;
                if (standUpPoint != null) transform.position = standUpPoint.position;
                motor.enabled = true;
            }
            previous = stage;
        }
        private void ResetPose()
        {
            motor.enabled = false;
            transform.position = seatedPosition;
            verticalSpeed = 0;
            Stamina = 1;
            Exhausted = IsClimbing = Sprinting = false;
            previous = EscapeStage.Ready;
            if (player != null) player.ResetView();
        }
        private void Update()
        {
            Sprinting = false;
            if (IsClimbing)
            {
                if (!game.CanExplore) return;
                climbTime += Time.deltaTime;
                transform.position = ClimbPosition(climbStart, climbEnd, climbTime / ClimbDuration);
                if (climbTime >= ClimbDuration)
                {
                    IsClimbing = false;
                    motor.enabled = player.LocalInput;
                }
                return;
            }
            if (!motor.enabled || player == null || !player.LocalInput || player.ViewCamera == null ||
                !game.CanExplore || Cursor.lockState != CursorLockMode.Locked ||
                (game.Campaign != null && game.Campaign.Rules.Swarming)) return;
#if ENABLE_LEGACY_INPUT_MANAGER
            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            input = Vector2.ClampMagnitude(input, 1);
            TickStamina(Time.deltaTime, input.sqrMagnitude > .01f && Input.GetKey(KeyCode.LeftShift));
            Vector3 forward = Vector3.ProjectOnPlane(player.ViewCamera.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            if (motor.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed = Mathf.Max(-20, verticalSpeed - 20 * Time.deltaTime);
            motor.Move(((forward * input.y + right * input.x) * (Sprinting ? speed * 1.85f : speed) + Vector3.up * verticalSpeed) * Time.deltaTime);
#endif
        }
        public void Warp(Vector3 position)
        {
            IsClimbing = false;
            motor.enabled = false;
            transform.position = position;
            verticalSpeed = 0;
            motor.enabled = player.LocalInput;
        }
        public void BeginClimb(Vector3 destination)
        {
            if (IsClimbing) return;
            climbStart = transform.position; climbEnd = destination; climbTime = 0;
            IsClimbing = true; Sprinting = false; verticalSpeed = 0; motor.enabled = false;
        }
        public static Vector3 ClimbPosition(Vector3 from, Vector3 to, float progress)
        {
            float top = Mathf.Max(from.y, to.y) + 2.85f;
            Vector3 up = new Vector3(from.x, top, from.z);
            Vector3 across = new Vector3(to.x, top, to.z);
            if (progress < .35f) return Vector3.Lerp(from, up, Mathf.SmoothStep(0, 1, progress / .35f));
            if (progress < .7f) return Vector3.Lerp(up, across, Mathf.SmoothStep(0, 1, (progress - .35f) / .35f));
            return Vector3.Lerp(across, to, Mathf.SmoothStep(0, 1, (progress - .7f) / .3f));
        }
        public void TickStamina(float dt, bool wantsSprint)
        {
            if (Exhausted && Stamina >= .3f) Exhausted = false;
            Sprinting = wantsSprint && !Exhausted && Stamina > 0;
            Stamina = Mathf.Clamp01(Stamina + Mathf.Max(0, dt) * (Sprinting ? -.18f : .12f));
            if (Stamina <= 0) { Exhausted = true; Sprinting = false; }
        }
    }
}

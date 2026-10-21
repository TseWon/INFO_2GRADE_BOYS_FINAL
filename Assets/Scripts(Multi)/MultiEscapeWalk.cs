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
            if (player != null) player.ResetView();
        }
        private void Update()
        {
            if (!motor.enabled || player == null || !player.LocalInput || player.ViewCamera == null) return;
#if ENABLE_LEGACY_INPUT_MANAGER
            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            input = Vector2.ClampMagnitude(input, 1);
            Vector3 forward = Vector3.ProjectOnPlane(player.ViewCamera.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            if (motor.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed = Mathf.Max(-20, verticalSpeed - 20 * Time.deltaTime);
            motor.Move(((forward * input.y + right * input.x) * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
#endif
        }
    }
}

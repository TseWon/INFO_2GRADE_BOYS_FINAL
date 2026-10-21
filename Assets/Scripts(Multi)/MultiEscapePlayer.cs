using UnityEngine;

namespace FacilityEscape
{
    [DisallowMultipleComponent]
    public sealed class MultiEscapePlayer : MonoBehaviour
    {
        [SerializeField] private MultiEscapeGame game;
        [SerializeField] private EscapeRole role;
        [SerializeField] private Camera viewCamera;
        [Tooltip("B만 보는 안내판 전용 레이어. 안내판과 동일하게 설정하세요.")]
        [SerializeField, Range(8, 31)] private int clueLayer = 30;
        [Tooltip("이 컴퓨터에서 마우스로 조종할 플레이어만 활성화합니다.")]
        [SerializeField] private bool localInput = true;
        [SerializeField, Min(0.1f)] private float interactionDistance = 3;
        [SerializeField] private LayerMask interactionMask = ~0;
        [SerializeField, Min(0)] private float lookSensitivity = 2;
        [SerializeField, Range(1, 89)] private float verticalLookLimit = 80;
        [Tooltip("이동 스크립트만 등록. 플레이어/Game/카메라 스크립트는 넣지 마세요.")]
        [SerializeField] private Behaviour[] movementWhileFree = new Behaviour[0];

        public EscapeRole Role => role;
        public MultiEscapeGame Game => game;
        public Camera ViewCamera => viewCamera;
        public bool LocalInput { get => localInput; set => localInput = value; }
        public System.Func<bool> IsPointerBlocked { get; set; }
        private bool[] originalMovementEnabled;
        private Quaternion initialCameraRotation;
        private Vector2 look;
        private int originalCullingMask;
        private MultiGrayscaleCamera grayscale;
        private bool originalGrayscaleEnabled;
        private int ignoreMouseThroughFrame;
        public Ray InteractionRay => viewCamera.ViewportPointToRay(new Vector3(.5f, .5f, 0));

        private void Awake()
        {
            originalMovementEnabled = new bool[movementWhileFree.Length];
            for (int i = 0; i < movementWhileFree.Length; i++)
                originalMovementEnabled[i] = movementWhileFree[i] != null && movementWhileFree[i].enabled;
            if (viewCamera == null)
            {
                Debug.LogError("플레이어 전용 View Camera를 연결하세요.", this);
                return;
            }
            originalCullingMask = viewCamera.cullingMask;
            initialCameraRotation = viewCamera.transform.localRotation;
            grayscale = viewCamera.GetComponent<MultiGrayscaleCamera>();
            originalGrayscaleEnabled = grayscale != null && grayscale.enabled;
            if (role == EscapeRole.Blindfolded && grayscale == null)
            {
                grayscale = viewCamera.gameObject.AddComponent<MultiGrayscaleCamera>();
                originalGrayscaleEnabled = false;
            }
        }

        private void OnEnable()
        {
            if (viewCamera != null)
            {
                viewCamera.cullingMask = role == EscapeRole.Blindfolded
                    ? originalCullingMask & ~(1 << clueLayer) : originalCullingMask | (1 << clueLayer);
                if (grayscale != null) grayscale.enabled = role == EscapeRole.Blindfolded;
            }
            if (game != null) game.StateChanged += RefreshRestraints;
            RefreshRestraints();
        }

        private void OnDisable()
        {
            if (game != null) game.StateChanged -= RefreshRestraints;
            if (viewCamera != null) viewCamera.cullingMask = originalCullingMask;
            if (grayscale != null) grayscale.enabled = originalGrayscaleEnabled;
            for (int i = 0; i < movementWhileFree.Length; i++)
                if (movementWhileFree[i] != null) movementWhileFree[i].enabled = originalMovementEnabled[i];
        }

        private void RefreshRestraints()
        {
            bool free = game != null && game.Rules != null && game.Rules.Stage == EscapeStage.Escaped;
            for (int i = 0; i < movementWhileFree.Length; i++)
                if (movementWhileFree[i] != null)
                    movementWhileFree[i].enabled = free && originalMovementEnabled[i];
        }

        private void Update()
        {
            if (!localInput || viewCamera == null || !viewCamera.isActiveAndEnabled ||
                game == null || game.Rules == null ||
                (!game.Rules.IsRunning && game.Rules.Stage != EscapeStage.Escaped)) return;
            // 커서를 고정한 상태에서 마우스로 시점을 돌리고 화면 중앙으로 조작합니다.
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Cursor.lockState != CursorLockMode.Locked || Time.frameCount <= ignoreMouseThroughFrame) return;
            Look(new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")));
            if (Input.GetMouseButtonDown(0)) TryInteract(InteractionRay);
#endif
        }

        public void SuppressMouseForTwoFrames() => ignoreMouseThroughFrame = Time.frameCount + 2;

        public void ResetView()
        {
            look = Vector2.zero;
            SuppressMouseForTwoFrames();
            if (viewCamera != null) viewCamera.transform.localRotation = initialCameraRotation;
        }

        public void Look(Vector2 delta)
        {
            if (viewCamera == null) return;
            look += delta * lookSensitivity;
            look.x = Mathf.Repeat(look.x + 180, 360) - 180;
            Vector3 initial = initialCameraRotation.eulerAngles;
            float initialPitch = Mathf.DeltaAngle(0, initial.x);
            float pitch = Mathf.Clamp(initialPitch - look.y, -verticalLookLimit, verticalLookLimit);
            look.y = initialPitch - pitch;
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, initial.y + look.x, 0);
        }

        public void TryInteract(Ray ray)
        {
            if (game == null || !game.IsOperator(this)) return;
            // 레이는 벽 등 가장 가까운 Collider에서 멈추므로 가려진 장치를 조작할 수 없습니다.
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionMask, QueryTriggerInteraction.Ignore))
            {
                var target = hit.collider.GetComponentInParent<MultiEscapeInteractable>();
                if (target != null) target.Interact(this);
            }
        }
    }
}

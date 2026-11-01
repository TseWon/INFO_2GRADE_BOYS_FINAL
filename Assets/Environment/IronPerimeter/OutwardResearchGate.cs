using UnityEngine;

public class OutwardResearchGate : MonoBehaviour
{
    [SerializeField] Transform leftLeaf;
    [SerializeField] Transform rightLeaf;
    [SerializeField] float interactDistance = 4.0f;
    [SerializeField] float swingDegrees = 100f;
    [SerializeField] float swingSpeed = 100f;
    [SerializeField] bool open;

    public bool IsOpen => open;
    public void Configure(Transform left, Transform right)
    {
        leftLeaf = left;
        rightLeaf = right;
    }

    void Update()
    {
        if (leftLeaf == null || rightLeaf == null) return;
        Camera view = Camera.main;
        if (view != null && Input.GetKeyDown(KeyCode.E))
        {
            // The hinge is reached from either side, but the leaves always swing toward the road.
            Vector3 local = transform.InverseTransformPoint(view.transform.position);
            if (Mathf.Abs(local.x) < 4.8f && Mathf.Abs(local.z) < interactDistance)
                open = !open;
        }
        float target = open ? swingDegrees : 0f;
        leftLeaf.localRotation = Quaternion.RotateTowards(leftLeaf.localRotation,
            Quaternion.Euler(0f, target, 0f), swingSpeed * Time.deltaTime);
        rightLeaf.localRotation = Quaternion.RotateTowards(rightLeaf.localRotation,
            Quaternion.Euler(0f, -target, 0f), swingSpeed * Time.deltaTime);
    }

    public void SetOpen(bool value) => open = value;

    void OnGUI()
    {
        Camera view = Camera.main;
        if (view == null) return;
        Vector3 local = transform.InverseTransformPoint(view.transform.position);
        if (Mathf.Abs(local.x) >= 4.8f || Mathf.Abs(local.z) >= interactDistance) return;
        var rect = new Rect(Screen.width * .5f - 70f, Screen.height * .72f, 180f, 28f);
        GUI.Label(rect, open ? "E  Close gate" : "E  Open gate");
    }
}

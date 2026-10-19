using UnityEngine;

public class SkinnedRigFollow : MonoBehaviour
{
    [System.Serializable]
    public class Arm
    {
        public Transform upper;
        public Transform lower;
        public Transform hand;
        public Transform target;
        public Vector3 elbowDirection = new Vector3(1f, -1f, -0.5f);

        internal Quaternion upperRest, lowerRest;
    }

    [Header("Rig de físicas")]
    [SerializeField] Rigidbody movement;
    [SerializeField] Rigidbody head;
    [SerializeField] Rigidbody body;

    [Header("Esqueleto")]
    [SerializeField] Transform spine;
    [SerializeField] Arm leftArm = new Arm { elbowDirection = new Vector3(-1f, -1f, -0.5f) };
    [SerializeField] Arm rightArm = new Arm();

    [SerializeField, Range(0f, 3f)] float lean = 1f;

    [SerializeField, Range(0f, 1f)] float twist = 1f;

    Vector3 rootOffset;
    Quaternion rootRotation;
    Vector3 restLean;
    Quaternion spineRest;

    void Awake()
    {
        Quaternion inverse = Quaternion.Inverse(movement.rotation);
        rootOffset = inverse * (transform.position - movement.position);
        rootRotation = inverse * transform.rotation;
        restLean = inverse * (head.position - body.position);
        spineRest = spine.localRotation;
        Store(leftArm);
        Store(rightArm);

        foreach (var rb in movement.transform.parent.GetComponentsInChildren<Rigidbody>())
            rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    static void Store(Arm arm)
    {
        arm.upperRest = arm.upper.localRotation;
        arm.lowerRest = arm.lower.localRotation;
    }

    void LateUpdate()
    {
        Transform leader = movement.transform;
        transform.SetPositionAndRotation(leader.position +leader.rotation * rootOffset, leader.rotation * rootRotation);

        spine.localRotation = spineRest;
        Vector3 currentLean = head.transform.position - body.transform.position;
        Quaternion bend = Quaternion.FromToRotation(leader.rotation * restLean, currentLean);
        bend = Quaternion.SlerpUnclamped(Quaternion.identity, bend, lean);
        float yawLag = Mathf.DeltaAngle(leader.eulerAngles.y, body.transform.eulerAngles.y) * twist;
        spine.rotation = bend * Quaternion.AngleAxis(yawLag, Vector3.up) * spine.rotation;

        Solve(leftArm);
        Solve(rightArm);
    }

    void Solve(Arm arm)
    {
        arm.upper.localRotation = arm.upperRest;
        arm.lower.localRotation = arm.lowerRest;

        Vector3 a = arm.upper.position;
        float upperLength = Vector3.Distance(a, arm.lower.position);
        float lowerLength = Vector3.Distance(arm.lower.position, arm.hand.position);

        Vector3 toTarget = arm.target.position - a;
        float distance = Mathf.Clamp(toTarget.magnitude, 0.001f, (upperLength + lowerLength) * 0.999f);
        Vector3 direction = toTarget.normalized;

        float cos = (upperLength * upperLength + distance * distance - lowerLength * lowerLength) / (2f * upperLength * distance);
        float angle = Mathf.Acos(Mathf.Clamp(cos, -1f, 1f));

        Vector3 pole = Vector3.ProjectOnPlane(transform.rotation * arm.elbowDirection, direction).normalized;
        Vector3 elbow = a + (direction * Mathf.Cos(angle) + pole * Mathf.Sin(angle)) * upperLength;

        arm.upper.rotation = Quaternion.FromToRotation(arm.lower.position - a, elbow - a) * arm.upper.rotation;
        Vector3 wrist = a + direction * distance;
        arm.lower.rotation = Quaternion.FromToRotation(arm.hand.position - arm.lower.position, wrist - arm.lower.position) * arm.lower.rotation;
    }
}

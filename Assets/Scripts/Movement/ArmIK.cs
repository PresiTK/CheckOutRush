using UnityEngine;

public class ArmIK : MonoBehaviour
{
    [SerializeField] Transform upperArm;
    [SerializeField] Transform forearm;
    [SerializeField] Transform hand;

    [SerializeField] Transform target;

    [SerializeField] Transform pole;

    [SerializeField] Vector3 restElbowDirection = Vector3.back;

    Quaternion upperOffset, forearmOffset;
    float upperLength, forearmLength;

    void Awake()
    {
        upperLength = Vector3.Distance(upperArm.position, forearm.position);
        forearmLength = Vector3.Distance(forearm.position, hand.position);

        Vector3 elbowDirection = transform.TransformDirection(restElbowDirection);
        upperOffset = Quaternion.Inverse(Basis(forearm.position - upperArm.position, elbowDirection)) * upperArm.rotation;
        forearmOffset = Quaternion.Inverse(Basis(hand.position - forearm.position, elbowDirection)) * forearm.rotation;
    }

    void LateUpdate()
    {
        Vector3 shoulder = upperArm.position;
        Vector3 toTarget = target.position - shoulder;
        if (toTarget.sqrMagnitude < 0.000001f) return;

        float reach = Mathf.Clamp(toTarget.magnitude, Mathf.Abs(upperLength - forearmLength) + 0.001f, upperLength + forearmLength - 0.001f);
        Vector3 direction = toTarget.normalized;

        Vector3 bend = Vector3.ProjectOnPlane(pole.position - shoulder, direction);
        if (bend.sqrMagnitude < 0.000001f) bend = Vector3.ProjectOnPlane(transform.TransformDirection(restElbowDirection), direction);
        bend.Normalize();

        float cos = (upperLength * upperLength + reach * reach - forearmLength * forearmLength) / (2f * upperLength * reach);
        float sin = Mathf.Sqrt(Mathf.Max(0f, 1f - cos * cos));
        Vector3 elbow = shoulder + upperLength * (direction * cos + bend * sin);
        Vector3 wrist = shoulder + direction * reach;

        upperArm.rotation = Basis(elbow - shoulder, bend) * upperOffset;
        forearm.rotation = Basis(wrist - elbow, bend) * forearmOffset;
    }

    static Quaternion Basis(Vector3 along, Vector3 elbowDirection) => Quaternion.LookRotation(along, elbowDirection);

    void OnDrawGizmosSelected()
    {
        if (target == null || pole == null || forearm == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(forearm.position, pole.position);
        Gizmos.DrawWireSphere(pole.position, 0.05f);
    }
}

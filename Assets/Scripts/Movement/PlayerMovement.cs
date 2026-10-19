using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] float maxSpeed = 5f;

    [SerializeField, Min(0f)] float acceleration = 12f;

    [SerializeField, Min(0f)] float deceleration = 15f;

    [SerializeField, Min(0f)] float turnSpeed = 150f;

    [SerializeField, Range(1f, 180f)] float walkWithinAngle = 60f;

    Rigidbody rb;
    NavMeshPath path;
    Vector3[] corners = new Vector3[0];
    int nextCorner;
    float speed;
    float heightOffset;
    float distanceLimit = float.PositiveInfinity;

    public Transform Character => transform.parent != null ? transform.parent : transform;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        path = new NavMeshPath();

        var ball = GetComponent<Collider>();
        if (ball != null)
            foreach (var other in Character.GetComponentsInChildren<Collider>())
                if (other != ball) Physics.IgnoreCollision(ball, other);
    }

    public bool MoveTo(Vector3 worldPoint)
    {
        if (!NavMesh.SamplePosition(worldPoint, out NavMeshHit target, 1f, NavMesh.AllAreas)) return false;
        if (!NavMesh.SamplePosition(rb.position, out NavMeshHit start, 2f, NavMesh.AllAreas)) return false;
        if (!NavMesh.CalculatePath(start.position, target.position, NavMesh.AllAreas, path)) return false;

        heightOffset = rb.position.y - start.position.y;
        corners = path.corners;
        nextCorner = 1;
        distanceLimit = float.PositiveInfinity;
        return true;
    }

    public void Stop() => nextCorner = corners.Length;

    public void StopWithin(float distance) => distanceLimit = Mathf.Max(0f, distance);

    public bool IsMoving => nextCorner < corners.Length;

    public float SpeedFraction => maxSpeed > 0f ? speed / maxSpeed : 0f;

    public void StopIfBlocked(Vector3 normal)
    {
        if (nextCorner >= corners.Length) return;

        Vector3 heading = Vector3.ProjectOnPlane(corners[nextCorner] - rb.position, Vector3.up);
        if (Vector3.Dot(heading.normalized, normal) < -0.1f) Stop();
    }

    void FixedUpdate()
    {
        if (nextCorner >= corners.Length)
        {
            speed = 0f;
            return;
        }

        Vector3 target = corners[nextCorner] + Vector3.up * heightOffset;
        Vector3 toTarget = target - rb.position;
        Vector3 flat = Vector3.ProjectOnPlane(toTarget, Vector3.up);

        float angle = 0f;
        if (flat.sqrMagnitude > 0.0001f)
        {
            angle = Vector3.Angle(rb.rotation * Vector3.forward, flat);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, Quaternion.LookRotation(flat), turnSpeed * Time.fixedDeltaTime));
        }

        float facing = Mathf.Clamp01(1f - angle / walkWithinAngle);
        float stopDistance = Mathf.Max(0f, Mathf.Min(RemainingDistance(toTarget.magnitude), distanceLimit - speed * Time.fixedDeltaTime));
        float brakeSpeed = Mathf.Sqrt(2f * deceleration * stopDistance);
        float targetSpeed = Mathf.Min(maxSpeed * facing, brakeSpeed);
        speed = Mathf.MoveTowards(speed, targetSpeed, (targetSpeed > speed ? acceleration : deceleration) * Time.fixedDeltaTime);

        float step = speed * Time.fixedDeltaTime;
        distanceLimit = Mathf.Max(0f, distanceLimit - step);
        if (step >= toTarget.magnitude)
        {
            rb.MovePosition(target);
            nextCorner++;
        }
        else
        {
            rb.MovePosition(rb.position + toTarget.normalized * step);
        }
    }

    float RemainingDistance(float toNextCorner)
    {
        float distance = toNextCorner;
        for (int i = nextCorner + 1; i < corners.Length; i++)
            distance += Vector3.Distance(corners[i - 1], corners[i]);
        return distance;
    }
}

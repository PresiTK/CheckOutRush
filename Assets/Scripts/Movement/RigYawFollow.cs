using System.Collections.Generic;
using UnityEngine;

public class RigYawFollow : MonoBehaviour
{
    [SerializeField] Rigidbody leader;

    [SerializeField, Min(0f)] float followSpeed = 30f;

    readonly List<Rigidbody> parts = new List<Rigidbody>();

    void Awake()
    {
        var leaderJoints = leader.GetComponents<Joint>();
        foreach (var body in GetComponentsInChildren<Rigidbody>())
        {
            if (body == leader || body.isKinematic || IsFixedTo(body, leaderJoints)) continue;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            parts.Add(body);
        }
    }

    void FixedUpdate()
    {
        float yaw = leader.rotation.eulerAngles.y;
        foreach (var body in parts)
        {
            float error = Mathf.DeltaAngle(body.rotation.eulerAngles.y, yaw);
            body.angularVelocity = new Vector3(0f, error * Mathf.Deg2Rad * followSpeed, 0f);
        }
    }

    static bool IsFixedTo(Rigidbody body, Joint[] leaderJoints)
    {
        foreach (var joint in leaderJoints)
            if (joint.connectedBody == body) return true;
        return false;
    }
}

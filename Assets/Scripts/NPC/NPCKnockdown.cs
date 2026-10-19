using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPCKnockdown : MonoBehaviour
{
    [SerializeField, Min(0f)] float recoverTime = 3f;

    [SerializeField, Min(0f)] float immunityTime = 1.5f;

    [SerializeField, Min(0f)] float pushSpeed = 2f;

    [SerializeField] Rigidbody cart;

    [Header("Sonido al caer")]
    [SerializeField] AudioClip knockClip;
    [SerializeField] Vector2 knockPitch = new Vector2(1.6f, 2.2f);
    [SerializeField, Range(0f, 1f)] float knockVolume = 1f;

    struct JointInfo
    {
        public Rigidbody Body;
        public Rigidbody Connected;
        public Vector3 Anchor;
        public Vector3 ConnectedAnchor;
    }

    struct PartPose
    {
        public Rigidbody Body;
        public Vector3 Position;
        public Quaternion Rotation;
        public RigidbodyConstraints Constraints;
    }

    readonly List<PartPose> poses = new List<PartPose>();
    readonly List<JointInfo> brokenJoints = new List<JointInfo>();

    NavMeshAgent agent;
    NPCWander wander;
    RigYawFollow yawFollow;
    Rigidbody movement;
    bool knockedDown;
    float immuneUntil;

    void Awake()
    {
        agent = GetComponentInChildren<NavMeshAgent>();
        wander = GetComponentInChildren<NPCWander>();
        yawFollow = GetComponent<RigYawFollow>();
        movement = agent.GetComponent<Rigidbody>();

        if (cart == null)
            foreach (var body in GetComponentsInChildren<Rigidbody>())
                if (body.name == "Cart") cart = body;

        foreach (var body in GetComponentsInChildren<Rigidbody>())
            body.gameObject.AddComponent<NPCHitRelay>().Owner = this;

        var ball = movement.GetComponent<Collider>();
        if (ball != null)
            foreach (var other in GetComponentsInChildren<Collider>())
                if (other != ball) Physics.IgnoreCollision(ball, other);
    }

    void Start()
    {
        Quaternion toLocal = Quaternion.Inverse(movement.rotation);
        foreach (var body in GetComponentsInChildren<Rigidbody>())
        {
            if (body == movement) continue;
            poses.Add(new PartPose
            {
                Body = body,
                Position = toLocal * (body.position - movement.position),
                Rotation = toLocal * body.rotation,
            });
        }
    }

    public void OnPartHit(Collision collision)
    {
        if (knockedDown || Time.time < immuneUntil) return;

        var player = collision.collider.transform.root.GetComponentInChildren<PlayerMovement>();
        if (player == null) return;

        Vector3 push = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
        if (knockClip != null) SoundManager.Instance.PlaySfx(knockClip, knockVolume, Random.Range(knockPitch.x, knockPitch.y));
        StartCoroutine(KnockDown(push));
    }

    IEnumerator KnockDown(Vector3 push)
    {
        knockedDown = true;

        wander.enabled = false;
        agent.enabled = false;
        yawFollow.enabled = false;

        for (int i = 0; i < poses.Count; i++)
        {
            var pose = poses[i];
            pose.Constraints = pose.Body.constraints;
            pose.Body.constraints = RigidbodyConstraints.None;
            poses[i] = pose;
        }

        brokenJoints.Clear();
        foreach (var joint in GetComponentsInChildren<FixedJoint>())
        {
            if (joint.connectedBody != cart && joint.GetComponent<Rigidbody>() != cart) continue;
            brokenJoints.Add(new JointInfo
            {
                Body = joint.GetComponent<Rigidbody>(),
                Connected = joint.connectedBody,
                Anchor = joint.anchor,
                ConnectedAnchor = joint.connectedAnchor,
            });
            Destroy(joint);
        }

        movement.isKinematic = false;

        foreach (var body in GetComponentsInChildren<Rigidbody>())
        {
            body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
            if (body != movement) body.AddForce(push * pushSpeed, ForceMode.VelocityChange);
        }

        yield return new WaitForSeconds(recoverTime);

        StandUp();
        knockedDown = false;
        immuneUntil = Time.time + immunityTime;
    }

    void StandUp()
    {
        if (!NavMesh.SamplePosition(movement.position, out NavMeshHit spot, 5f, agent.areaMask))
            spot.position = movement.position;
        Quaternion upright = Quaternion.Euler(0f, movement.rotation.eulerAngles.y, 0f);

        movement.isKinematic = true;
        movement.transform.SetPositionAndRotation(spot.position + Vector3.up * agent.baseOffset, upright);
        agent.enabled = true;
        movement.position = movement.transform.position;
        movement.rotation = upright;

        foreach (var pose in poses)
        {
            var body = pose.Body;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = movement.position + upright * pose.Position;
            body.rotation = upright * pose.Rotation;
            body.transform.SetPositionAndRotation(body.position, body.rotation);
            body.constraints = pose.Constraints;
        }
        Physics.SyncTransforms();

        foreach (var info in brokenJoints)
        {
            var joint = info.Body.gameObject.AddComponent<FixedJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = info.Anchor;
            joint.connectedAnchor = info.ConnectedAnchor;
            joint.connectedBody = info.Connected;
        }
        brokenJoints.Clear();

        yawFollow.enabled = true;
        wander.enabled = true;
    }
}

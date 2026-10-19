using UnityEngine;

public class NPCHitRelay : MonoBehaviour
{
    public NPCKnockdown Owner;

    void OnCollisionEnter(Collision collision) => Owner.OnPartHit(collision);
}

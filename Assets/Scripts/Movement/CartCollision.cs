using UnityEngine;

public class CartCollision : MonoBehaviour
{
    public PlayerMovement playerMovement;

    [SerializeField] AudioClip hitClip;
    [SerializeField, Range(0f, 1f)] float hitVolume = 1f;

    [SerializeField, Min(0f)] float minHitSpeed = 0.5f;

    [SerializeField, Min(0f)] float hitCooldown = 0.3f;

    float nextHitTime;

    void OnCollisionEnter(Collision collision)
    {
        playerMovement.StopIfBlocked(collision.GetContact(0).normal);
        PlayHit(collision);
    }

    void OnCollisionStay(Collision collision) => playerMovement.StopIfBlocked(collision.GetContact(0).normal);

    void PlayHit(Collision collision)
    {
        if (hitClip == null || Time.time < nextHitTime) return;
        if (collision.transform.IsChildOf(playerMovement.Character)) return;
        if (collision.relativeVelocity.magnitude < minHitSpeed) return;

        nextHitTime = Time.time + hitCooldown;
        SoundManager.Instance.PlaySfx(hitClip, hitVolume);
    }
}

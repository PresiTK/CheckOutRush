using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] PlayerMovement player;
    [SerializeField] Camera clickCamera;
    [SerializeField] ParticleSystem clickEffect;

    [SerializeField] LayerMask interactLayers;

    [SerializeField, Min(0f)] float facingRayLength = 3f;

    [Header("Cursor")]
    [SerializeField] Texture2D clickCursor;
    [SerializeField] Vector2 clickHotspot;
    [SerializeField] Texture2D grabCursor;
    [SerializeField] Vector2 grabHotspot;

    bool showingGrab;

    readonly RaycastHit[] hits = new RaycastHit[16];
    readonly List<Collider> colliders = new List<Collider>();

    LookZone lookZone;

    enum ZoneAction { Look, Use }
    LookZone pendingZone;
    ZoneAction pendingAction;
    bool lookShown;
    Rigidbody body;
    Bounds footprint;

    InputSystem_Actions.PlayerActions Actions => InputManager.Instance.InputMap.Player;

    Matrix4x4 PlayerPose => Matrix4x4.TRS(body.position, body.rotation, Vector3.one);

    void Reset() => interactLayers = LayerMask.GetMask("Interact");

    void Awake()
    {
        if (player == null) player = GetComponent<PlayerMovement>();
        if (clickCamera == null) clickCamera = Camera.main;
        body = player.GetComponent<Rigidbody>();
        footprint = MeasureFootprint();
        SetCursor(false);
    }

    void OnDisable() => SetCursor(false);

    void Update()
    {
        UpdateCursor();
        if (Actions.Grab.WasPressedThisFrame() && TryGetClickHit(out RaycastHit leftHit)) OnLeftClick(leftHit);
        if (Actions.MoveLook.WasPressedThisFrame() && TryGetClickHit(out RaycastHit rightHit)) OnRightClick(rightHit);
        UpdatePending();
        UpdateLook();

        Debug.DrawRay(FacingRayOrigin, body.rotation * Vector3.forward * (facingRayLength + FacingRayBack), IsFacingInteractable(out _) ? Color.red : Color.green);
    }

    const float FacingRayBack = 0.5f;

    Vector3 FacingRayOrigin => PlayerPose.MultiplyPoint3x4(new Vector3(footprint.center.x, 0f, footprint.max.z - FacingRayBack));

    bool IsFacingInteractable(out RaycastHit hit) =>
        Physics.Raycast(FacingRayOrigin, body.rotation * Vector3.forward, out hit, facingRayLength + FacingRayBack, interactLayers, QueryTriggerInteraction.Ignore);

    void OnLeftClick(RaycastHit hit)
    {
        if (IsInteractable(hit.collider))
        {
            var zone = hit.collider.GetComponentInParent<LookZone>();
            if (zone != null) GoTo(zone, ZoneAction.Use);
        }
    }

    void OnRightClick(RaycastHit hit)
    {
        if (IsInteractable(hit.collider))
        {
            var zone = hit.collider.GetComponentInParent<LookZone>();
            if (zone != null) GoTo(zone, ZoneAction.Look);
        }
        else
        {
            StopLooking();
            MoveTo(hit);
        }
    }

    void GoTo(LookZone zone, ZoneAction action)
    {
        pendingZone = zone;
        pendingAction = action;
        if (zone.PlayerInside) return;

        Vector3 front = zone.Front;
        Vector3 target = new Vector3(front.x, body.position.y, front.z);
        if (NavMesh.SamplePosition(target, out NavMeshHit spot, 2f, NavMesh.AllAreas) && player.MoveTo(spot.position))
            SpawnClickEffect(spot.position, Vector3.up);
        else
            pendingZone = null;
    }

    void UpdatePending()
    {
        if (pendingZone == null) return;

        if (pendingZone.PlayerInside)
        {
            var zone = pendingZone;
            pendingZone = null;
            player.Stop();
            if (pendingAction == ZoneAction.Look) LookAt(zone);
            else Use(zone);
        }
        else if (!player.IsMoving)
        {
            pendingZone = null;
        }
    }

    void Use(LookZone zone)
    {
        Debug.Log("CanUse", zone);
        var product = zone.Product;
        if (product != null && product.IsInRecipe && GameManager.Instance != null)
            GameManager.Instance.Collect(product);
    }

    void MoveTo(RaycastHit hit)
    {
        if (player.MoveTo(hit.point))
            SpawnClickEffect(hit.point, hit.normal);
    }

    void LookAt(LookZone zone)
    {
        if (zone == lookZone) return;
        if (IsFacingInteractable(out RaycastHit facing))
        {
            Debug.Log($"De frente a {facing.collider.name}: hay que ponerse de lado para mirar", facing.collider);
            return;
        }
        StopLooking();
        player.Stop();
        UIManager.Instance.ShowLookText(zone.Message);
        lookZone = zone;
        lookShown = true;
    }

    void UpdateLook()
    {
        if (lookZone != null && !lookZone.PlayerInside) HideLook();
    }

    void StopLooking()
    {
        HideLook();
        pendingZone = null;
    }

    void HideLook()
    {
        if (lookShown) UIManager.Instance.HideLookText();
        lookZone = null;
        lookShown = false;
    }

    Bounds MeasureFootprint()
    {
        Matrix4x4 toLocal = PlayerPose.inverse;
        var rest = new Bounds(Vector3.zero, Vector3.zero);
        player.Character.GetComponentsInChildren(colliders);
        foreach (var collider in colliders)
        {
            if (!collider.enabled || collider.isTrigger) continue;
            for (int i = 0; i < 4; i++) rest.Encapsulate(toLocal.MultiplyPoint3x4(Corner(collider.bounds, i)));
        }
        return rest;
    }

    static Vector3 Corner(Bounds box, int i) => new Vector3(i % 2 == 0 ? box.min.x : box.max.x, box.center.y, i < 2 ? box.min.z : box.max.z);

    void Interact(GameObject target)
    {
        Debug.Log($"Interactuar con {target.name}", target);
    }

    bool IsInteractable(Collider collider) => (interactLayers.value & (1 << collider.gameObject.layer)) != 0;

    void UpdateCursor()
    {
        bool overShelf = TryGetClickHit(out RaycastHit hit)
                         && IsInteractable(hit.collider)
                         && hit.collider.GetComponentInParent<LookZone>() != null;
        if (overShelf != showingGrab) SetCursor(overShelf);
    }

    void SetCursor(bool grab)
    {
        showingGrab = grab;
        if (grab && grabCursor != null) Cursor.SetCursor(grabCursor, grabHotspot, CursorMode.Auto);
        else Cursor.SetCursor(clickCursor, clickHotspot, CursorMode.Auto);
    }

    bool TryGetClickHit(out RaycastHit closest)
    {
        closest = default;
        if (Pointer.current == null) return false;

        Ray ray = clickCamera.ScreenPointToRay(Pointer.current.position.ReadValue());
        closest.distance = float.MaxValue;
        int count = Physics.RaycastNonAlloc(ray, hits, 1000f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
            if (hits[i].distance < closest.distance && !hits[i].collider.transform.IsChildOf(player.Character))
                closest = hits[i];
        return closest.collider != null;
    }

    void SpawnClickEffect(Vector3 point, Vector3 normal)
    {
        if (clickEffect == null) return;

        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal) * clickEffect.transform.rotation;
        var effect = Instantiate(clickEffect, point + normal * 0.02f, rotation);

        float lifetime = 0f;
        foreach (var system in effect.GetComponentsInChildren<ParticleSystem>())
            lifetime = Mathf.Max(lifetime, system.main.duration + system.main.startLifetime.constantMax);
        Destroy(effect.gameObject, lifetime);
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// JANUS — VR Input Handler
///
/// Targets: Unity 6000.3.10f1 · XRI 3.3.1 · New Input System 1.18.0
///
/// Uses the same InputActionReferences already configured in the project's
/// "XRI Default Input Actions.inputactions" asset — no duplicate action maps needed.
///
/// NAVIGATION:
///   NearFarInteractor ray + Trigger  →  click UI elements (handled by XRUIInputModule)
///   Menu button (either controller)  →  toggle JANUS menu
///   Head-gaze dwell                  →  fallback activation (no controller required)
///
/// HAPTICS:
///   Hover new element  →  soft 30ms pulse
///   Select / click     →  firmer 55ms pulse
/// </summary>
[RequireComponent(typeof(JANUSMenuManager))]
public class JANUSVRInputHandler : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────────

    [Header("NearFar Interactors (from XR Origin rig)")]
    [SerializeField] private NearFarInteractor leftInteractor;
    [SerializeField] private NearFarInteractor rightInteractor;

    [Header("Menu Toggle — Input Action")]
    [Tooltip("Drag the 'Menu' InputActionReference from XRI Default Input Actions here. " +
             "Maps to Menu button on Quest/OpenXR controllers.")]
    [SerializeField] private InputActionReference leftMenuAction;
    [SerializeField] private InputActionReference rightMenuAction;

    [Header("Gaze Dwell Fallback")]
    [SerializeField] private bool  gazeEnabled  = true;
    [SerializeField] private float dwellSeconds = 1.8f;

    [Header("Haptics")]
    [SerializeField] private bool  hapticsEnabled   = true;
    [SerializeField] private float hoverAmplitude   = 0.04f;
    [SerializeField] private float hoverDuration    = 0.030f;
    [SerializeField] private float selectAmplitude  = 0.22f;
    [SerializeField] private float selectDuration   = 0.055f;

    // ─────────────────────────────────────────────
    // Private
    // ─────────────────────────────────────────────

    private JANUSMenuManager _menu;

    // Gaze dwell
    private GameObject _gazeTarget;
    private float      _gazeTimer;

    // Hover tracking for haptics
    private GameObject _lastHoveredLeft;
    private GameObject _lastHoveredRight;

    // ─────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────

    private void Awake()
    {
        _menu = GetComponent<JANUSMenuManager>();
    }

    private void OnEnable()
    {
        if (leftMenuAction?.action  != null) leftMenuAction.action.performed  += OnMenuToggle;
        if (rightMenuAction?.action != null) rightMenuAction.action.performed += OnMenuToggle;

        leftMenuAction?.action.Enable();
        rightMenuAction?.action.Enable();
    }

    private void OnDisable()
    {
        if (leftMenuAction?.action  != null) leftMenuAction.action.performed  -= OnMenuToggle;
        if (rightMenuAction?.action != null) rightMenuAction.action.performed -= OnMenuToggle;
    }

    private void Update()
    {
        HandleHoverHaptics();
        if (gazeEnabled) HandleGazeDwell();
    }

    // ─────────────────────────────────────────────
    // Menu Toggle
    // ─────────────────────────────────────────────

    private void OnMenuToggle(InputAction.CallbackContext ctx)
    {
        _menu.ToggleVisible();
        TriggerHaptic(rightInteractor, selectAmplitude, selectDuration);
    }

    // ─────────────────────────────────────────────
    // Hover Haptics
    // ─────────────────────────────────────────────

    // XRUIInputModule handles the actual click — we just layer feel on top.
    private void HandleHoverHaptics()
    {
        CheckHover(rightInteractor, ref _lastHoveredRight);
        CheckHover(leftInteractor,  ref _lastHoveredLeft);
    }

    private void CheckHover(NearFarInteractor interactor, ref GameObject last)
    {
        if (interactor == null) return;

        GameObject current = null;
        if (interactor.TryGetCurrentUIRaycastResult(out var result))
            current = result.gameObject;

        if (current != null && current != last)
            TriggerHaptic(interactor, hoverAmplitude, hoverDuration);

        last = current;
    }

    // ─────────────────────────────────────────────
    // Gaze Dwell (hands-free fallback)
    // ─────────────────────────────────────────────

    private void HandleGazeDwell()
    {
        // Only activate gaze if no controller is already hovering UI
        if (_lastHoveredLeft != null || _lastHoveredRight != null)
        {
            _gazeTarget = null;
            _gazeTimer  = 0f;
            return;
        }

        var cam = Camera.main;
        if (cam == null) return;

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, 6f))
        {
            if (hit.collider.gameObject == _gazeTarget)
            {
                _gazeTimer += Time.deltaTime;
                JANUSEvents.OnGazeDwellProgress?.Invoke(_gazeTimer / dwellSeconds);

                if (_gazeTimer >= dwellSeconds)
                {
                    FireClick(hit.collider.gameObject);
                    TriggerHaptic(rightInteractor, selectAmplitude, selectDuration);
                    _gazeTarget = null;
                    _gazeTimer  = 0f;
                }
            }
            else
            {
                _gazeTarget = hit.collider.gameObject;
                _gazeTimer  = 0f;
            }
        }
        else
        {
            _gazeTarget = null;
            _gazeTimer  = 0f;
        }
    }

    private static void FireClick(GameObject target)
    {
        var pointer = new PointerEventData(EventSystem.current);
        ExecuteEvents.ExecuteHierarchy(target, pointer, ExecuteEvents.pointerClickHandler);
    }

    // ─────────────────────────────────────────────
    // Haptics — XRI 3.3.1 approach
    // ─────────────────────────────────────────────

    private void TriggerHaptic(NearFarInteractor interactor, float amplitude, float duration)
    {
        if (!hapticsEnabled || interactor == null) return;

        // XRI 3.x: haptics via IXRHapticFeedback on the interactor's controller
        if (interactor.TryGetComponent<UnityEngine.XR.Interaction.Toolkit.XRBaseController>(out var ctrl))
            ctrl.SendHapticImpulse(amplitude, duration);
    }

    public void SelectHaptic(bool left = false)
        => TriggerHaptic(left ? leftInteractor : rightInteractor, selectAmplitude, selectDuration);
}

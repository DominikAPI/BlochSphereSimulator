using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Orbit camera with the new Input System: hold right mouse button + move mouse = rotate around the target
/// (the origin by default), scroll wheel = zoom. It does not respond over the UI.
/// Put it on the Main Camera.
/// </summary>
public class OrbitCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;          // left empty: the origin

    [Header("Initial state")]
    [SerializeField] private float startYaw = 135f;     
    [SerializeField] private float startPitch = 15f;    // positive = looking from above
    [SerializeField] private float startDistance = 4f;

    [Header("Control")]
    [SerializeField] private float rotateSpeed = 0.2f;  // degrees / pixel
    [SerializeField] private bool invertY = false;
    [SerializeField] private float zoomStep = 0.1f;     // one scroll = 10% distance change
    [SerializeField] private float minDistance = 1.8f;
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private float smoothing = 14f;     // larger = sharper tracking

    private InputAction _hold;
    private InputAction _look;
    private InputAction _zoom;
    private bool _dragging;

    // target values (these are modified by input) and the displayed, smoothed values
    private float _yaw, _pitch, _distance;
    private float _curYaw, _curPitch, _curDistance;

    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();

    private void Awake()
    {
        ResetView();
        _curYaw = _yaw;
        _curPitch = _pitch;
        _curDistance = _distance;
        Apply();
    }

    private void OnEnable()
    {
        _hold = new InputAction("OrbitHold", InputActionType.Button, "<Mouse>/leftButton");
        _look = new InputAction("OrbitLook", InputActionType.Value, "<Mouse>/delta");
        _zoom = new InputAction("OrbitZoom", InputActionType.Value, "<Mouse>/scroll/y");

        _hold.started += OnHoldStarted;
        _hold.canceled += OnHoldCanceled;

        _hold.Enable();
        _look.Enable();
        _zoom.Enable();
    }

    private void OnDisable()
    {
        _hold.started -= OnHoldStarted;
        _hold.canceled -= OnHoldCanceled;
        _hold.Dispose();
        _look.Dispose();
        _zoom.Dispose();
        _dragging = false;
    }

    /// <summary>Restores the initial view (smoothed). You can also bind it to a "Reset View" button.</summary>
    public void ResetView()
    {
        _yaw = startYaw;
        _pitch = startPitch;
        _distance = startDistance;
    }

    private void OnHoldStarted(InputAction.CallbackContext ctx)
    {
        // If the button press happened on a UI element, rotation does not start.
        _dragging = !IsPointerOverUI();
    }

    private void OnHoldCanceled(InputAction.CallbackContext ctx)
    {
        _dragging = false;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Pointer.current != null
                ? Pointer.current.position.ReadValue()
                : (Vector2)Input.mousePosition   // fallback
        };

        _raycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, _raycastResults);
        return _raycastResults.Count > 0;
    }

    private void LateUpdate()
    {
        if (_dragging)
        {
            Vector2 delta = _look.ReadValue<Vector2>();
            // The scene follows the mouse: dragging right turns the sphere right, dragging up turns the front of the sphere upward.
            _yaw += delta.x * rotateSpeed;
            _pitch += (invertY ? delta.y : -delta.y) * rotateSpeed;
            _pitch = Mathf.Clamp(_pitch, -89f, 89f);   // don't flip over at the top/bottom
        }

        float scroll = _zoom.ReadValue<float>();
        if (Mathf.Abs(scroll) > 0.01f && !IsPointerOverUI())
        {
            // We only use the sign: the scroll value's magnitude differs per platform.
            _distance *= 1f - Mathf.Sign(scroll) * zoomStep;
            _distance = Mathf.Clamp(_distance, minDistance, maxDistance);
        }

        float k = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
        _curYaw = Mathf.Lerp(_curYaw, _yaw, k);
        _curPitch = Mathf.Lerp(_curPitch, _pitch, k);
        _curDistance = Mathf.Lerp(_curDistance, _distance, k);
        Apply();
    }

    private void Apply()
    {
        Vector3 center = target != null ? target.position : Vector3.zero;
        Quaternion rotation = Quaternion.Euler(_curPitch, _curYaw, 0f);
        transform.SetPositionAndRotation(center + rotation * new Vector3(0f, 0f, -_curDistance), rotation);
    }
}
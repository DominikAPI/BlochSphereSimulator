using System.Collections.Generic;
using BlochSim.Model;
using UnityEngine;

/// <summary>
/// Displays one state on the sphere: arrow + trail.
/// The active state is fully colored (alpha 1.0), the others are more faded (inactiveAlpha).
/// The arrow's material should be of transparent type, otherwise the alpha won't be visible.
/// </summary>
public class StateMarker : MonoBehaviour
{
    [SerializeField] private ArrowView arrow;
    [SerializeField] private TrailView trail;  
    [SerializeField, Range(0f, 1f)] private float inactiveAlpha = 0.35f;

    private Color _color = Color.white;
    private bool _active = true;

    // <summary>The currently displayed (during animation, the intermediate) Bloch vector.</summary>
    public BlochVector CurrentBloch { get; private set; }

    public void SetBloch(BlochVector b)
    {
        CurrentBloch = b;
        arrow.SetDirection(BlochSpace.ToUnity(b));
    }

    /// <summary>The state's base color (the alpha is given by SetActiveState).</summary>
    public void SetColor(Color color)
    {
        _color = color;
        ApplyAppearance();
    }

    /// <summary>Active state: alpha 1.0, inactive: inactiveAlpha (affects both the arrow and the trail).</summary>
    public void SetActiveState(bool active)
    {
        _active = active;
        ApplyAppearance();
    }

    public void SetTrailPersistent(bool persistent)
    {
        if (trail != null) trail.SetPersistent(persistent);
    }

    /// <summary>The path of completed gates; an empty list clears the history.</summary>
    public void SetTrailHistory(List<Vector3> points)
    {
        if (trail != null) trail.SetHistory(points);
    }

    public void ShowTrailArc(BlochVector start, BlochVector axis, double angle, double progress)
    {
        if (trail != null) trail.ShowArc(start, axis, angle, progress);
    }

    public void FinishTrail()
    {
        if (trail != null) trail.Finish();
    }

    public void ClearTrail()
    {
        if (trail != null) trail.Clear();
    }

    private void ApplyAppearance()
    {
        float a = _active ? 1f : inactiveAlpha;
        arrow.SetColor(new Color(_color.r, _color.g, _color.b, a));
        if (trail != null)
        {
            trail.SetColor(_color);
            trail.SetOpacity(a);
        }
    }
}
using BlochSim.Model;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

/// <summary>
/// Trail behind a state marker: draws the path of the arrow tip on the sphere's surface.
/// There are two modes:
///  - non-persistent: only the arc of the currently running gate is visible, and it fades out at the end of the rotation;
///  - persistent: the entire path so far is visible (the arc of every previous gate), without fading.
/// The arcs are analytical (axis + angle), so they are smooth even at low FPS. The LineRenderer works in local space,
/// so it moves together with the marker.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class TrailView : MonoBehaviour
{
    /// <summary>This many segments per half turn (180°); BlochView also uses this for computing the history.</summary>
    public const int SamplesPerHalfTurn = 48;

    [Header("Appaerance")]
    [SerializeField] private Material material;          
    [SerializeField] private float width = 0.03f;
    [SerializeField, Range(0f, 1f)] private float tailAlpha = 0.05f;            // the start of the line, in fading mode
    [SerializeField, Range(0f, 1f)] private float persistentTailAlpha = 0.3f;   // the oldest part, in persistent mode
    [SerializeField] private int renderQueue = 3100;     // draw after the sphere (3000)

    [Header("Fade-out (only in non-persistent mode)")]
    [SerializeField] private bool fadeOut = true;
    [SerializeField] private float lingerSeconds = 1f;   // stays at full brightness this long after the rotation
    [SerializeField] private float fadeSeconds = 1.5f;

    private LineRenderer _line;
    private Material _instance;
    private Color _color = Color.white;
    private bool _persistent;
    private bool _finished;
    private float _age;
    private float _opacity = 1f;   // the trail of an inactive state is more faded
    private float _fade = 1f;      // the current fade multiplier

    private readonly List<Vector3> _history = new List<Vector3>();   // the path of already completed gates
    private readonly List<Vector3> _scratch = new List<Vector3>();   // history + the currently running arc
    private Vector3[] _buffer = new Vector3[256];

    private void Awake()
    {
        Shader fallback = material == null ? Shader.Find("Sprites/Default") : null;
        if (material == null && fallback == null)
        {
            Debug.LogError("railView: no material assigned, and the Sprites/Default shader cannot be found either", this);
            enabled = false;
            return;
        }
        _instance = material != null ? new Material(material) : new Material(fallback);
        _instance.renderQueue = renderQueue;

        _line = GetComponent<LineRenderer>();
        _line.sharedMaterial = _instance;
        _line.useWorldSpace = false;
        _line.loop = false;
        _line.alignment = LineAlignment.View;
        _line.numCapVertices = 4;
        _line.numCornerVertices = 2;
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows = false;
        _line.widthMultiplier = width;
        _line.positionCount = 0;
        ApplyWidthCurve();
    }

    private void OnDestroy()
    {
        if (_instance != null) Destroy(_instance);
    }

    public void SetColor(Color color)
    {
        _color = color;
        if (_line != null) ApplyColor(_fade);
    }

    /// <summary>General opacity multiplier (0..1), e.g. smaller for an inactive state.</summary>
    public void SetOpacity(float opacity)
    {
        _opacity = Mathf.Clamp01(opacity);
        if (_line != null) ApplyColor(_fade);
    }

    /// <summary>Toggle persistent mode on/off. The history is filled in by BlochView (SetHistory).</summary>
    public void SetPersistent(bool persistent)
    {
        _persistent = persistent;
        if (_line == null) return;
        ApplyWidthCurve();
        ApplyColor(1f);
    }

    /// <summary>The path of completed gates (points in Unity space). Empty list = no history.</summary>
    public void SetHistory(List<Vector3> points)
    {
        _history.Clear();
        if (points != null) _history.AddRange(points);
        _finished = false;
        _age = 0f;
        if (_line == null) return;

        Apply(_history);
        ApplyColor(1f);
    }

    public void ShowArc(BlochVector start, BlochVector axis, double angle, double progress)
    {
        if (_line == null) return;

        _finished = false;
        _age = 0f;

        _scratch.Clear();
        _scratch.AddRange(_history);
        SampleArc(_scratch, start, axis, angle, progress, SamplesPerHalfTurn);
        Apply(_scratch);
        ApplyColor(1f);
    }

    /// <summary>The rotation has ended: in non-persistent mode, the fade-out starts from here.</summary>
    public void Finish()
    {
        if (_persistent) return;
        _finished = true;
        _age = 0f;
    }

    public void Clear()
    {
        _history.Clear();
        _finished = false;
        if (_line != null) _line.positionCount = 0;
    }

    //// <summary>
    /// Appends arc points to the list (in Unity space). If the list is not empty, the first point of the arc is skipped,
    /// because it coincides with the previous last point.
    /// </summary>
    public static void SampleArc(List<Vector3> points, BlochVector start, BlochVector axis,
                                 double angle, double progress, int samplesPerHalfTurn)
    {
        bool first = points.Count == 0;
        double turns = Math.Abs(angle) / Math.PI * progress;
        if (turns < 1e-9)
        {
            if (first) points.Add(BlochSpace.ToUnity(start));
            return;
        }

        int segments = Mathf.Max(2, Mathf.CeilToInt((float)(samplesPerHalfTurn * turns)));
        for (int i = first ? 0 : 1; i <= segments; i++)
        {
            double f = progress * i / segments;
            points.Add(BlochSpace.ToUnity(start.Rotated(axis, angle * f)));
        }
    }

    private void Update()
    {
        if (_persistent || !_finished || !fadeOut) return;

        _age += Time.unscaledDeltaTime;
        if (_age <= lingerSeconds) return;

        float f = 1f - (_age - lingerSeconds) / Mathf.Max(0.01f, fadeSeconds);
        if (f <= 0f) Clear();
        else ApplyColor(f);
    }

    private void Apply(List<Vector3> points)
    {
        if (points.Count < 2)
        {
            _line.positionCount = 0;
            return;
        }
        if (_buffer.Length < points.Count)
            _buffer = new Vector3[Mathf.NextPowerOfTwo(points.Count)];

        points.CopyTo(_buffer);
        _line.positionCount = points.Count;
        _line.SetPositions(_buffer);
    }

    // The tail is thinner, the head (the end toward the arrow tip) is the thickest.
    private void ApplyWidthCurve()
    {
        float tail = _persistent ? 0.55f : 0.15f;
        _line.widthCurve = new AnimationCurve(new Keyframe(0f, tail), new Keyframe(1f, 1f));
    }

    // startColor -> endColor linear gradient along the line: the tail is faded, the head is fully colored.
    private void ApplyColor(float fade)
    {
        _fade = fade;
        float a = fade * _opacity;
        float tail = _persistent ? persistentTailAlpha : tailAlpha;
        _line.startColor = new Color(_color.r, _color.g, _color.b, tail * a);
        _line.endColor = new Color(_color.r, _color.g, _color.b, a);
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BlochSim.Model;
using UnityEngine;

/// <summary>
/// Displays the model's states. When a new gate is appended, it animates (the arrow rotates around the gate's axis);
/// for any other change, it snaps immediately to the new state.
/// </summary>
public class BlochView : MonoBehaviour
{
    [SerializeField] private SimulationController simulation;
    [SerializeField] private StateMarker markerPrefab;
    [SerializeField] private Transform markerParent;
    [SerializeField] private float gateDuration = 1.0f;
    [SerializeField]
    private Color[] palette =
    {
        new Color(1f, 0.35f, 0.3f), new Color(0.3f, 0.8f, 1f),
        new Color(0.6f, 1f, 0.4f), new Color(1f, 0.85f, 0.3f)
    };

    private readonly Dictionary<int, StateMarker> _markers = new Dictionary<int, StateMarker>();
    private BlochSimulator _model;
    private Coroutine _animation;

    private void Start()
    {
        _model = simulation.Model;
        _model.Changed += OnModelChanged;
        SnapToModel();
    }

    private void OnDestroy()
    {
        if (_model != null) _model.Changed -= OnModelChanged;
    }

    private void OnModelChanged(ModelChangeKind kind)
    {
        if (kind == ModelChangeKind.GateAppended) PlayLastGate();
        else SnapToModel();
    }

    private void SnapToModel()
    {
        StopAnimation();
        SyncMarkerSet();
        foreach (var kv in _markers)
            kv.Value.SetBloch(_model.FinalState(kv.Key).ToBloch());
    }

    private void PlayLastGate()
    {
        StopAnimation();
        SyncMarkerSet();

        int n = _model.Circuit.Count;
        Gate gate = _model.Circuit[n - 1];
        var starts = new Dictionary<int, BlochVector>();
        var ends = new Dictionary<int, BlochVector>();
        foreach (var s in _model.States)
        {
            starts[s.Id] = _model.StateAfter(s.Id, n - 1).ToBloch();
            ends[s.Id] = _model.StateAfter(s.Id, n).ToBloch();
        }
        _animation = StartCoroutine(Animate(gate, starts, ends));
    }

    private IEnumerator Animate(Gate gate, Dictionary<int, BlochVector> starts, Dictionary<int, BlochVector> ends)
    {
        BlochVector axis = gate.Rotation.Axis;
        double angle = gate.Rotation.Angle;
        float duration = Mathf.Max(0.01f, gateDuration);

        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            double k = Mathf.SmoothStep(0f, 1f, t);
            foreach (var kv in _markers)
                kv.Value.SetBloch(starts[kv.Key].Rotated(axis, angle * k));
            yield return null;
        }

        // At the end, set the exact model value so numerical drift doesn't accumulate in the display.
        foreach (var kv in _markers)
            kv.Value.SetBloch(ends[kv.Key]);
        _animation = null;
    }

    private void StopAnimation()
    {
        if (_animation != null)
        {
            StopCoroutine(_animation);
            _animation = null;
        }
    }

    private void SyncMarkerSet()
    {
        var ids = new HashSet<int>(_model.States.Select(s => s.Id));
        foreach (int id in _markers.Keys.Where(id => !ids.Contains(id)).ToList())
        {
            Destroy(_markers[id].gameObject);
            _markers.Remove(id);
        }
        foreach (var s in _model.States)
        {
            if (_markers.ContainsKey(s.Id)) continue;
            StateMarker m = Instantiate(markerPrefab, markerParent);
            m.SetColor(palette[_markers.Count % palette.Length]);
            _markers[s.Id] = m;
        }
    }
}
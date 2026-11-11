using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BlochSim.Model;
using UnityEngine;

/// <summary>
/// Displays the model's states (one arrow per state). When a new gate is appended it animates
/// (every arrow rotates around the gate's axis, drawing a trail meanwhile); on any other change it
/// immediately jumps to the new state and clears the trail.
/// </summary>
public class BlochView : MonoBehaviour
{
    [SerializeField] private SimulationController simulation;
    [SerializeField] private StateMarker markerPrefab;
    [SerializeField] private Transform markerParent;
    [SerializeField] private float gateDuration = 0.6f;
    [SerializeField] private bool keepHistory = true;   // the entire path so far is kept
    [SerializeField]
    private Color[] palette =
    {
        new Color(1f, 0.35f, 0.3f), new Color(0.3f, 0.8f, 1f),
        new Color(0.6f, 1f, 0.4f), new Color(1f, 0.85f, 0.3f)
    };

    private readonly Dictionary<int, StateMarker> _markers = new Dictionary<int, StateMarker>();
    private readonly Dictionary<int, int> _colorIndex = new Dictionary<int, int>();   // state identifier -> palette index
    private readonly Dictionary<int, int> _trailFrom = new Dictionary<int, int>();     // from which gate the path is visible
    private readonly Dictionary<int, QubitState> _lastInitial = new Dictionary<int, QubitState>();
    private BlochSimulator _model;
    private Coroutine _animation;

    public bool KeepHistory => keepHistory;

    /// <summary>Toggle keeping the path so far on/off (can be bound to Toggle.onValueChanged, dynamic bool).</summary>
    public void SetKeepHistory(bool value)
    {
        if (keepHistory == value) return;
        keepHistory = value;
        foreach (var m in _markers.Values) m.SetTrailPersistent(keepHistory);
        if (_animation == null) RefreshTrails();
    }

    /// <summary>The index of the active state (the sliders, columns follow this).</summary>
    public int ActiveIndex => simulation.ActiveIndex;

    private void Start()
    {
        _model = simulation.Model;
        _model.Changed += OnModelChanged;
        simulation.ActiveChanged += ApplyActive;
        SnapToModel();
    }

    private void OnDestroy()
    {
        if (_model != null) _model.Changed -= OnModelChanged;
        if (simulation != null) simulation.ActiveChanged -= ApplyActive;
    }

    private void OnModelChanged(ModelChangeKind kind)
    {
        if (kind == ModelChangeKind.InitialChanged) MarkRepositionedStates();

        if (kind == ModelChangeKind.GateAppended) PlayLastGate();
        else SnapToModel();
    }

    private void SnapToModel()
    {
        StopAnimation();
        SyncMarkerSet();
        foreach (var kv in _markers)
            kv.Value.SetBloch(_model.FinalState(kv.Key).ToBloch());
        RefreshTrails();
    }

    // In keep-history mode it redraws the entire path from the model (so it is correct after Undo, Reset, slider too),
    // otherwise it clears the trail.
    private void RefreshTrails()
    {
        int n = _model.Circuit.Count;
        foreach (var kv in _markers)
        {
            if (keepHistory) kv.Value.SetTrailHistory(BuildPath(kv.Key, ClampTrailFrom(kv.Key, n), n));
            else kv.Value.ClearTrail();
        }
    }

    // If a state's initial state has changed (slider, SetCurrentState), that is not a gate: the previous path is no longer
    // the state's path, so it does not rotate with it but disappears, and the new path starts from where the state now stands.
    private void MarkRepositionedStates()
    {
        foreach (StateEntry s in _model.States)
        {
            if (_lastInitial.TryGetValue(s.Id, out QubitState last) && last.Alpha == s.Initial.Alpha && last.Beta == s.Initial.Beta)
                continue;

            _lastInitial[s.Id] = s.Initial;
            _trailFrom[s.Id] = _model.Circuit.Count;
        }
    }

    // On undo/reset the gate sequence may shrink below the starting point: in that case the starting point also slides back.
    private int ClampTrailFrom(int stateId, int gateCount)
    {
        int from = _trailFrom.TryGetValue(stateId, out int f) ? Mathf.Min(f, gateCount) : 0;
        _trailFrom[stateId] = from;
        return from;
    }

    // The state's path from the fromGate-th gate up to toGate, as points in Unity space (one analytical arc per gate).
    private List<Vector3> BuildPath(int stateId, int fromGate, int toGate)
    {
        var points = new List<Vector3>();
        QubitState s = _model.StateAfter(stateId, fromGate);
        for (int i = fromGate; i < toGate; i++)
        {
            Gate g = _model.Circuit[i];
            TrailView.SampleArc(points, s.ToBloch(), g.Rotation.Axis, g.Rotation.Angle, 1.0, TrailView.SamplesPerHalfTurn);
            s = g.Apply(s);
        }
        return points;
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
        foreach (var kv in _markers)
        {
            if (keepHistory)
            {
                int from = Mathf.Min(ClampTrailFrom(kv.Key, n), n - 1);
                kv.Value.SetTrailHistory(BuildPath(kv.Key, from, n - 1));
            }
            else kv.Value.ClearTrail();
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
            {
                kv.Value.SetBloch(starts[kv.Key].Rotated(axis, angle * k));
                kv.Value.ShowTrailArc(starts[kv.Key], axis, angle, k);
            }
            yield return null;
        }

        // At the end, the model's exact value, so that numerical error does not accumulate in the display.
        foreach (var kv in _markers)
        {
            kv.Value.SetBloch(ends[kv.Key]);
            kv.Value.ShowTrailArc(starts[kv.Key], axis, angle, 1.0);
            kv.Value.FinishTrail();
        }
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

    // --- Queries for the UI ---

    /// <summary>
    /// The displayed probabilities of the stateIndex-th state. During animation they change together with the arrow.
    /// False if there is no such state or marker yet.
    /// </summary>
    public bool TryGetDisplayedProbabilities(int stateIndex, out float p0, out float p1)
    {
        p0 = 0f;
        p1 = 0f;
        if (!TryGetDisplayedBloch(stateIndex, out BlochVector b)) return false;

        p0 = (float)b.Prob0;
        p1 = (float)b.Prob1;
        return true;
    }

    public bool TryGetDisplayedBloch(int stateIndex, out BlochVector bloch)
    {
        bloch = default;
        if (_model == null || stateIndex < 0 || stateIndex >= _model.States.Count) return false;
        if (!_markers.TryGetValue(_model.States[stateIndex].Id, out StateMarker marker)) return false;

        bloch = marker.CurrentBloch;
        return true;
    }

    public bool TryGetColor(int stateIndex, out Color color)
    {
        color = Color.white;
        if (_model == null || stateIndex < 0 || stateIndex >= _model.States.Count) return false;
        if (!_colorIndex.TryGetValue(_model.States[stateIndex].Id, out int ci)) return false;

        color = palette[ci];
        return true;
    }

    // --- Marker management ---

    private void SyncMarkerSet()
    {
        var ids = new HashSet<int>(_model.States.Select(s => s.Id));
        foreach (int id in _markers.Keys.Where(id => !ids.Contains(id)).ToList())
        {
            Destroy(_markers[id].gameObject);
            _markers.Remove(id);
            _colorIndex.Remove(id);
            _trailFrom.Remove(id);
            _lastInitial.Remove(id);
        }
        foreach (var s in _model.States)
        {
            if (_markers.ContainsKey(s.Id)) continue;
            int ci = FreeColorIndex();
            StateMarker m = Instantiate(markerPrefab, markerParent);
            m.SetColor(palette[ci]);
            m.SetTrailPersistent(keepHistory);
            _markers[s.Id] = m;
            _colorIndex[s.Id] = ci;
            _trailFrom[s.Id] = _model.Circuit.Count;   // a new state has no history
            _lastInitial[s.Id] = s.Initial;
        }
        ApplyActive();
    }

    // The first palette color not used by any other state, so that after deletion there won't be two arrows of the same color.
    private int FreeColorIndex()
    {
        for (int i = 0; i < palette.Length; i++)
            if (!_colorIndex.ContainsValue(i)) return i;
        return _colorIndex.Count % palette.Length;
    }

    private void ApplyActive()
    {
        for (int i = 0; i < _model.States.Count; i++)
        {
            if (_markers.TryGetValue(_model.States[i].Id, out StateMarker m))
                m.SetActiveState(i == simulation.ActiveIndex);
        }
    }
}
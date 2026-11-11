using System;
using BlochSim.Model;
using UnityEngine;

/// <summary>
/// A "simulation instance": it owns the model and the active state.
/// The UI modifies through this, the view subscribes to the Model.Changed event.
/// Gates affect ALL states (a shared gate sequence); the active state only determines which state
/// the sliders, bars, and labels show, and which arrow is fully colored.
/// </summary>
[DefaultExecutionOrder(-100)]
public class SimulationController : MonoBehaviour
{
    [SerializeField] private int maxStates = 4;   // should not exceed the colors in the BlochView palette

    private static readonly QubitState[] Presets =
    {
        QubitState.Zero, QubitState.One, QubitState.Plus,
        QubitState.Minus, QubitState.PlusI, QubitState.MinusI
    };

    public BlochSimulator Model { get; private set; }

    /// <summary>The angle of parameterized gates  (Rx, Ry, Rz) in degrees</summary>
    public float AngleDegrees { get; private set; } = 90f;

    public int MaxStates => maxStates;

    /// <summary>The index of the active state in the Model.States list.</summary>
    public int ActiveIndex { get; private set; }

    /// <summary>
    /// Fires only on an Activate() call. When a state is added/removed, Model.Changed fires,
    /// and at that point ActiveIndex is already at the new value (the controller subscribes first).
    /// </summary>
    public event Action ActiveChanged;

    private void Awake()
    {
        Model = new BlochSimulator();
        Model.Changed += OnModelChanged;
        Model.AddState(QubitState.Zero, "ψ");
    }

    private void OnDestroy()
    {
        if (Model != null) Model.Changed -= OnModelChanged;
    }

    private void OnModelChanged(ModelChangeKind kind)
    {
        if (kind == ModelChangeKind.StateAdded)
            ActiveIndex = Model.States.Count - 1;                                  // the new state should be the active one
        else if (kind == ModelChangeKind.StateRemoved)
            ActiveIndex = Mathf.Clamp(ActiveIndex, 0, Model.States.Count - 1);     // the state that took its place
    }

    // --- Gates (for all states) ---

    public void Apply(Gate gate) => Model.AppendGate(gate);

    public void Undo() => Model.UndoLastGate();
    public void ResetCircuit() => Model.ClearCircuit();
    public void SetAngleDegrees(float degrees) => AngleDegrees = degrees;

    // --- States ---

    public void Activate(int index)
    {
        if (index < 0 || index >= Model.States.Count || index == ActiveIndex) return;
        ActiveIndex = index;
        ActiveChanged?.Invoke();
    }

    /// <summary>A new state at a still-free point, at the displayed position (the gate sequence does not change).</summary>
    public void AddState()
    {
        if (Model.States.Count >= maxStates) return;
        Model.AddStateAtCurrent(PickFreePreset());
    }

    /// <summary>Deletes the active state. The last state cannot be deleted.</summary>
    public void RemoveActive()
    {
        if (Model.States.Count <= 1) return;
        Model.RemoveState(Model.States[ActiveIndex].Id);
    }

    // The first base state (|0>, |1>, |+>, ...) that does not coincide with an existing arrow.
    private QubitState PickFreePreset()
    {
        foreach (QubitState preset in Presets)
        {
            BlochVector p = preset.ToBloch();
            bool free = true;
            foreach (StateEntry s in Model.States)
            {
                BlochVector q = Model.FinalState(s.Id).ToBloch();
                double dx = p.X - q.X, dy = p.Y - q.Y, dz = p.Z - q.Z;
                if (dx * dx + dy * dy + dz * dz < 0.04)   // closer than 0.2: occupied
                {
                    free = false;
                    break;
                }
            }
            if (free) return preset;
        }
        return QubitState.Zero;
    }
}
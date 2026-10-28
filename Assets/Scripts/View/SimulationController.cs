using BlochSim.Model;
using UnityEngine;

/// <summary>
/// The "simulation instance": this owns the model. The UI modifies it through this,
/// and the view (BlochView) subscribes to the Model.Changed event.
/// </summary>
[DefaultExecutionOrder(-100)]
public class SimulationController : MonoBehaviour
{
    public BlochSimulator Model { get; private set; }

    private void Awake()
    {
        Model = new BlochSimulator();
        Model.AddState(QubitState.Zero, "ψ");
    }

    public void Apply(Gate gate) => Model.AppendGate(gate);

    // The following can be bound to Button.onClick / Slider.onValueChanged events in the Inspector.

    public void Undo() => Model.UndoLastGate();
    public void ResetCircuit() => Model.ClearCircuit();
}
using BlochSim.Model;
using UnityEngine;
using UnityEngine.UI;

public enum GateKind { I, X, Y, Z, H, S, T, Sdg, Tdg, Rx, Ry, Rz }

/// <summary>Component that can be attached to a button: on click, appends the selected gate to the gate sequence.</summary>
[RequireComponent(typeof(Button))]
public class GateButton : MonoBehaviour
{
    [SerializeField] private SimulationController simulation;
    [SerializeField] private GateKind kind;

    [SerializeField] private InputHandler angleInput;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        simulation.Apply(Create());
    }

    private Gate Create()
    {
        double angle = angleInput.GetFieldValue() * Mathf.Deg2Rad;

        return kind switch
        {
            GateKind.I => Gates.I,
            GateKind.X => Gates.X,
            GateKind.Y => Gates.Y,
            GateKind.Z => Gates.Z,
            GateKind.H => Gates.H,
            GateKind.S => Gates.S,
            GateKind.T => Gates.T,
            GateKind.Sdg => Gates.Sdg,
            GateKind.Tdg => Gates.Tdg,
            GateKind.Rx => Gates.Rx(angle),
            GateKind.Ry => Gates.Ry(angle),
            GateKind.Rz => Gates.Rz(angle),
            _ => throw new System.ArgumentOutOfRangeException(),
        };
    }
}
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ResetButton : MonoBehaviour
{
    [SerializeField] private SimulationController simulation;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(ResetSimulation);
    }

    private void ResetSimulation()
    {
        simulation.ResetCircuit();
    }
}

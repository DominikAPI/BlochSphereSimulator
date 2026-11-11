using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ResetButton : MonoBehaviour
{
    [SerializeField] private SimulationController simulation;
    [SerializeField] private AngleSliders sliders;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(ResetSimulation);
    }

    private void ResetSimulation()
    {
        simulation.ResetCircuit();
        sliders.ResetSliders();
        Camera.main.GetComponent<OrbitCamera>().ResetView();
    }
}

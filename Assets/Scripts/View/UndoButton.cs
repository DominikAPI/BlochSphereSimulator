using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UndoButton : MonoBehaviour
{
    [SerializeField] private SimulationController simulation;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Undo);
    }

    private void Undo()
    {
        simulation.Undo();
    }
}

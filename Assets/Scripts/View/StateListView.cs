using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The list of states (at most 4 pre-placed rows): colored marker + name, clicking makes the state active.
/// It aligns the rows with the model every frame (it only writes on change), so it doesn't depend on the order of events.
/// </summary>
public class StateListView : MonoBehaviour
{
    [Serializable]
    public class Row
    {
        public Button button;
        public RawImage swatch;           
        public TMP_Text label;         
        public GameObject activeFrame;
    }

    [SerializeField] private SimulationController simulation;
    [SerializeField] private BlochView view;
    [SerializeField] private Row[] rows;
    [SerializeField] private Button addButton;      // onClick -> SimulationController.AddState
    [SerializeField] private Button removeButton;   // onClick -> SimulationController.RemoveActive

    private static readonly string[] Labels = { "ψ1", "ψ2", "ψ3", "ψ4" };

    private void Awake()
    {
        for (int i = 0; i < rows.Length; i++)
        {
            int index = i;  
            rows[i].button.onClick.AddListener(() => simulation.Activate(index));
        }

        addButton.onClick.AddListener(() => simulation.AddState());
        removeButton.onClick.AddListener(() => simulation.RemoveActive());
    }

    private void LateUpdate()
    {
        BlochSim.Model.BlochSimulator model = simulation.Model;
        if (model == null) return;

        int count = model.States.Count;
        for (int i = 0; i < rows.Length; i++)
        {
            Row r = rows[i];
            bool visible = i < count;
            if (r.button.gameObject.activeSelf != visible)
            {
                r.button.gameObject.SetActive(visible);
                r.activeFrame.SetActive(visible);
                r.label.gameObject.SetActive(visible);
                r.swatch.gameObject.SetActive(visible);
            }
            if (!visible) continue;

            if (view.TryGetColor(i, out Color c) && r.swatch != null && r.swatch.color != c)
                r.swatch.color = c;

            if (r.label != null)
            {
                string text = i < Labels.Length ? Labels[i] : "ψ" + (i + 1);
                if (r.label.text != text) r.label.text = text;
            }

            bool isActive = i == simulation.ActiveIndex;
            if (r.activeFrame != null && r.activeFrame.activeSelf != isActive)
                r.activeFrame.SetActive(isActive);
        }

        if (addButton != null) addButton.interactable = count < simulation.MaxStates;
        if (removeButton != null) removeButton.interactable = count > 1;
    }
}
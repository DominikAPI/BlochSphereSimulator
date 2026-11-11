using System;
using BlochSim.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Two sliders (polar angle θ and azimuth φ) that set the CURRENT position of the selected state.
/// Not a gate: it does not go into the gate sequence, the model recalculates the initial state (SetCurrentState).
/// The sliders also continuously follow the displayed arrow, so they move during gate presses and animations as well.
/// </summary>
public class AngleSliders : MonoBehaviour
{
    [SerializeField] private SimulationController simulation;
    [SerializeField] private BlochView view;
    private int StateIndex => simulation.ActiveIndex;

    [Header("Sliders")]
    [SerializeField] private Slider thetaSlider;   // the script sets it between 0..180
    [SerializeField] private Slider phiSlider;     // the script sets it between 0..360

    [Header("Texts")]
    [SerializeField] private TMP_Text thetaLabel;
    [SerializeField] private TMP_Text phiLabel;

    private void Awake()
    {
        thetaSlider.minValue = 0f;
        thetaSlider.maxValue = 180f;
        thetaSlider.wholeNumbers = false;

        phiSlider.minValue = 0f;
        phiSlider.maxValue = 360f;
        phiSlider.wholeNumbers = false;

        thetaSlider.onValueChanged.AddListener(OnSliderChanged);
        phiSlider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnDestroy()
    {
        thetaSlider.onValueChanged.RemoveListener(OnSliderChanged);
        phiSlider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    private void OnSliderChanged(float _)
    {
        BlochSimulator model = simulation.Model;
        if (model == null || StateIndex < 0 || StateIndex >= model.States.Count) return;

        double theta = thetaSlider.value / Mathf.Rad2Deg;
        double phi = phiSlider.value / Mathf.Rad2Deg;
        model.SetCurrentState(model.States[StateIndex].Id, QubitState.FromAngles(theta, phi));
    }

    private void Start()
    {
        thetaLabel.text = $"θ = 0.0°";
        phiLabel.text = $"φ = 0.0°";
    }

    // LateUpdate: the BlochView animation runs after Update, so the slider follows the arrow in the same frame.
    private void LateUpdate()
    {
        if (!view.TryGetDisplayedBloch(StateIndex, out BlochVector b)) return;

        double theta = b.Theta * Mathf.Rad2Deg;
        thetaSlider.SetValueWithoutNotify((float)theta);
        thetaLabel.text = $"θ = {theta:0.0}°";

        double equatorial = Math.Sqrt(b.X * b.X + b.Y * b.Y);
        if (equatorial > 1e-4)
        {
            double phi = b.Phi * Mathf.Rad2Deg;
            phiSlider.SetValueWithoutNotify((float)phi);
            phiLabel.text = $"φ = {phi:0.0}°";
        }
    }

    public void ResetSliders()
    {
        thetaLabel.text = $"θ = 0.0°";
        phiLabel.text = $"φ = 0.0°";

        thetaSlider.value = 0;
        phiSlider.value = 0;
    }
}
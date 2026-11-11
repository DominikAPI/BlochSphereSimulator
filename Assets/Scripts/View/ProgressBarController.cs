using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ProbabilityOutcome { Zero, One }

/// <summary>
/// A horizontal/vertical bar placed on a RawImage that shows the P(0) or P(1) value of the displayed state.
/// It requests the value from BlochView, so the bar moves together with the arrow during the gate animation.
/// Two instances are needed: one Zero and one One, both pointing to the same BlochView and state index.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class ProgressBarController : MonoBehaviour
{
    [SerializeField] private Material baseMaterial;
    [SerializeField] private BlochView view;
    [SerializeField] private ProbabilityOutcome outcome = ProbabilityOutcome.Zero;

    [SerializeField] private TMP_Text percentageBox;

    private static readonly int ProgressId = Shader.PropertyToID("_Progress");

    private Material _material;
    private float _last = -1f;

    private void Awake()
    {
        var image = GetComponent<RawImage>();
        _material = Instantiate(baseMaterial);
        image.material = _material;
        SetProgress(0f);
    }

    private void OnDestroy()
    {
        if (_material != null) Destroy(_material);
    }

    // LateUpdate: the BlochView animation coroutine runs after Update, so the bar already
    // sees the new arrow position in the same frame.
    private void LateUpdate()
    {
        UpdateBar();
    }

    public void UpdateBar()
    {
        if (view == null) return;
        if (!view.TryGetDisplayedProbabilities(view.ActiveIndex, out float p0, out float p1)) return;

        SetProgress(outcome == ProbabilityOutcome.Zero ? p0 : p1);
    }

    private void SetProgress(float value)
    {
        if (Mathf.Approximately(value, _last)) return;
        _last = value;
        _material.SetFloat(ProgressId, value);
        percentageBox.text = $"{Mathf.Round(value * 100)}%";
    }
}
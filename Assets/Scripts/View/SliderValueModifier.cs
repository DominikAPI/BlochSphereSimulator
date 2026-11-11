using UnityEngine;
using UnityEngine.UI;

public enum ModifierButtonType { Increase, Decrease }

[RequireComponent(typeof(Button))]
public class SliderValueModifier : MonoBehaviour
{
    [SerializeField] private Slider affectedSlider;
    [SerializeField] private ModifierButtonType buttonType;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(ApplyButton);
    }

    private void ApplyButton()
    {
        if (buttonType == ModifierButtonType.Increase) affectedSlider.value = Mathf.RoundToInt(++affectedSlider.value);
        else affectedSlider.value = Mathf.RoundToInt(--affectedSlider.value);
    }

    private void OnDestroy()
    {
        GetComponent<Button>().onClick.RemoveListener(ApplyButton);
    }
}

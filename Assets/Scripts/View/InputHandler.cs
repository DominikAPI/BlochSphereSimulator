using UnityEngine;
using TMPro;


/// <summary>
/// Custom input field wrapper for integer input limited to [-360, 360].
/// Type-time filtering only removes invalid characters and clamps completed integers;
/// the final fallback (default value + clamping) happens in <see cref="GetFieldValue"/>.
/// </summary>
[RequireComponent(typeof(TMP_InputField))]
public class InputHandler : MonoBehaviour
{
    private TMP_InputField inputField;

    private const int DefaultValue = 90;            // Fallback when the field is empty or only "-"
    private const int MinValue = -360;
    private const int MaxValue = 360;

    private void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
    }

    private void Start()
    {
        // Initialize with the default value without firing onValueChanged
        inputField.SetTextWithoutNotify(DefaultValue.ToString());
        inputField.onValueChanged.AddListener(OnValueChanged);
    }


    /// <summary>
    /// Filters input while the user is typing.
    /// - Partial states ("", "-", "-0") are allowed so the user can delete / type a minus sign.
    /// - Invalid characters are sanitized out.
    /// - Completed integers outside the range are clamped immediately (visual feedback).
    /// Uses SetTextWithoutNotify to avoid infinite recursion through onValueChanged.
    /// </summary>
    private void OnValueChanged(string newText)
    {
        // Partial state (empty, "-", "-0"): let the user keep editing
        if (IsPartialOrValidInteger(newText) && !int.TryParse(newText, out _))
            return;

        // Invalid characters: sanitize the string
        if (!IsPartialOrValidInteger(newText))
        {
            string sanitized = SanitizeInput(newText);
            inputField.SetTextWithoutNotify(sanitized);
            return;
        }

        // Complete valid integer: clamp if out of range
        int value = int.Parse(newText);
        if (value < MinValue || value > MaxValue)
        {
            int clamped = Mathf.Clamp(value, MinValue, MaxValue);
            inputField.SetTextWithoutNotify(clamped.ToString());
        }
    }

    /// <summary>
    /// Returns true for empty string, "-", "-0", or a valid integer (with optional leading minus).
    /// These are states we tolerate while the user is still typing.
    /// </summary>
    private static bool IsPartialOrValidInteger(string text)
    {
        if (string.IsNullOrEmpty(text)) return true;
        if (text == "-") return true;
        if (text == "-0") return true;

        return int.TryParse(text, out _);
    }

    /// <summary>
    /// Keeps only digits and a single leading minus sign.
    /// </summary>
    private static string SanitizeInput(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        bool negative = text.StartsWith("-");
        string digits = string.Empty;
        foreach (char c in text)
        {
            if (char.IsDigit(c)) digits += c;
        }

        if (digits.Length == 0) return negative ? "-" : string.Empty;
        return (negative ? "-" : "") + digits;
    }

    /// <summary>
    /// Returns the current integer value of the field.
    /// Falls back to <see cref="DefaultValue"/> if the text is empty or invalid,
    /// and clamps to [<see cref="MinValue"/>, <see cref="MaxValue"/>].
    /// </summary>
    public int GetFieldValue()
    {
        if (!int.TryParse(inputField.text, out int value))
            return DefaultValue;

        return Mathf.Clamp(value, MinValue, MaxValue);
    }
}
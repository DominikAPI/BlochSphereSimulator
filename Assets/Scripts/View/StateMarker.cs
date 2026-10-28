using BlochSim.Model;
using UnityEngine;

/// <summary>Displays a single state on the sphere: arrow</summary>
public class StateMarker : MonoBehaviour
{
    [SerializeField] private ArrowView arrow;

    public void SetBloch(BlochVector b)
    {
        Vector3 p = BlochSpace.ToUnity(b);
        arrow.SetDirection(p);
    }

    public void SetColor(Color color)
    {
        arrow.SetColor(color);
    }
}
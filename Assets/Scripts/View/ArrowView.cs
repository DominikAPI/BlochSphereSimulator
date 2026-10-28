using UnityEngine;

/// <summary>
/// Arrow from the origin: shaft (Unity Cylinder) + procedural cone. The arrow is built along its own +Y axis;
/// SetDirection rotates it to the desired direction. Dimensions are scaled to the sphere radius (radius = 1).
/// </summary>
public class ArrowView : MonoBehaviour
{
    [Header("Parts")]
    [SerializeField] private Transform shaft;        // Unity Cylinder primitive 
    [SerializeField] private MeshFilter coneFilter;  // empty GameObject with MeshFilter + MeshRenderer components

    [Header("Size")]
    [SerializeField] private float length = 1f;      // the tip should lie on the sphere surface
    [SerializeField] private float shaftDiameter = 0.025f;
    [SerializeField] private float coneRadius = 0.045f;
    [SerializeField] private float coneHeight = 0.14f;
    [SerializeField] private int coneSegments = 24;

    private void Awake()
    {
        Rebuild();
    }

    private void OnDestroy()
    {
        if (coneFilter != null && coneFilter.sharedMesh != null)
            Destroy(coneFilter.sharedMesh);
    }

    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        float shaftLength = Mathf.Max(0.001f, length - coneHeight);
        
        shaft.localScale = new Vector3(shaftDiameter, shaftLength * 0.5f, shaftDiameter);
        shaft.localPosition = new Vector3(0f, shaftLength * 0.5f, 0f);
        coneFilter.transform.localPosition = new Vector3(0f, shaftLength, 0f);
        coneFilter.sharedMesh = ConeMeshFactory.Create(coneRadius, coneHeight, coneSegments);
    }

    /// <summary>Direction in Unity space (does not need to be normalized).</summary>
    public void SetDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude < 1e-10f) return;
        transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
    }

    public void SetColor(Color color)
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.material.color = color;
    }
}
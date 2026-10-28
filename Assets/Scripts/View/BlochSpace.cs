using BlochSim.Model;
using UnityEngine;

public static class BlochSpace
{
    public static Vector3 ToUnity(BlochVector b) => new Vector3((float)b.X, (float)b.Z, (float)b.Y);

    public static BlochVector FromUnity(Vector3 v) => new BlochVector(v.x, v.z, v.y);
}
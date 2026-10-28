using System.Collections.Generic;
using UnityEngine;

public static class ConeMeshFactory
{
    /// <summary>Cone along the +Y axis: base center at (0,0,0), apex at (0,height,0).</summary>
    public static Mesh Create(float radius, float height, int segments = 24)
    {
        segments = Mathf.Max(3, segments);
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();
        float step = 2f * Mathf.PI / segments;

        // Side surface: ring points 0..segments (seam duplicated), and a separate apex vertex
        // per segment so the normal is correct at the apex too.
        for (int i = 0; i <= segments; i++)
        {
            float a = i * step;
            vertices.Add(new Vector3(radius * Mathf.Cos(a), 0f, radius * Mathf.Sin(a)));
            normals.Add(SideNormal(a, radius, height));
        }
        int apexStart = vertices.Count;
        for (int i = 0; i < segments; i++)
        {
            vertices.Add(new Vector3(0f, height, 0f));
            normals.Add(SideNormal((i + 0.5f) * step, radius, height));
        }
        for (int i = 0; i < segments; i++)
        {
            // Unity: clockwise winding when viewed from outside is the front face.
            triangles.Add(i);
            triangles.Add(apexStart + i);
            triangles.Add(i + 1);
        }

        // Base (facing down)
        int capCenter = vertices.Count;
        vertices.Add(Vector3.zero);
        normals.Add(Vector3.down);
        int capRing = vertices.Count;
        for (int i = 0; i <= segments; i++)
        {
            float a = i * step;
            vertices.Add(new Vector3(radius * Mathf.Cos(a), 0f, radius * Mathf.Sin(a)));
            normals.Add(Vector3.down);
        }
        for (int i = 0; i < segments; i++)
        {
            triangles.Add(capCenter);
            triangles.Add(capRing + i);
            triangles.Add(capRing + i + 1);
        }

        var mesh = new Mesh { name = "Cone" };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>
    /// Side surface normal: (height*cos a, radius, height*sin a), normalized.
    /// </summary>
    private static Vector3 SideNormal(float angle, float radius, float height)
    {
        return new Vector3(height * Mathf.Cos(angle), radius, height * Mathf.Sin(angle)).normalized;
    }
}
using UnityEditor;
using UnityEngine;

class InsideOutMeshFixer : AssetPostprocessor
{
    const string Folder = "Assets/Models/Productos/";
    const float InsideOutThreshold = 0.25f;

    void OnPostprocessModel(GameObject root)
    {
        if (!assetPath.StartsWith(Folder)) return;

        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            var mesh = filter.sharedMesh;
            if (mesh == null || OutwardFraction(mesh) >= InsideOutThreshold) continue;
            Flip(mesh);
        }
    }

    static float OutwardFraction(Mesh mesh)
    {
        var vertices = mesh.vertices;
        var triangles = mesh.triangles;
        Vector3 center = mesh.bounds.center;
        int total = 0, outward = 0;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = vertices[triangles[i]], b = vertices[triangles[i + 1]], c = vertices[triangles[i + 2]];
            Vector3 face = Vector3.Cross(b - a, c - a);
            if (face.sqrMagnitude < 1e-12f) continue;
            total++;
            if (Vector3.Dot(face, (a + b + c) / 3f - center) > 0f) outward++;
        }
        return total > 0 ? (float)outward / total : 1f;
    }

    static void Flip(Mesh mesh)
    {
        for (int s = 0; s < mesh.subMeshCount; s++)
        {
            var triangles = mesh.GetTriangles(s);
            for (int i = 0; i < triangles.Length; i += 3)
                (triangles[i + 1], triangles[i + 2]) = (triangles[i + 2], triangles[i + 1]);
            mesh.SetTriangles(triangles, s);
        }

        var normals = mesh.normals;
        for (int i = 0; i < normals.Length; i++) normals[i] = -normals[i];
        mesh.normals = normals;

        if (mesh.tangents.Length > 0) mesh.RecalculateTangents();
    }
}

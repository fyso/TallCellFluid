using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Common
{
    public static uint SPHThreadCount = 512;
    public static uint SWThreadCount = 32;

    public static void SwapComputeBuffer(ref ComputeBuffer Buffer1, ref ComputeBuffer Buffer2)
    {
        ComputeBuffer Temp = Buffer1;
        Buffer1 = Buffer2;
        Buffer2 = Temp;
    }

    public static Mesh GeneratePlane(Vector3 origin, Vector3 axis0, Vector3 axis1, int axis0Vertices, int axis1Vertices, Vector2 uvStart, Vector2 uvEnd)
    {
        var vertices = new Vector3[axis0Vertices * axis1Vertices];
        var normals = new Vector3[vertices.Length];
        var uvs = new Vector2[vertices.Length];
        var triangles = new int[(axis0Vertices - 1) * (axis1Vertices - 1) * 2 * 3];
        var normal = Vector3.Cross(axis1, axis0).normalized;

        // Vertices
        for (var i = 0; i < vertices.Length; i++)
        {
            var i0 = i / axis1Vertices;
            var i1 = i % axis1Vertices;
            var localU = i0 / (axis0Vertices - 1f);
            var localV = i1 / (axis1Vertices - 1f);

            vertices[i] = origin + localU * axis0 + localV * axis1;
            normals[i] = normal;
            uvs[i].x = Mathf.Lerp(uvStart.x, uvEnd.x, localU);
            uvs[i].y = Mathf.Lerp(uvStart.y, uvEnd.y, localV);
        }

        // Triangles
        var vertexIndex = 0;
        for (var i0 = 0; i0 < axis0Vertices - 1; i0++)
        {
            for (var i1 = 0; i1 < axis1Vertices - 1; i1++)
            {
                triangles[vertexIndex++] = (i0 + 0) * axis1Vertices + (i1 + 0);
                triangles[vertexIndex++] = (i0 + 1) * axis1Vertices + (i1 + 1);
                triangles[vertexIndex++] = (i0 + 1) * axis1Vertices + (i1 + 0);

                triangles[vertexIndex++] = (i0 + 0) * axis1Vertices + (i1 + 0);
                triangles[vertexIndex++] = (i0 + 0) * axis1Vertices + (i1 + 1);
                triangles[vertexIndex++] = (i0 + 1) * axis1Vertices + (i1 + 1);
            }
        }

        var mesh = new Mesh()
        {
            name = $"Plane_{axis0Vertices}x{axis1Vertices}",
            vertices = vertices,
            normals = normals,
            uv = uvs,
            triangles = triangles
        };

        mesh.RecalculateBounds();
        return mesh;
    }
}
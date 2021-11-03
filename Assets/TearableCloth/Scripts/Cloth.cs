using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TearableCloth
{
    public class Cloth
    {
        private List<Point> points;
        private ClothScript clothScript;

        private Vector3[] mVertices;


        public Vector3[] vertices
        {
            get
            {
                if (mVertices == null)
                    return mVertices = clothScript.mesh.vertices;
                return clothScript.mesh.vertices = mVertices;
            }
        }

        public Cloth(ClothScript clothScript)
        {
            this.clothScript = clothScript;
            points = new List<Point>();

            var startX = 0;
            var vectors = new List<Vector3>();
            var uvs = new List<Vector2>();
            for (var y = 0; y <= clothScript.clothY; y++)
            {
                for (var x = 0; x <= clothScript.clothX; x++)
                {
                    var v = new Vector3(startX + x * clothScript.spacing, 0, -y * clothScript.spacing);
                    var point = new Point(v.x, v.y, v.z, clothScript);

                    if (y == 0)
                        point.Pin();
                    if (x != 0)
                        point.Attach(points[points.Count - 1]);
                    if (y != 0)
                        point.Attach(points[x + (y - 1) * (clothScript.clothX + 1)]);

                    points.Add(point);
                    vectors.Add(Vector3.zero);

                    var uv = new Vector2(x * 1f / clothScript.clothX, y * 1f / clothScript.clothY);
                    uvs.Add(uv);
                }
            }

            var cellCount = clothScript.clothX * clothScript.clothY;
            var indices = new int[cellCount * 2 * 3];
            for (var y = 0; y < clothScript.clothY; y++)
            {
                for (var x = 0; x < clothScript.clothX; x++)
                {
                    var cellInx = y * clothScript.clothX + x;
                    indices[cellInx * 6 + 0] = x + y * (clothScript.clothX + 1);
                    indices[cellInx * 6 + 1] = x + 1 + y * (clothScript.clothX + 1);
                    indices[cellInx * 6 + 2] = x + (y + 1) * (clothScript.clothX + 1);

                    indices[cellInx * 6 + 3] = x + 1 + y * (clothScript.clothX + 1);
                    indices[cellInx * 6 + 4] = x + 1 + (y + 1) * (clothScript.clothX + 1);
                    indices[cellInx * 6 + 5] = x + (y + 1) * (clothScript.clothX + 1);
                }
            }

            if (clothScript.mesh)
            {
                clothScript.mesh.vertices = vectors.ToArray();
                clothScript.mesh.uv = uvs.ToArray();
                clothScript.mesh.SetIndices(indices, MeshTopology.Triangles, 0);
                clothScript.mesh.RecalculateNormals();
                clothScript.mesh.RecalculateBounds();
            }
        }

        public void Update(float force, float particleSize)
        {
            var i = clothScript.accuracy;

            while (i-- > 0)
            {
                for (var index = 0; index < points.Count; index++)
                {
                    var point = points[index];
                    point.Resolve();
                }
            }

            var delta = Time.deltaTime * Time.deltaTime;
            for (var index = 0; index < points.Count; index++)
            {
                var point = points[index];
                point.Update(delta, force, particleSize);

                var v = vertices[index];
                v.x = point.x;
                v.y = point.y;
                v.z = point.z;
                // if (!point.isPinAt)
                v = clothScript.transform.InverseTransformPoint(v);
                vertices[index] = v;
            }

            if (clothScript.mesh)
            {
                clothScript.mesh.RecalculateNormals();
            }
        }

#if UNITY_EDITOR
        public void DrawGizmos(float particleSize)
        {
            // Handles.matrix = clothScript.transform.localToWorldMatrix;
            for (var index = 0; index < points.Count; index++)
            {
                var point = points[index];
                Handles.SphereHandleCap(index, point,
                    Quaternion.identity, particleSize, EventType.Repaint);
            }
        }
#endif
    }
}
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TearableCloth
{
    public class Cloth
    {
        private List<Point> points;
        private ClothScript clothData;

        private Vector3[] mVertices;


        public Vector3[] vertices =>
            mVertices != null ? clothData.mesh.vertices = mVertices : (mVertices = clothData.mesh.vertices);

        public Cloth(ClothScript clothData)
        {
            this.clothData = clothData;
            this.points = new List<Point>();

            var startX = 0;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            for (var y = 0; y <= clothData.clothY; y++)
            {
                for (var x = 0; x <= clothData.clothX; x++)
                {
                    var point = new Point(startX + x * clothData.spacing, -y * clothData.spacing, 0, clothData);

                    if (y == 0)
                        point.Pin();
                    if (x != 0)
                        point.Attach(this.points[this.points.Count - 1]);
                    if (y != 0)
                        point.Attach(this.points[x + (y - 1) * (clothData.clothX + 1)]);

                    this.points.Add(point);
                    vertices.Add(Vector3.zero);

                    var uv = new Vector2(x * 1f / clothData.clothX, y * 1f / clothData.clothY);
                    uvs.Add(uv);
                }
            }

            var cellCount = clothData.clothX * clothData.clothY;
            var indices = new int[cellCount * 2 * 3];
            for (var y = 0; y < clothData.clothY; y++)
            {
                for (var x = 0; x < clothData.clothX; x++)
                {
                    var cellInx = y * clothData.clothX + x;
                    indices[cellInx * 6 + 0] = x + y * (clothData.clothX + 1);
                    indices[cellInx * 6 + 1] = x + 1 + y * (clothData.clothX + 1);
                    indices[cellInx * 6 + 2] = x + (y + 1) * (clothData.clothX + 1);

                    indices[cellInx * 6 + 3] = x + 1 + y * (clothData.clothX + 1);
                    indices[cellInx * 6 + 4] = x + 1 + (y + 1) * (clothData.clothX + 1);
                    indices[cellInx * 6 + 5] = x + (y + 1) * (clothData.clothX + 1);
                }
            }

            if (clothData.mesh)
            {
                clothData.mesh.vertices = vertices.ToArray();
                clothData.mesh.uv = uvs.ToArray();
                clothData.mesh.SetIndices(indices, MeshTopology.Triangles, 0);
                clothData.mesh.RecalculateNormals();
                clothData.mesh.RecalculateBounds();
            }
        }

        public void Update(float force, float particleSize)
        {
            var i = clothData.accuracy;

            while (i-- > 0)
            {
                for (var index = 0; index < this.points.Count; index++)
                {
                    var point = this.points[index];
                    point.Resolve();

                    var v = vertices[index];
                    v.x = point.x;
                    v.y = point.y;
                    v.z = point.z;
                    vertices[index] = v;
                }
            }

            var delta = Time.deltaTime * Time.deltaTime;
            for (var index = 0; index < this.points.Count; index++)
            {
                var point = this.points[index];
                point.Update(delta, force, particleSize);
                points[index] = point;
            }

            if (clothData.mesh)
            {
                clothData.mesh.RecalculateNormals();
            }
        }

#if UNITY_EDITOR
        public void DrawGizmos(float particleSize)
        {
            Handles.matrix = clothData.transform.localToWorldMatrix;
            for (var index = 0; index < this.points.Count; index++)
            {
                var point = this.points[index];
                Handles.SphereHandleCap(index, point,
                    Quaternion.identity, particleSize, EventType.Repaint);
            }
        }
#endif
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ClothScript : MonoBehaviour
{
    public int accuracy = 5;
    public int gravity = -10;
    public int clothY = 28;
    public int clothX = 54;
    public int spacing = 8;
    public int tearDist = 60;
    public float friction = 0.99f;
    public float bounce = 0.5f;

    public float force = 0.01f;
    public float particleSize = 0.1f;

    [HideInInspector] public Mesh mesh;
    public MouseInfo mouse;

    private TearableCloth.Cloth cloth;
    public float width = 800;
    public float height = 1260;

    private void Awake()
    {
        mouse = new MouseInfo
        {
            influence = 26,
            cut = 8
        };

        mesh = GetComponent<MeshFilter>().mesh;
        cloth = new TearableCloth.Cloth(this);
    }

    private void Update()
    {
        mouse.Update();

        cloth.Update(force, particleSize);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (cloth != null)
            cloth.DrawGizmos(particleSize);
    }
#endif

    [Serializable]
    public class MouseInfo
    {
        [HideInInspector] public float x;
        [HideInInspector] public float y;
        public float influence;
        [HideInInspector] public float px;
        [HideInInspector] public float py;
        public float cut;
        [HideInInspector] public bool down;

        public bool isEnable = true;

        public void Update()
        {
            if (!isEnable)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                down = true;
            }

            if (Input.GetMouseButtonUp(0))
            {
                down = false;
            }

            var p = Input.mousePosition;
            px = x;
            py = y;

            x = p.x;
            y = p.y;
        }
    }
}

namespace TearableCloth
{
    class Point
    {
        public float x;
        public float y;
        public float z;
        private float px;
        private float py;
        private float pz;
        private float vx;
        private float vy;
        private float vz;

        public Point pinAt;

        private List<Constraint> constraints;
        private ClothScript clothData;

        public Point(float x, float y, float z, ClothScript clothData)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.px = x;
            this.py = y;
            this.pz = z;
            vx = 0;
            vy = 0;
            vz = 0;
            pinAt = null;
            this.clothData = clothData;
            constraints = new List<Constraint>();
        }

        public override string ToString()
        {
            return $"({x},{y},{z}) CONSTRAIN: {constraints.Count}";
        }

        private RaycastHit[] hits = new RaycastHit[1];

        public Point Update(float delta, float force, float particleSize)
        {
            if (this.pinAt != null)
            {
                return this;
            }

            var worldPos = clothData.transform.localToWorldMatrix.MultiplyPoint3x4(this);
            // Debug.Log(this + "  ->  " + worldPos);
            if (clothData.mouse.down)
            {
                var distance = Vector2.Distance(worldPos,
                    Camera.main.ScreenToWorldPoint(new Vector3(clothData.mouse.x, clothData.mouse.y)));


                if (distance < clothData.mouse.influence)
                {
                    this.px = this.x - (clothData.mouse.x - clothData.mouse.px);
                    this.py = this.y - (clothData.mouse.y - clothData.mouse.py);
                    this.pz = this.z - force;
                }
                else if (distance < clothData.mouse.cut)
                {
                    this.constraints = new List<Constraint>();
                }
            }

            this.AddForce(0, clothData.gravity, 0);

            var dx = (this.x - this.px) * clothData.friction + this.vx * delta;
            var dy = (this.y - this.py) * clothData.friction + this.vy * delta;
            var dz = (this.z - this.pz) * clothData.friction + this.vz * delta;

            var dir = new Vector3(dx, dy, dz);
            var l = dir.magnitude;
            dir *= 1f / l;
            if (Physics.SphereCastNonAlloc(worldPos, particleSize,
                dir, hits, l) > 0)
            {
                RaycastHit raycastHit = hits[0];

                var n = Vector3.ProjectOnPlane(dir, raycastHit.normal);
                dx = n.x;
                dy = n.y;
                dz = n.z;
            }

            var nx = this.x + dx;
            var ny = this.y + dy;
            var nz = this.z + dz;

            this.px = this.x;
            this.py = this.y;
            this.pz = this.z;

            this.x = nx;
            this.y = ny;
            this.z = nz;

            this.vy = this.vx = this.vz = 0;


            if (this.x >= clothData.width)
            {
                this.px = clothData.width + (clothData.width - this.px) * clothData.bounce;
                this.x = clothData.width;
            }
            else if (this.x <= -clothData.width)
            {
                this.px *= -1 * clothData.bounce;
                this.x = -clothData.width;
            }

            if (this.y >= clothData.height)
            {
                this.py = clothData.height + (clothData.height - this.py) * clothData.bounce;
                this.y = clothData.height;
            }
            else if (this.y <= -clothData.height)
            {
                this.py *= -1 * clothData.bounce;
                this.y = -clothData.height;
            }

            if (this.z >= clothData.height)
            {
                this.pz = clothData.height + (clothData.height - this.pz) * clothData.bounce;
                this.z = clothData.height;
            }
            else if (this.z <= -clothData.height)
            {
                this.pz *= -1 * clothData.bounce;
                this.z = -clothData.height;
            }


            return this;
        }


        public void Resolve()
        {
            if (this.pinAt != null)
            {
                this.x = this.pinAt.x;
                this.y = this.pinAt.y;
                this.z = this.pinAt.z;
                return;
            }

            for (var index = 0; index < this.constraints.Count; index++)
            {
                var constraint = this.constraints[index];
                constraint.Resolve();
            }
        }

        public void Attach(Point point)
        {
            this.constraints.Add(new Constraint(this, point, clothData));
        }

        public void Free(Constraint constraint)
        {
            constraints.RemoveAt(this.constraints.IndexOf(constraint));
        }

        void AddForce(float x, float y, float z)
        {
            this.vx += x;
            this.vy += y;
            this.vz += z;
        }

        public void Pin(float pinX, float pinY, float pinZ)
        {
            this.pinAt = new Point(pinX, pinY, pinZ, null);
        }

        public static implicit operator Vector3(Point t)
        {
            return new Vector3(t.x, t.y, t.z);
        }

        public static implicit operator Vector4(Point t)
        {
            return new Vector4(t.x, t.y, t.z);
        }
    }


    class Constraint
    {
        private Point p1;
        private Point p2;
        private float length;
        private ClothScript clothData;

        public Constraint(Point p1, Point p2, ClothScript clothData)
        {
            this.p1 = p1;
            this.p2 = p2;
            this.length = clothData.spacing;
            this.clothData = clothData;
        }

        public Constraint Resolve()
        {
            var dx = this.p1.x - this.p2.x;
            var dy = this.p1.y - this.p2.y;
            var dz = this.p1.z - this.p2.z;
            var dist = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);

            if (dist < this.length) return this;

            var diff = (this.length - dist) / dist;

            if (dist > clothData.tearDist) this.p1.Free(this);

            var mul = diff * 0.5f * (1 - this.length / dist);

            var px = dx * mul;
            var py = dy * mul;
            var pz = dz * mul;

            if (this.p1.pinAt == null)
            {
                this.p1.x += px;
                this.p1.y += py;
                this.p1.z += pz;
            }

            if (this.p2.pinAt == null)
            {
                this.p2.x -= px;
                this.p2.y -= py;
                this.p2.z -= pz;
            }


            return this;
        }
    }


    class Cloth
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
                        point.Pin(point.x, point.y, point.z);
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

    [HideInInspector] public Mesh mesh;
    public MouseInfo mouse;

    private TearableCloth.Cloth cloth;
    public float width = 800;
    public float height = 1260;

    private void Awake()
    {
        mouse = new MouseInfo();
        mouse.influence = 26;
        mouse.cut = 8;

        mesh = GetComponent<MeshFilter>().mesh;
        cloth = new TearableCloth.Cloth(this);
    }

    private void Update()
    {
        mouse.update();

        cloth.update();
    }

    public class MouseInfo
    {
        public float x;
        public float y;
        public float influence;
        public float px;
        public float py;
        public float cut;
        public bool down;

        public void update()
        {
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
        private float px;
        private float py;
        private float vx;
        private float vy;
        public float pinX;
        public float pinY;

        private List<Constraint> constraints;
        private ClothScript clothData;

        public Point(float x, float y, ClothScript clothData)
        {
            this.x = x;
            this.y = y;
            this.px = x;
            this.py = y;
            vx = 0;
            vy = 0;
            pinX = 0;
            pinY = 0;
            this.clothData = clothData;
            constraints = new List<Constraint>();
        }

        public Point update(float delta)
        {
            if (this.pinX != 0 && this.pinY != 0) return this;

            if (clothData.mouse.down)
            {
                var dx = this.x - clothData.mouse.x;
                var dy = this.y - clothData.mouse.y;

                var dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist < clothData.mouse.influence)
                {
                    this.px = this.x - (clothData.mouse.x - clothData.mouse.px);
                    this.py = this.y - (clothData.mouse.y - clothData.mouse.py);
                }
                else if (dist < clothData.mouse.cut)
                {
                    this.constraints = new List<Constraint>();
                }
            }

            this.addForce(0, clothData.gravity);

            var nx = this.x + (this.x - this.px) * clothData.friction + this.vx * delta;
            var ny = this.y + (this.y - this.py) * clothData.friction + this.vy * delta;

            this.px = this.x;
            this.py = this.y;

            this.x = nx;
            this.y = ny;

            this.vy = this.vx = 0;


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

            return this;
        }


        public void resolve()
        {
            if (this.pinX != 0 && this.pinY != 0)
            {
                this.x = this.pinX;
                this.y = this.pinY;
                return;
            }

            for (var index = 0; index < this.constraints.Count; index++)
            {
                var constraint = this.constraints[index];
                constraint.resolve();
            }
        }

        public void attach(Point point)
        {
            this.constraints.Add(new Constraint(this, point, clothData));
        }

        public void free(Constraint constraint)
        {
            constraints.RemoveAt(this.constraints.IndexOf(constraint));
        }

        void addForce(float x, float y)
        {
            this.vx += x;
            this.vy += y;
        }

        public void pin(float pinX, float pinY)
        {
            this.pinX = pinX;
            this.pinY = pinY;
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

        public Constraint resolve()
        {
            var dx = this.p1.x - this.p2.x;
            var dy = this.p1.y - this.p2.y;
            var dist = Mathf.Sqrt(dx * dx + dy * dy);

            if (dist < this.length) return this;

            var diff = (this.length - dist) / dist;

            if (dist > clothData.tearDist) this.p1.free(this);

            var mul = diff * 0.5f * (1 - this.length / dist);

            var px = dx * mul;
            var py = dy * mul;

            if (this.p1.pinX == 0)
            {
                this.p1.x += px;
            }

            if (this.p1.pinY == 0)
            {
                this.p1.y += py;
            }

            if (this.p2.pinX == 0)
            {
                this.p2.x += px;
            }

            if (this.p2.pinY == 0)
            {
                this.p2.y += py;
            }

            return this;
        }
    }


    class Cloth
    {
        private List<Point> points;
        private ClothScript clothData;

        private Vector3[] mVertices;


        public Vector3[] vertices
        {
            get
            {
                return mVertices != null ? clothData.mesh.vertices = mVertices : (mVertices = clothData.mesh.vertices);
            }
        }

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
                    var point = new Point(startX + x * clothData.spacing, -y * clothData.spacing, clothData);

                    if (y == 0)
                        point.pin(point.x, point.y);
                    if (x != 0)
                        point.attach(this.points[this.points.Count - 1]);
                    if (y != 0)
                        point.attach(this.points[x + (y - 1) * (clothData.clothX + 1)]);

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
                clothData.mesh.RecalculateBounds();
            }
        }

        public void update()
        {
            var i = clothData.accuracy;

            while (i-- > 0)
            {
                for (var index = 0; index < this.points.Count; index++)
                {
                    var point = this.points[index];
                    point.resolve();

                    var v = vertices[index];
                    v.x = point.x;
                    v.y = point.y;
                    vertices[index] = v;
                }
            }

            var delta = Time.deltaTime * Time.deltaTime;
            for (var index = 0; index < this.points.Count; index++)
            {
                var point = this.points[index];
                point.update(delta);
            }
        }
    }
}
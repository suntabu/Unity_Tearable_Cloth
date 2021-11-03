using System.Collections.Generic;
using UnityEngine;

namespace TearableCloth
{
    public class Point
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
        internal bool isPinAt;

        private List<Constraint> constraints;
        private ClothScript clothScript;

        private float pinX, pinY, pinZ;

        public Point(float x, float y, float z, ClothScript clothScript)
        {
            this.x = pinX = x;
            this.y = pinY = y;
            this.z = pinZ = z;
            this.px = x;
            this.py = y;
            this.pz = z;
            vx = 0;
            vy = 0;
            vz = 0;
            this.clothScript = clothScript;
            constraints = new List<Constraint>();
            hits = new RaycastHit[1];
            isPinAt = false;
        }

        public override string ToString()
        {
            return $"({x},{y},{z}) Constraint:{constraints.Count}";
        }

        private RaycastHit[] hits;

        public Point Update(float delta, float force, float particleSize)
        {
            if (this.isPinAt)
            {
                var v = new Vector3(pinX, pinY, pinZ);
                v = clothScript.transform.TransformPoint(v);
                x = v.x;
                y = v.y;
                z = v.z;
                return this;
            }

            // var worldPos =  clothScript.transform.TransformPoint(this);
            // Debug.Log(this + "  ->  " + worldPos);

            var worldPos = (this);
            if (clothScript.mouse.down)
            {
                var distance = Vector2.Distance(worldPos,
                    Camera.main.ScreenToWorldPoint(new Vector3(clothScript.mouse.x, clothScript.mouse.y)));


                if (distance < clothScript.mouse.influence)
                {
                    this.px = this.x - (clothScript.mouse.x - clothScript.mouse.px);
                    this.py = this.y - (clothScript.mouse.y - clothScript.mouse.py);
                    this.pz = this.z - force;
                }
                else if (distance < clothScript.mouse.cut)
                {
                    this.constraints = new List<Constraint>();
                }
            }

            this.AddForce(0, clothScript.gravity, 0);

            var dx = (this.x - this.px) * clothScript.friction + this.vx * delta;
            var dy = (this.y - this.py) * clothScript.friction + this.vy * delta;
            var dz = (this.z - this.pz) * clothScript.friction + this.vz * delta;

            var dir = new Vector3(dx, dy, dz);
            var l = dir.magnitude;
            dir *= 1f / l;
            if (Physics.SphereCastNonAlloc(worldPos, particleSize,
                dir, hits, l) > 0)
            {
                RaycastHit raycastHit = hits[0];

                //TODO: 快速收敛
                var n = Vector3.Reflect(dir, raycastHit.normal) * l;
                // var n = dir + raycastHit.normal;
                dx = n.x;
                dy = n.y;
                dz = n.z;

                // dx = dy = dz = 0;
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


            if (this.x >= clothScript.width)
            {
                this.px = clothScript.width + (clothScript.width - this.px) * clothScript.bounce;
                this.x = clothScript.width;
            }
            else if (this.x <= -clothScript.width)
            {
                this.px *= -1 * clothScript.bounce;
                this.x = -clothScript.width;
            }

            if (this.y >= clothScript.height)
            {
                this.py = clothScript.height + (clothScript.height - this.py) * clothScript.bounce;
                this.y = clothScript.height;
            }
            else if (this.y <= -clothScript.height)
            {
                this.py *= -1 * clothScript.bounce;
                this.y = -clothScript.height;
            }

            if (this.z >= clothScript.height)
            {
                this.pz = clothScript.height + (clothScript.height - this.pz) * clothScript.bounce;
                this.z = clothScript.height;
            }
            else if (this.z <= -clothScript.height)
            {
                this.pz *= -1 * clothScript.bounce;
                this.z = -clothScript.height;
            }


            return this;
        }


        public void Resolve()
        {
            if (this.isPinAt)
            {
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
            this.constraints.Add(new Constraint(this, point, clothScript));
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

        public void Pin()
        {
            isPinAt = true;
        }

        public static implicit operator Vector3(Point t)
        {
            return new Vector3(t.x, t.y, t.z);
        }

        public static implicit operator Vector4(Point t)
        {
            return new Vector4(t.x, t.y, t.z);
        }

        public static implicit operator Vector2(Point t)
        {
            return new Vector2(t.x, t.y);
        }
    }

    public class Constraint
    {
        private Point p1;
        private Point p2;
        private float length;
        private ClothScript clothScript;

        public Constraint(Point p1, Point p2, ClothScript clothScript)
        {
            this.p1 = p1;
            this.p2 = p2;
            this.length = clothScript.spacing;
            this.clothScript = clothScript;
        }

        public Constraint Resolve()
        {
            var dx = this.p1.x - this.p2.x;
            var dy = this.p1.y - this.p2.y;
            var dz = this.p1.z - this.p2.z;
            var dist = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);

            if (dist < this.length) return this;

            var diff = (this.length - dist) / dist;

            if (dist > clothScript.tearDist) this.p1.Free(this);

            var mul = diff * 0.5f * (1 - this.length / dist);

            var px = dx * mul;
            var py = dy * mul;
            var pz = dz * mul;

            if (!this.p1.isPinAt)
            {
                this.p1.x += px;
                this.p1.y += py;
                this.p1.z += pz;
            }

            if (!this.p2.isPinAt)
            {
                this.p2.x -= px;
                this.p2.y -= py;
                this.p2.z -= pz;
            }

            return this;
        }
    }
}
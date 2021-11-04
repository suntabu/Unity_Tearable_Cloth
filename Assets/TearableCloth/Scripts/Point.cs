using System.Collections.Generic;
using UnityEngine;

namespace TearableCloth
{
    public class Point
    {
        public float x;
        public float y;
        public float z;

        public float mass;

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
            px = x;
            py = y;
            pz = z;
            vx = 0;
            vy = 0;
            vz = 0;
            this.clothScript = clothScript;
            constraints = new List<Constraint>();
            hits = new RaycastHit[1];
            colliders = new Collider[1];
            isPinAt = false;
        }

        public override string ToString()
        {
            return $"({x},{y},{z}) Constraint:{constraints.Count}";
        }

        private RaycastHit[] hits;
        private Collider[] colliders;

        public Point Update(float delta, float force, float particleSize)
        {
            if (isPinAt)
            {
                var v = new Vector3(pinX, pinY, pinZ);
                v = clothScript.transform.TransformPoint(v);
                x = v.x;
                y = v.y;
                z = v.z;
                return this;
            }


            var worldPos = (this);
            if (clothScript.mouse.down)
            {
                var distance = Vector2.Distance(worldPos,
                    Camera.main.ScreenToWorldPoint(new Vector3(clothScript.mouse.x, clothScript.mouse.y)));


                if (distance < clothScript.mouse.influence)
                {
                    px = x - (clothScript.mouse.x - clothScript.mouse.px);
                    py = y - (clothScript.mouse.y - clothScript.mouse.py);
                    pz = z - force;
                }
                else if (distance < clothScript.mouse.cut)
                {
                    constraints = new List<Constraint>();
                }
            }

            AddForce(0, clothScript.gravity, 0);

            var dx = (x - px) * clothScript.friction + vx * delta;
            var dy = (y - py) * clothScript.friction + vy * delta;
            var dz = (z - pz) * clothScript.friction + vz * delta;

            var dir = new Vector3(dx, dy, dz);
            var l = dir.magnitude;
            var dirNormal = dir * (1f / l);


            if (Physics.OverlapSphereNonAlloc(worldPos, particleSize, colliders) > 0)
            {
                var c = colliders[0];
                var p = c.ClosestPoint(worldPos);
                var n = (p - worldPos);
                dir += n - n.normalized * particleSize;

                dx = dir.x;
                dy = dir.y;
                dz = dir.z;
            }
            else
            {
                if (Physics.SphereCastNonAlloc(worldPos, particleSize,
                    dirNormal, hits, l) > 0)
                {
                    RaycastHit raycastHit = hits[0];

                    // var n = Vector3.Reflect(dir, raycastHit.normal) * l;
                    var n = Vector3.ProjectOnPlane(dir, raycastHit.normal) - Vector3.Project(dir, raycastHit.normal);
                    // var n = dir + raycastHit.normal;

                    dx = n.x;
                    dy = n.y;
                    dz = n.z;
                    // dx = dy = dz = 0;
                }
            }


            var nx = x + dx;
            var ny = y + dy;
            var nz = z + dz;

            px = x;
            py = y;
            pz = z;

            x = nx;
            y = ny;
            z = nz;

            vy = vx = vz = 0;


            if (x >= clothScript.width)
            {
                px = clothScript.width + (clothScript.width - px) * clothScript.bounce;
                x = clothScript.width;
            }
            else if (x <= -clothScript.width)
            {
                px *= -1 * clothScript.bounce;
                x = -clothScript.width;
            }

            if (y >= clothScript.height)
            {
                py = clothScript.height + (clothScript.height - py) * clothScript.bounce;
                y = clothScript.height;
            }
            else if (y <= -clothScript.height)
            {
                py *= -1 * clothScript.bounce;
                y = -clothScript.height;
            }

            if (z >= clothScript.height)
            {
                pz = clothScript.height + (clothScript.height - pz) * clothScript.bounce;
                z = clothScript.height;
            }
            else if (z <= -clothScript.height)
            {
                pz *= -1 * clothScript.bounce;
                z = -clothScript.height;
            }

            return this;
        }


        public void Resolve()
        {
            if (isPinAt)
            {
                return;
            }

            for (var index = 0; index < constraints.Count; index++)
            {
                var constraint = constraints[index];
                constraint.Resolve();
            }
        }

        public void Attach(Point point)
        {
            constraints.Add(new Constraint(this, point, clothScript));
        }

        public void Free(Constraint constraint)
        {
            constraints.RemoveAt(constraints.IndexOf(constraint));
        }

        void AddForce(float x, float y, float z)
        {
            vx += x;
            vy += y;
            vz += z;
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
            length = clothScript.spacing;
            this.clothScript = clothScript;
        }

        public Constraint Resolve()
        {
            var dx = p1.x - p2.x;
            var dy = p1.y - p2.y;
            var dz = p1.z - p2.z;
            var dist = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);

            if (dist < length) return this;

            var diff = (length - dist) / dist;

            if (dist > clothScript.tearDist) p1.Free(this);

            var mul = diff * 0.15f * (1 - length / dist);

            var px = dx * mul;
            var py = dy * mul;
            var pz = dz * mul;

            if (!p1.isPinAt)
            {
                p1.x += px;
                p1.y += py;
                p1.z += pz;
            }

            if (!p2.isPinAt)
            {
                p2.x -= px;
                p2.y -= py;
                p2.z -= pz;
            }

            return this;
        }
    }
}
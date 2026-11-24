using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WegInside.EditorTools
{
    sealed class MotorMeshBuilder
    {
        readonly List<Vector3> vertices = new();
        readonly List<Vector3> normals = new();
        readonly List<Vector2> uv = new();
        readonly List<int> indices = new();

        public static Vector3 Polar(float x, float r, float a) => new(x, Mathf.Cos(a) * r, Mathf.Sin(a) * r);

        void Vertex(Vector3 p, Vector3 n, Vector2 tex)
        {
            vertices.Add(p);
            normals.Add(n);
            uv.Add(tex);
        }

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 na, Vector3 nb, Vector3 nc, Vector3 nd)
        {
            int i = vertices.Count;
            Vertex(a, na, Vector2.zero);
            Vertex(b, nb, Vector2.right);
            Vertex(c, nc, Vector2.one);
            Vertex(d, nd, Vector2.up);
            indices.Add(i);
            indices.Add(i + 1);
            indices.Add(i + 2);
            indices.Add(i);
            indices.Add(i + 2);
            indices.Add(i + 3);
        }

        void Flat(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            Quad(a, b, c, d, n, n, n, n);
        }

        // Profile follows the outer surface toward +X and the inner surface toward -X.
        public void Lathe(Vector3 center, int sides, params Vector2[] profile)
        {
            for (int j = 1; j < profile.Length; j++)
            {
                Vector2 a = profile[j - 1], b = profile[j];
                Vector2 slope = (b - a).normalized;
                for (int i = 0; i < sides; i++)
                {
                    float p = 2 * Mathf.PI * i / sides, q = 2 * Mathf.PI * (i + 1) / sides;
                    Vector3 np = Polar(-slope.y, slope.x, p), nq = Polar(-slope.y, slope.x, q);
                    Quad(center + Polar(a.x, a.y, p), center + Polar(a.x, a.y, q),
                        center + Polar(b.x, b.y, q), center + Polar(b.x, b.y, p), np, nq, nq, np);
                }
            }
        }

        public void Ring(float x, float width, float inner, float outer, float bevel = .02f, int sides = 64, Vector3 offset = default)
        {
            float a = x - width / 2, b = x + width / 2, e = bevel;
            Lathe(offset, sides, new(a, inner + e), new(a, outer - e), new(a + e, outer),
                new(b - e, outer), new(b, outer - e), new(b, inner + e),
                new(b - e, inner), new(a + e, inner), new(a, inner + e));
        }

        public void Pin(float x, float width, float radius, float bevel = .015f, int sides = 32, Vector3 offset = default)
        {
            float a = x - width / 2, b = x + width / 2;
            Lathe(offset, sides, new(a, 0), new(a, radius - bevel), new(a + bevel, radius),
                new(b - bevel, radius), new(b, radius - bevel), new(b, 0));
        }

        public void Box(Vector3 center, Vector3 size, float bevel, Quaternion rotation = default)
        {
            if (rotation.Equals(default(Quaternion))) rotation = Quaternion.identity;
            var h = size * .5f;
            Vector2[] corners = { new(h.y-bevel,h.z), new(-h.y+bevel,h.z), new(-h.y,h.z-bevel),
                new(-h.y,-h.z+bevel), new(-h.y+bevel,-h.z), new(h.y-bevel,-h.z),
                new(h.y,-h.z+bevel), new(h.y,h.z-bevel) };
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 p = corners[i], q = corners[(i+1)%corners.Length];
                Vector2 pi = new(p.x * (h.y-bevel)/h.y, p.y * (h.z-bevel)/h.z);
                Vector2 qi = new(q.x * (h.y-bevel)/h.y, q.y * (h.z-bevel)/h.z);
                Vector3 A(float x, Vector2 yz) => center + rotation * new Vector3(x, yz.x, yz.y);
                Vector3 a = A(-h.x + bevel,p), b = A(-h.x + bevel,q);
                Vector3 c = A(h.x - bevel,q), d = A(h.x - bevel,p);
                Flat(a,b,c,d);
                Flat(A(-h.x,pi),A(-h.x,qi),b,a);
                Flat(d,c,A(h.x,qi),A(h.x,pi));
                Flat(A(-h.x,Vector2.zero),A(-h.x,qi),A(-h.x,pi),A(-h.x,pi));
                Flat(A(h.x,Vector2.zero),A(h.x,pi),A(h.x,qi),A(h.x,qi));
            }
        }

        public void MountPad(Vector3 center, float height, float halfSize, float hole)
        {
            const int n = 32;
            for (int i = 0; i < n; i++)
            {
                Vector3 P(int step, float y, bool outer)
                {
                    float a = 2 * Mathf.PI * step / n;
                    float x = Mathf.Cos(a), z = Mathf.Sin(a);
                    float r = outer ? Mathf.Min(halfSize / Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)),
                        (2*halfSize-.07f)/(Mathf.Abs(x)+Mathf.Abs(z))) : hole;
                    return center + new Vector3(x*r, y, z*r);
                }
                float b = -height/2, t = height/2;
                Flat(P(i,t,false),P(i+1,t,false),P(i+1,t,true),P(i,t,true));
                Flat(P(i,b,true),P(i+1,b,true),P(i+1,b,false),P(i,b,false));
                Flat(P(i,t,true),P(i+1,t,true),P(i+1,b,true),P(i,b,true));
                Flat(P(i,b,false),P(i+1,b,false),P(i+1,t,false),P(i,t,false));
            }
        }

        public void Tube(IReadOnlyList<Vector3> path, float radius, int sides, bool loop = false)
        {
            int segments = loop ? path.Count : path.Count - 1;
            Vector3 N(int index, int sidesIndex)
            {
                int prev = loop ? (index + path.Count - 1) % path.Count : Mathf.Max(0, index - 1);
                int next = loop ? (index + 1) % path.Count : Mathf.Min(path.Count - 1, index + 1);
                Vector3 forward = (path[next] - path[prev]).normalized;
                Vector3 reference = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) < .93f ? Vector3.up : Vector3.forward;
                Vector3 u = Vector3.Cross(reference, forward).normalized;
                Vector3 v = Vector3.Cross(forward, u);
                float a = 2 * Mathf.PI * sidesIndex / sides;
                return u * Mathf.Cos(a) + v * Mathf.Sin(a);
            }
            for (int i = 0; i < segments; i++)
            {
                int k = (i + 1) % path.Count;
                for (int j = 0; j < sides; j++)
                {
                    Vector3 a = N(i,j), b = N(i,j+1), c = N(k,j+1), d = N(k,j);
                    Quad(path[i]+radius*a,path[i]+radius*b,path[k]+radius*c,path[k]+radius*d,a,b,c,d);
                }
            }
        }

        public void Sphere(Vector3 center, float radius)
        {
            const int slices = 16, bands = 8;
            for (int j = 0; j < bands; j++)
            {
                float p = Mathf.PI * (1f - (float)j / bands), q = Mathf.PI * (1f - (float)(j+1) / bands);
                for (int i = 0; i < slices; i++)
                {
                    float a = i * 2 * Mathf.PI / slices, b = (i+1) * 2 * Mathf.PI / slices;
                    Vector3 n0 = Polar(Mathf.Cos(p),Mathf.Sin(p),a), n1 = Polar(Mathf.Cos(p),Mathf.Sin(p),b);
                    Vector3 n2 = Polar(Mathf.Cos(q),Mathf.Sin(q),b), n3 = Polar(Mathf.Cos(q),Mathf.Sin(q),a);
                    Quad(center+radius*n0,center+radius*n1,center+radius*n2,center+radius*n3,n0,n1,n2,n3);
                }
            }
        }

        public void FanBlade(float angle)
        {
            Vector3 P(float t, float edge, float thickness)
            {
                float r = Mathf.Lerp(.235f,.96f,t);
                return Polar(-.13f + .12f*t + edge*.235f*(.55f+.45f*t) + thickness, r,
                    angle - .40f*t*t + edge*(.55f-.31f*t));
            }
            const int steps = 9;
            for (int i = 0; i < steps; i++)
            {
                float a = (float)i/steps, b = (float)(i+1)/steps;
                Flat(P(a,0,.019f),P(b,0,.019f),P(b,1,.019f),P(a,1,.019f));
                Flat(P(a,1,-.019f),P(b,1,-.019f),P(b,0,-.019f),P(a,0,-.019f));
                Flat(P(a,0,-.019f),P(b,0,-.019f),P(b,0,.019f),P(a,0,.019f));
                Flat(P(a,1,.019f),P(b,1,.019f),P(b,1,-.019f),P(a,1,-.019f));
            }
            Flat(P(1,0,.019f),P(1,0,-.019f),P(1,1,-.019f),P(1,1,.019f));
        }

        public void KeyedShaftTip()
        {
            const int sides = 64;
            const float left = -2.46f, right = -1.75f, r = .153f;
            Vector3 P(float x, int step, bool cut)
            {
                float a = step * 2 * Mathf.PI / sides;
                Vector3 p = Polar(x,r,a);
                if (cut && (step <= 4 || step >= sides-4)) p.y = .112f;
                return p;
            }
            for (int i = 0; i < sides; i++)
            {
                Vector3 a = P(left,i,true), b = P(left,i+1,true);
                Vector3 c = P(-1.96f,i+1,true), d = P(-1.96f,i,true);
                Flat(a,b,c,d);
                Flat(a,new Vector3(left,0,0),b,b);
                Flat(d,c,P(-1.96f,i+1,false),P(-1.96f,i,false));
                Flat(P(-1.96f,i,false),P(-1.96f,i+1,false),P(right,i+1,false),P(right,i,false));
            }
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0,uv);
            mesh.SetTriangles(indices,0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

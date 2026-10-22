// Um UnityEngine de mentira, só o bastante para rodar o código visual do Unity (Scripts/Visual) fora do Unity e exportar
// o que ele monta: transformações, malhas, materiais e texturas. Usado por tools/unity-ver.js para comparar o mundo do
// Unity com o do navegador. Nada aqui desenha; os números (vetores, quatérnios, cores) seguem as fórmulas do Unity.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public static class Mathf
    {
        public const float PI = (float)Math.PI, Deg2Rad = PI / 180f, Rad2Deg = 180f / PI, Epsilon = 1.401298E-45f, Infinity = float.PositiveInfinity;
        public static float Sin(float f) { return (float)Math.Sin(f); }
        public static float Cos(float f) { return (float)Math.Cos(f); }
        public static float Tan(float f) { return (float)Math.Tan(f); }
        public static float Asin(float f) { return (float)Math.Asin(f); }
        public static float Acos(float f) { return (float)Math.Acos(f); }
        public static float Atan(float f) { return (float)Math.Atan(f); }
        public static float Atan2(float y, float x) { return (float)Math.Atan2(y, x); }
        public static float Sqrt(float f) { return (float)Math.Sqrt(f); }
        public static float Abs(float f) { return Math.Abs(f); }
        public static int Abs(int f) { return Math.Abs(f); }
        public static float Min(float a, float b) { return a < b ? a : b; }
        public static float Max(float a, float b) { return a > b ? a : b; }
        public static float Min(params float[] v) { float m = v[0]; foreach (var x in v) if (x < m) m = x; return m; }
        public static float Max(params float[] v) { float m = v[0]; foreach (var x in v) if (x > m) m = x; return m; }
        public static int Min(int a, int b) { return a < b ? a : b; }
        public static int Max(int a, int b) { return a > b ? a : b; }
        public static float Pow(float f, float p) { return (float)Math.Pow(f, p); }
        public static float Exp(float p) { return (float)Math.Exp(p); }
        public static float Log(float f) { return (float)Math.Log(f); }
        public static float Ceil(float f) { return (float)Math.Ceiling(f); }
        public static float Floor(float f) { return (float)Math.Floor(f); }
        public static float Round(float f) { return (float)Math.Round(f); }
        public static int CeilToInt(float f) { return (int)Math.Ceiling(f); }
        public static int FloorToInt(float f) { return (int)Math.Floor(f); }
        public static int RoundToInt(float f) { return (int)Math.Round(f); }
        public static float Sign(float f) { return f >= 0 ? 1 : -1; }
        public static float Clamp(float v, float a, float b) { return v < a ? a : v > b ? b : v; }
        public static int Clamp(int v, int a, int b) { return v < a ? a : v > b ? b : v; }
        public static float Clamp01(float v) { return Clamp(v, 0, 1); }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * Clamp01(t); }
        public static float LerpUnclamped(float a, float b, float t) { return a + (b - a) * t; }
        public static float InverseLerp(float a, float b, float v) { return a != b ? Clamp01((v - a) / (b - a)) : 0; }
        public static float SmoothStep(float a, float b, float t) { t = Clamp01(t); t = -2 * t * t * t + 3 * t * t; return b * t + a * (1 - t); }
        public static float Repeat(float t, float l) { return Clamp(t - Floor(t / l) * l, 0, l); }
        public static float PingPong(float t, float l) { t = Repeat(t, l * 2); return l - Abs(t - l); }
        public static float MoveTowards(float c, float t, float d) { return Abs(t - c) <= d ? t : c + Sign(t - c) * d; }
        public static float DeltaAngle(float c, float t) { float d = Repeat(t - c, 360); if (d > 180) d -= 360; return d; }
        public static float LerpAngle(float a, float b, float t) { float d = Repeat(b - a, 360); if (d > 180) d -= 360; return a + d * Clamp01(t); }
        public static bool Approximately(float a, float b) { return Abs(b - a) < Max(1E-06f * Max(Abs(a), Abs(b)), Epsilon * 8); }
        public static float PerlinNoise(float x, float y) { return .5f; }
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(0, 0); } }
        public static Vector2 one { get { return new Vector2(1, 1); } }
        public static Vector2 up { get { return new Vector2(0, 1); } }
        public static Vector2 right { get { return new Vector2(1, 0); } }
        public float magnitude { get { return (float)Math.Sqrt(x * x + y * y); } }
        public float sqrMagnitude { get { return x * x + y * y; } }
        public Vector2 normalized { get { float m = magnitude; return m > 1e-5f ? new Vector2(x / m, y / m) : zero; } }
        public void Normalize() { this = normalized; }
        public float this[int i] { get { return i == 0 ? x : y; } set { if (i == 0) x = value; else y = value; } }
        public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
        public static Vector2 operator -(Vector2 a) { return new Vector2(-a.x, -a.y); }
        public static Vector2 operator *(Vector2 a, float d) { return new Vector2(a.x * d, a.y * d); }
        public static Vector2 operator *(float d, Vector2 a) { return new Vector2(a.x * d, a.y * d); }
        public static Vector2 operator *(Vector2 a, Vector2 b) { return new Vector2(a.x * b.x, a.y * b.y); }
        public static Vector2 operator /(Vector2 a, float d) { return new Vector2(a.x / d, a.y / d); }
        public static bool operator ==(Vector2 a, Vector2 b) { return (a - b).sqrMagnitude < 9.99999944E-11f; }
        public static bool operator !=(Vector2 a, Vector2 b) { return !(a == b); }
        public override bool Equals(object o) { return o is Vector2 && this == (Vector2)o; }
        public override int GetHashCode() { return x.GetHashCode() ^ y.GetHashCode() << 2; }
        public static float Dot(Vector2 a, Vector2 b) { return a.x * b.x + a.y * b.y; }
        public static float Distance(Vector2 a, Vector2 b) { return (a - b).magnitude; }
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) { t = Mathf.Clamp01(t); return a + (b - a) * t; }
        public static implicit operator Vector2(Vector3 v) { return new Vector2(v.x, v.y); }
        public static implicit operator Vector3(Vector2 v) { return new Vector3(v.x, v.y, 0); }
        public override string ToString() { return "(" + x + ", " + y + ")"; }
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y) { this.x = x; this.y = y; z = 0; }
        public static Vector3 zero { get { return new Vector3(0, 0, 0); } }
        public static Vector3 one { get { return new Vector3(1, 1, 1); } }
        public static Vector3 up { get { return new Vector3(0, 1, 0); } }
        public static Vector3 down { get { return new Vector3(0, -1, 0); } }
        public static Vector3 right { get { return new Vector3(1, 0, 0); } }
        public static Vector3 left { get { return new Vector3(-1, 0, 0); } }
        public static Vector3 forward { get { return new Vector3(0, 0, 1); } }
        public static Vector3 back { get { return new Vector3(0, 0, -1); } }
        public float magnitude { get { return (float)Math.Sqrt(x * x + y * y + z * z); } }
        public float sqrMagnitude { get { return x * x + y * y + z * z; } }
        public Vector3 normalized { get { float m = magnitude; return m > 1e-5f ? this / m : zero; } }
        public void Normalize() { this = normalized; }
        public float this[int i] { get { return i == 0 ? x : i == 1 ? y : z; } set { if (i == 0) x = value; else if (i == 1) y = value; else z = value; } }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static Vector3 operator -(Vector3 a) { return new Vector3(-a.x, -a.y, -a.z); }
        public static Vector3 operator *(Vector3 a, float d) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        public static Vector3 operator *(float d, Vector3 a) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        public static Vector3 operator /(Vector3 a, float d) { return new Vector3(a.x / d, a.y / d, a.z / d); }
        public static bool operator ==(Vector3 a, Vector3 b) { return (a - b).sqrMagnitude < 9.99999944E-11f; }
        public static bool operator !=(Vector3 a, Vector3 b) { return !(a == b); }
        public override bool Equals(object o) { return o is Vector3 && this == (Vector3)o; }
        public override int GetHashCode() { return x.GetHashCode() ^ y.GetHashCode() << 2 ^ z.GetHashCode() >> 2; }
        public static float Dot(Vector3 a, Vector3 b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
        public static Vector3 Cross(Vector3 a, Vector3 b) { return new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x); }
        public static float Distance(Vector3 a, Vector3 b) { return (a - b).magnitude; }
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { t = Mathf.Clamp01(t); return a + (b - a) * t; }
        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t) { return a + (b - a) * t; }
        public static Vector3 Scale(Vector3 a, Vector3 b) { return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z); }
        public static Vector3 Normalize(Vector3 v) { return v.normalized; }
        public static Vector3 Project(Vector3 v, Vector3 n) { float d = Dot(n, n); return d < 1e-12f ? zero : n * Dot(v, n) / d; }
        public static Vector3 ProjectOnPlane(Vector3 v, Vector3 n) { return v - Project(v, n); }
        public static float Angle(Vector3 a, Vector3 b) { float d = (float)Math.Sqrt(a.sqrMagnitude * b.sqrMagnitude); return d < 1e-15f ? 0 : Mathf.Acos(Mathf.Clamp(Dot(a, b) / d, -1, 1)) * Mathf.Rad2Deg; }
        public static Vector3 Min(Vector3 a, Vector3 b) { return new Vector3(Math.Min(a.x, b.x), Math.Min(a.y, b.y), Math.Min(a.z, b.z)); }
        public static Vector3 Max(Vector3 a, Vector3 b) { return new Vector3(Math.Max(a.x, b.x), Math.Max(a.y, b.y), Math.Max(a.z, b.z)); }
        public static Vector3 MoveTowards(Vector3 c, Vector3 t, float d) { var v = t - c; float m = v.magnitude; return m <= d || m == 0 ? t : c + v / m * d; }
        public static Vector3 Slerp(Vector3 a, Vector3 b, float t) { return Lerp(a, b, t); }
        public override string ToString() { return "(" + x + ", " + y + ", " + z + ")"; }
    }

    public struct Vector4
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Vector4 zero { get { return new Vector4(); } }
        public static implicit operator Vector4(Vector3 v) { return new Vector4(v.x, v.y, v.z, 0); }
        public static implicit operator Vector3(Vector4 v) { return new Vector3(v.x, v.y, v.z); }
    }

    public struct Quaternion
    {
        public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity { get { return new Quaternion(0, 0, 0, 1); } }
        public static Quaternion operator *(Quaternion a, Quaternion b)
        {
            return new Quaternion(a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y, a.w * b.y + a.y * b.w + a.z * b.x - a.x * b.z, a.w * b.z + a.z * b.w + a.x * b.y - a.y * b.x, a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);
        }
        public static Vector3 operator *(Quaternion q, Vector3 p)
        {
            float x = q.x * 2, y = q.y * 2, z = q.z * 2, xx = q.x * x, yy = q.y * y, zz = q.z * z, xy = q.x * y, xz = q.x * z, yz = q.y * z, wx = q.w * x, wy = q.w * y, wz = q.w * z;
            return new Vector3((1 - (yy + zz)) * p.x + (xy - wz) * p.y + (xz + wy) * p.z, (xy + wz) * p.x + (1 - (xx + zz)) * p.y + (yz - wx) * p.z, (xz - wy) * p.x + (yz + wx) * p.y + (1 - (xx + yy)) * p.z);
        }
        public static Quaternion AngleAxis(float graus, Vector3 eixo) { eixo = eixo.normalized; float a = graus * Mathf.Deg2Rad / 2, s = Mathf.Sin(a); return new Quaternion(eixo.x * s, eixo.y * s, eixo.z * s, Mathf.Cos(a)); }
        // ordem do Unity: Z, depois X, depois Y
        public static Quaternion Euler(float x, float y, float z) { return AngleAxis(y, Vector3.up) * AngleAxis(x, Vector3.right) * AngleAxis(z, Vector3.forward); }
        public static Quaternion Euler(Vector3 e) { return Euler(e.x, e.y, e.z); }
        public static Quaternion Inverse(Quaternion q) { return new Quaternion(-q.x, -q.y, -q.z, q.w); }
        public Quaternion normalized { get { float m = Mathf.Sqrt(x * x + y * y + z * z + w * w); return m > 1e-9f ? new Quaternion(x / m, y / m, z / m, w / m) : identity; } }
        public static float Dot(Quaternion a, Quaternion b) { return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w; }
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t)
        {
            t = Mathf.Clamp01(t); float d = Dot(a, b); if (d < 0) { b = new Quaternion(-b.x, -b.y, -b.z, -b.w); d = -d; }
            if (d > .9995f) return new Quaternion(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t, a.w + (b.w - a.w) * t).normalized;
            float th = Mathf.Acos(d), s = Mathf.Sin(th), wa = Mathf.Sin((1 - t) * th) / s, wb = Mathf.Sin(t * th) / s;
            return new Quaternion(a.x * wa + b.x * wb, a.y * wa + b.y * wb, a.z * wa + b.z * wb, a.w * wa + b.w * wb);
        }
        public static Quaternion Lerp(Quaternion a, Quaternion b, float t) { return Slerp(a, b, t); }
        public static Quaternion FromToRotation(Vector3 de, Vector3 para)
        {
            de = de.normalized; para = para.normalized; float d = Vector3.Dot(de, para);
            if (d < -.999999f) { var ax = Vector3.Cross(Vector3.right, de); if (ax.sqrMagnitude < 1e-6f) ax = Vector3.Cross(Vector3.up, de); return AngleAxis(180, ax); }
            var c = Vector3.Cross(de, para); return new Quaternion(c.x, c.y, c.z, 1 + d).normalized;
        }
        public static Quaternion LookRotation(Vector3 frente, Vector3 cima)
        {
            var f = frente.normalized; var r = Vector3.Cross(cima, f).normalized; if (r.sqrMagnitude < 1e-9f) r = Vector3.right; var u = Vector3.Cross(f, r);
            float m00 = r.x, m01 = u.x, m02 = f.x, m10 = r.y, m11 = u.y, m12 = f.y, m20 = r.z, m21 = u.z, m22 = f.z, tr = m00 + m11 + m22; Quaternion q;
            if (tr > 0) { float s = Mathf.Sqrt(tr + 1) * 2; q = new Quaternion((m21 - m12) / s, (m02 - m20) / s, (m10 - m01) / s, .25f * s); }
            else if (m00 > m11 && m00 > m22) { float s = Mathf.Sqrt(1 + m00 - m11 - m22) * 2; q = new Quaternion(.25f * s, (m01 + m10) / s, (m02 + m20) / s, (m21 - m12) / s); }
            else if (m11 > m22) { float s = Mathf.Sqrt(1 + m11 - m00 - m22) * 2; q = new Quaternion((m01 + m10) / s, .25f * s, (m12 + m21) / s, (m02 - m20) / s); }
            else { float s = Mathf.Sqrt(1 + m22 - m00 - m11) * 2; q = new Quaternion((m02 + m20) / s, (m12 + m21) / s, .25f * s, (m10 - m01) / s); }
            return q.normalized;
        }
        public static Quaternion LookRotation(Vector3 frente) { return LookRotation(frente, Vector3.up); }
        public Vector3 eulerAngles { get { return Vector3.zero; } }
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white { get { return new Color(1, 1, 1, 1); } }
        public static Color black { get { return new Color(0, 0, 0, 1); } }
        public static Color clear { get { return new Color(0, 0, 0, 0); } }
        public static Color red { get { return new Color(1, 0, 0, 1); } }
        public static Color gray { get { return new Color(.5f, .5f, .5f, 1); } }
        public static Color magenta { get { return new Color(1, 0, 1, 1); } }
        public float maxColorComponent { get { return Math.Max(r, Math.Max(g, b)); } }
        static float L2G(float v) { return v <= .0031308f ? v * 12.92f : 1.055f * (float)Math.Pow(v, 1 / 2.4) - .055f; }
        static float G2L(float v) { return v <= .04045f ? v / 12.92f : (float)Math.Pow((v + .055) / 1.055, 2.4); }
        public Color gamma { get { return new Color(L2G(r), L2G(g), L2G(b), a); } }
        public Color linear { get { return new Color(G2L(r), G2L(g), G2L(b), a); } }
        public static Color operator *(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, c.a * k); }
        public static Color operator *(float k, Color c) { return c * k; }
        public static Color operator *(Color a, Color b) { return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a); }
        public static Color operator +(Color a, Color b) { return new Color(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a); }
        public static Color operator -(Color a, Color b) { return new Color(a.r - b.r, a.g - b.g, a.b - b.b, a.a - b.a); }
        public static Color operator /(Color c, float k) { return new Color(c.r / k, c.g / k, c.b / k, c.a / k); }
        public static Color Lerp(Color a, Color b, float t) { t = Mathf.Clamp01(t); return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t); }
        public static Color LerpUnclamped(Color a, Color b, float t) { return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t); }
        public static implicit operator Color(Color32 c) { return new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f); }
        public static implicit operator Vector4(Color c) { return new Vector4(c.r, c.g, c.b, c.a); }
        public static Color HSVToRGB(float h, float s, float v) { return new Color(v, v, v, 1); }
    }
    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        static byte B(float v) { return (byte)Math.Max(0, Math.Min(255, (int)Math.Round(v * 255))); }
        public static implicit operator Color32(Color c) { return new Color32(B(c.r), B(c.g), B(c.b), B(c.a)); }
    }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
        public float xMin { get { return x; } } public float yMin { get { return y; } } public float xMax { get { return x + width; } } public float yMax { get { return y + height; } }
        public Vector2 center { get { return new Vector2(x + width / 2, y + height / 2); } }
        public bool Contains(Vector2 p) { return p.x >= x && p.x < xMax && p.y >= y && p.y < yMax; }
    }
    public struct Bounds
    {
        public Vector3 center, size; public Bounds(Vector3 c, Vector3 s) { center = c; size = s; }
        public Vector3 extents { get { return size * .5f; } }
        public Vector3 min { get { return center - extents; } } public Vector3 max { get { return center + extents; } }
        public void Encapsulate(Vector3 p) { var a = Vector3.Min(min, p); var b = Vector3.Max(max, p); center = (a + b) / 2; size = b - a; }
    }
    public struct Matrix4x4 { }

    public enum ColorSpace { Gamma, Linear }
    public enum FogMode { Linear = 1, Exponential, ExponentialSquared }
    public enum LightType { Spot, Directional, Point, Area }
    public enum LightShadows { None, Hard, Soft }
    public enum LightRenderMode { Auto, ForcePixel, ForceVertex }
    public enum TextureWrapMode { Repeat, Clamp, Mirror, MirrorOnce }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum TextureFormat { Alpha8 = 1, RGB24 = 3, RGBA32 = 4, ARGB32 = 5 }
    public enum MaterialGlobalIlluminationFlags { None = 0, RealtimeEmissive = 1, BakedEmissive = 2, EmissiveIsBlack = 4 }
    public enum FindObjectsSortMode { None, InstanceID }
    public enum HideFlags { None = 0, HideAndDontSave = 61 }
    public enum Space { World, Self }
    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }

    public static class QualitySettings { public static ColorSpace activeColorSpace = ColorSpace.Linear; }
    public static class RenderSettings
    {
        public static bool fog; public static FogMode fogMode; public static float fogDensity; public static Color fogColor, ambientSkyColor, ambientEquatorColor, ambientGroundColor, ambientLight;
        public static Rendering.AmbientMode ambientMode;
    }
    public static class Time { public static float time, deltaTime = 1 / 60f, unscaledDeltaTime = 1 / 60f, realtimeSinceStartup; public static int frameCount; }
    public static class Random
    {
        static System.Random r = new System.Random(1);
        public static float value { get { return (float)r.NextDouble(); } }
        public static float Range(float a, float b) { return a + (float)r.NextDouble() * (b - a); }
        public static int Range(int a, int b) { return b > a ? r.Next(a, b) : a; }
        public static void InitState(int s) { r = new System.Random(s); }
        public static Vector3 insideUnitSphere { get { return new Vector3(value * 2 - 1, value * 2 - 1, value * 2 - 1) * .5f; } }
    }
    public static class Debug { public static void Log(object o) { Console.Error.WriteLine(o); } public static void LogWarning(object o) { Console.Error.WriteLine("aviso: " + o); } public static void LogError(object o) { Console.Error.WriteLine("erro: " + o); } }
    public static class Application { public static string persistentDataPath = "."; public static bool isPlaying = true; public static int targetFrameRate; }
    public static class PlayerPrefs
    {
        static readonly Dictionary<string, object> d = new Dictionary<string, object>();
        public static float GetFloat(string k, float v = 0) { object o; return d.TryGetValue(k, out o) ? (float)o : v; }
        public static int GetInt(string k, int v = 0) { object o; return d.TryGetValue(k, out o) ? (int)o : v; }
        public static string GetString(string k, string v = "") { object o; return d.TryGetValue(k, out o) ? (string)o : v; }
        public static void SetFloat(string k, float v) { d[k] = v; } public static void SetInt(string k, int v) { d[k] = v; } public static void SetString(string k, string v) { d[k] = v; }
        public static bool HasKey(string k) { return d.ContainsKey(k); } public static void Save() { }
    }

    public class Object
    {
        public string name = "";
        public HideFlags hideFlags;
        public static void Destroy(Object o) { var g = o as GameObject; if (g != null) { g.destruido = true; if (g.transform.parent != null) g.transform.parent.filhos.Remove(g.transform); } }
        public static void Destroy(Object o, float t) { Destroy(o); }
        public static void DestroyImmediate(Object o) { Destroy(o); }
        public static void DontDestroyOnLoad(Object o) { }
        public static T Instantiate<T>(T o) where T : Object { return o; }
        public static T FindObjectOfType<T>() where T : Object { return null; }
        public static T[] FindObjectsOfType<T>() where T : Object { return new T[0]; }
        public static T FindAnyObjectByType<T>() where T : Object { return null; }
        public static T[] FindObjectsByType<T>(FindObjectsSortMode m) where T : Object { return new T[0]; }
        public static bool operator ==(Object a, Object b) { bool an = ReferenceEquals(a, null) || a.Morto, bn = ReferenceEquals(b, null) || b.Morto; if (an || bn) return an && bn; return ReferenceEquals(a, b); }
        public static bool operator !=(Object a, Object b) { return !(a == b); }
        public static implicit operator bool(Object o) { return o != null; }
        protected virtual bool Morto { get { return false; } }
        public override bool Equals(object o) { return ReferenceEquals(this, o); }
        public override int GetHashCode() { return base.GetHashCode(); }
    }
    public class Component : Object
    {
        public GameObject gameObject; public Transform transform { get { return gameObject.transform; } }
        public T GetComponent<T>() where T : Component { return gameObject.GetComponent<T>(); }
        public T[] GetComponentsInChildren<T>(bool inativos = false) where T : Component { return gameObject.GetComponentsInChildren<T>(inativos); }
        public T AddComponent<T>() where T : Component, new() { return gameObject.AddComponent<T>(); }
        public string tag = "";
    }
    public class Behaviour : Component { public bool enabled = true; }
    public class MonoBehaviour : Behaviour
    {
        public void StartCoroutine(System.Collections.IEnumerator e) { }
        public void Invoke(string m, float t) { }
    }
    public class GameObject : Object
    {
        public Transform transform; public bool isStatic; public int layer; internal bool ativo = true, destruido;
        readonly List<Component> comps = new List<Component>();
        public GameObject(string nome = "GameObject") { name = nome; transform = new Transform(); transform.gameObject = this; comps.Add(transform); }
        public GameObject(string nome, params Type[] tipos) : this(nome) { }
        public T AddComponent<T>() where T : Component, new() { var c = new T(); c.gameObject = this; comps.Add(c); return c; }
        public T GetComponent<T>() where T : Component { foreach (var c in comps) if (c is T) return (T)c; return null; }
        public T[] GetComponentsInChildren<T>(bool inativos = false) where T : Component
        {
            var l = new List<T>(); Action<Transform> v = null; v = t => { if (!inativos && !t.gameObject.ativo) return; foreach (var c in t.gameObject.comps) if (c is T) l.Add((T)c); foreach (var f in t.filhos) v(f); }; v(transform); return l.ToArray();
        }
        public void SetActive(bool v) { ativo = v; }
        public bool activeSelf { get { return ativo; } }
        public bool activeInHierarchy { get { for (var t = transform; t != null; t = t.parent) if (!t.gameObject.ativo) return false; return true; } }
        protected override bool Morto { get { return destruido; } }
        public static GameObject CreatePrimitive(PrimitiveType t) { var g = new GameObject(t.ToString()); g.AddComponent<MeshFilter>(); g.AddComponent<MeshRenderer>(); return g; }
        public static GameObject Find(string n) { return null; }
        public string tag = "";
    }
    public class Transform : Component
    {
        public Vector3 localPosition = Vector3.zero, localScale = Vector3.one; public Quaternion localRotation = Quaternion.identity;
        internal Transform pai; internal readonly List<Transform> filhos = new List<Transform>();
        public Transform parent { get { return pai; } set { SetParent(value, true); } }
        public void SetParent(Transform p, bool manter = true) { if (pai != null) pai.filhos.Remove(this); pai = p; if (p != null) p.filhos.Add(this); }
        public int childCount { get { return filhos.Count; } }
        public Transform GetChild(int i) { return filhos[i]; }
        public List<Transform> Filhos { get { return filhos; } }
        public Transform root { get { var t = this; while (t.pai != null) t = t.pai; return t; } }
        public Transform Find(string n) { foreach (var f in filhos) if (f.gameObject.name == n) return f; return null; }
        // ponto local -> mundo (escala, giro, posição, subindo pelos pais)
        public Vector3 TransformPoint(Vector3 p) { var t = this; while (t != null) { p = t.localRotation * Vector3.Scale(p, t.localScale) + t.localPosition; t = t.pai; } return p; }
        public Vector3 TransformDirection(Vector3 d) { var t = this; while (t != null) { d = t.localRotation * d; t = t.pai; } return d; }
        public Vector3 TransformVector(Vector3 d) { var t = this; while (t != null) { d = t.localRotation * Vector3.Scale(d, t.localScale); t = t.pai; } return d; }
        public Vector3 InverseTransformPoint(Vector3 p) { if (pai != null) p = pai.InverseTransformPoint(p); p = Quaternion.Inverse(localRotation) * (p - localPosition); return new Vector3(p.x / localScale.x, p.y / localScale.y, p.z / localScale.z); }
        public Vector3 InverseTransformVector(Vector3 d) { if (pai != null) d = pai.InverseTransformVector(d); d = Quaternion.Inverse(localRotation) * d; return new Vector3(d.x / localScale.x, d.y / localScale.y, d.z / localScale.z); }
        public bool IsChildOf(Transform p) { for (var t = this; t != null; t = t.pai) if (t == p) return true; return false; }
        public Vector3 position { get { return pai == null ? localPosition : pai.TransformPoint(localPosition); } set { localPosition = value; } }
        public Quaternion rotation { get { var q = localRotation; for (var t = pai; t != null; t = t.pai) q = t.localRotation * q; return q; } set { localRotation = value; } }
        public Vector3 eulerAngles { get { return Vector3.zero; } set { localRotation = Quaternion.Euler(value); } }
        public Vector3 localEulerAngles { get { return Vector3.zero; } set { localRotation = Quaternion.Euler(value); } }
        public Vector3 forward { get { return rotation * Vector3.forward; } set { } }
        public Vector3 up { get { return rotation * Vector3.up; } }
        public Vector3 right { get { return rotation * Vector3.right; } }
        public Vector3 lossyScale { get { var s = localScale; for (var t = pai; t != null; t = t.pai) s = Vector3.Scale(s, t.localScale); return s; } }
        public void LookAt(Vector3 alvo) { localRotation = Quaternion.LookRotation(alvo - position); }
        public void Rotate(Vector3 e) { localRotation = localRotation * Quaternion.Euler(e); }
        public void SetPositionAndRotation(Vector3 p, Quaternion q) { localPosition = p; localRotation = q; }
        public void SetAsLastSibling() { }
    }
    public class Mesh : Object
    {
        public Rendering.IndexFormat indexFormat; public Bounds bounds;
        public Vector3[] vertices = new Vector3[0], normals = new Vector3[0]; public Vector2[] uv = new Vector2[0], uv2 = new Vector2[0]; public Color[] colors = new Color[0]; public int[] triangles = new int[0];
        public void SetVertices(List<Vector3> v) { vertices = v.ToArray(); }
        public void SetNormals(List<Vector3> v) { normals = v.ToArray(); }
        public void SetUVs(int c, List<Vector2> v) { if (c == 0) uv = v.ToArray(); else uv2 = v.ToArray(); }
        public void SetColors(List<Color> v) { colors = v.ToArray(); }
        public void SetTriangles(List<int> t, int s) { triangles = t.ToArray(); }
        public void SetTriangles(int[] t, int s) { triangles = t; }
        public void RecalculateBounds() { }
        public void Clear() { vertices = new Vector3[0]; normals = new Vector3[0]; uv = new Vector2[0]; uv2 = new Vector2[0]; colors = new Color[0]; triangles = new int[0]; }
        public void RecalculateNormals() { }
        public void MarkDynamic() { }
        public int vertexCount { get { return vertices.Length; } }
    }
    public class MeshFilter : Component { public Mesh sharedMesh; public Mesh mesh { get { return sharedMesh; } set { sharedMesh = value; } } }
    public class Renderer : Component
    {
        public bool enabled = true, receiveShadows = true; public Rendering.ShadowCastingMode shadowCastingMode = Rendering.ShadowCastingMode.On;
        public Material sharedMaterial; public Material material { get { return sharedMaterial; } set { sharedMaterial = value; } }
        public Material[] sharedMaterials { get { return new[] { sharedMaterial }; } set { sharedMaterial = value.Length > 0 ? value[0] : null; } }
        public Material[] materials { get { return sharedMaterials; } set { sharedMaterials = value; } }
        public int sortingOrder; public Bounds bounds;
        public void SetPropertyBlock(MaterialPropertyBlock b) { }
    }
    public class MeshRenderer : Renderer { }
    public class LineRenderer : Renderer
    {
        public bool useWorldSpace = true; public int positionCount; public float startWidth, endWidth; public Color startColor, endColor; readonly Dictionary<int, Vector3> pts = new Dictionary<int, Vector3>();
        public void SetPosition(int i, Vector3 p) { pts[i] = p; } public Vector3 GetPosition(int i) { Vector3 p; return pts.TryGetValue(i, out p) ? p : Vector3.zero; }
    }
    public class WaitForSeconds { public WaitForSeconds(float s) { } }
    public class MaterialPropertyBlock { public void SetColor(string n, Color c) { } public void SetFloat(string n, float f) { } }
    public class Shader : Object
    {
        public bool isSupported = true;
        public static readonly Dictionary<string, Shader> conhecidos = new Dictionary<string, Shader>();
        public static Shader Find(string n) { Shader s; if (!conhecidos.TryGetValue(n, out s)) { s = new Shader { name = n }; conhecidos[n] = s; } return s; }
        public static void EnableKeyword(string k) { } public static void DisableKeyword(string k) { }
        public static void SetGlobalFloat(string n, float f) { } public static void SetGlobalColor(string n, Color c) { } public static void SetGlobalVector(string n, Vector4 v) { }
    }
    public class Texture : Object { public TextureWrapMode wrapMode; public FilterMode filterMode; public int anisoLevel, width, height; public float mipMapBias; }
    public class Texture2D : Texture
    {
        public Color32[] pixels; public bool linear; static int seq;
        public readonly int id = ++seq;
        public Texture2D(int w, int h, TextureFormat f = TextureFormat.RGBA32, bool mip = true, bool linear = false) { width = w; height = h; this.linear = linear; pixels = new Color32[w * h]; }
        public void SetPixels32(Color32[] p) { pixels = (Color32[])p.Clone(); }
        public void SetPixels(Color[] p) { pixels = new Color32[p.Length]; for (int i = 0; i < p.Length; i++) pixels[i] = p[i]; }
        public Color32[] GetPixels32() { return pixels; }
        public void SetPixel(int x, int y, Color c) { pixels[y * width + x] = c; }
        public void Apply(bool mip = true, bool naoLer = false) { }
        public static Texture2D whiteTexture { get { var t = new Texture2D(1, 1); t.pixels[0] = new Color32(255, 255, 255, 255); return t; } }
    }
    public class Material : Object
    {
        public Shader shader; public int renderQueue = -1; public bool enableInstancing; public MaterialGlobalIlluminationFlags globalIlluminationFlags;
        public readonly Dictionary<string, Color> cores = new Dictionary<string, Color>(); public readonly Dictionary<string, float> numeros = new Dictionary<string, float>();
        public readonly Dictionary<string, Texture> texturas = new Dictionary<string, Texture>(); public readonly Dictionary<string, Vector2> escalas = new Dictionary<string, Vector2>(), deslocs = new Dictionary<string, Vector2>();
        public readonly HashSet<string> chaves = new HashSet<string>(); public readonly Dictionary<string, string> etiquetas = new Dictionary<string, string>();
        public Material(Shader s) { shader = s; }
        public Material(Material m)
        {
            shader = m.shader; renderQueue = m.renderQueue; name = m.name;
            foreach (var kv in m.cores) cores[kv.Key] = kv.Value; foreach (var kv in m.numeros) numeros[kv.Key] = kv.Value; foreach (var kv in m.texturas) texturas[kv.Key] = kv.Value;
            foreach (var kv in m.escalas) escalas[kv.Key] = kv.Value; foreach (var kv in m.deslocs) deslocs[kv.Key] = kv.Value; foreach (var k in m.chaves) chaves.Add(k);
        }
        public bool HasProperty(string n) { return true; }
        public void SetColor(string n, Color c) { cores[n] = c; } public Color GetColor(string n) { Color c; return cores.TryGetValue(n, out c) ? c : Color.white; }
        public void SetFloat(string n, float f) { numeros[n] = f; } public float GetFloat(string n) { float f; return numeros.TryGetValue(n, out f) ? f : 0; }
        public void SetInt(string n, int f) { numeros[n] = f; }
        public void SetVector(string n, Vector4 v) { cores[n] = new Color(v.x, v.y, v.z, v.w); }
        public void SetTexture(string n, Texture t) { texturas[n] = t; } public Texture GetTexture(string n) { Texture t; return texturas.TryGetValue(n, out t) ? t : null; }
        public void SetTextureScale(string n, Vector2 v) { escalas[n] = v; } public Vector2 GetTextureScale(string n) { Vector2 v; return escalas.TryGetValue(n, out v) ? v : Vector2.one; }
        public void SetTextureOffset(string n, Vector2 v) { deslocs[n] = v; } public Vector2 GetTextureOffset(string n) { Vector2 v; return deslocs.TryGetValue(n, out v) ? v : Vector2.zero; }
        public void EnableKeyword(string k) { chaves.Add(k); } public void DisableKeyword(string k) { chaves.Remove(k); }
        public void SetOverrideTag(string k, string v) { etiquetas[k] = v; }
        public Color color { get { return GetColor("_Color"); } set { SetColor("_Color", value); SetColor("_BaseColor", value); } }
        public Texture mainTexture { get { return GetTexture("_MainTex"); } set { SetTexture("_MainTex", value); } }
    }
    public class Light : Behaviour
    {
        public LightType type; public Color color = Color.white; public float range = 10, intensity = 1, spotAngle = 30, innerSpotAngle, shadowBias, shadowNormalBias, shadowStrength = 1; public LightShadows shadows; public LightRenderMode renderMode; public int cullingMask = -1;
    }
    public class TextAsset : Object { public string text; public byte[] bytes; public TextAsset(string t) { text = t; } public TextAsset(byte[] b) { bytes = b; } }
    public static class ImageConversion { public static bool LoadImage(Texture2D t, byte[] b) { return false; } }
    public static class ColorUtility
    {
        public static bool TryParseHtmlString(string h, out Color c)
        {
            c = new Color(1, 1, 1, 1); if (string.IsNullOrEmpty(h) || h[0] != '#') return false; h = h.Substring(1);
            if (h.Length == 3 || h.Length == 4) { var t = ""; foreach (var ch in h) t += new string(ch, 2); h = t; }
            if (h.Length != 6 && h.Length != 8) return false;
            System.Func<int, float> B = i => System.Convert.ToInt32(h.Substring(i, 2), 16) / 255f;
            c = new Color(B(0), B(2), B(4), h.Length == 8 ? B(6) : 1); return true;
        }
        public static string ToHtmlStringRGB(Color c) { System.Func<float, string> B = v => ((int)System.Math.Round(Mathf.Clamp01(v) * 255)).ToString("X2"); return B(c.r) + B(c.g) + B(c.b); }
    }
    public static class Resources
    {
        public static string pasta = "";
        public static T Load<T>(string n) where T : Object
        {
            if (typeof(T) == typeof(TextAsset))
            {
                var p = System.IO.Path.Combine(pasta, n + ".json"); if (System.IO.File.Exists(p)) return new TextAsset(System.IO.File.ReadAllText(p)) as T;
                p = System.IO.Path.Combine(pasta, n + ".bytes"); if (System.IO.File.Exists(p)) return new TextAsset(System.IO.File.ReadAllBytes(p)) as T;
            }
            return null;
        }
    }
    public class Camera : Behaviour { public float fieldOfView = 60, nearClipPlane = .1f, farClipPlane = 1000; public static Camera main; }
    public class AudioListener : Behaviour { }
}
namespace UnityEngine.Rendering
{
    public enum ShadowCastingMode { Off, On, TwoSided, ShadowsOnly }
    public enum BlendMode { Zero = 0, One = 1, DstColor = 2, SrcColor = 3, OneMinusDstColor = 4, SrcAlpha = 5, OneMinusSrcColor = 6, DstAlpha = 7, OneMinusDstAlpha = 8, SrcAlphaSaturate = 9, OneMinusSrcAlpha = 10 }
    public enum IndexFormat { UInt16, UInt32 }
    public enum AmbientMode { Skybox = 0, Trilight = 1, Flat = 3, Custom = 4 }
    public enum CullMode { Off, Front, Back }
}

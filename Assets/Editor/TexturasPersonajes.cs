using System.IO;
using UnityEditor;
using UnityEngine;

public static class TexturasPersonajes
{
    const string TexFolder = "Assets/Textures/Personajes";
    const string MatFolder = "Assets/Materials/Personajes";

    static readonly Color Piel = new Color(0.98f, 0.8f, 0.66f);
    static readonly Color Tinta = new Color(0.12f, 0.08f, 0.08f);

    [MenuItem("Tools/Personajes/Generar texturas")]
    public static void Generar()
    {
        Directory.CreateDirectory(TexFolder);
        Directory.CreateDirectory(MatFolder);

        var piel = CrearMaterial("Piel", Manos(1));

        var mom = new Set
        {
            Cabeza = CrearMaterial("Mom_Cabeza", Cabeza(2, new Color(0.45f, 0.25f, 0.12f), 0.68f, 0.2f, 0f, Estilo.Mom)),
            Ropa = CrearMaterial("Mom_Ropa", Vestido(3)),
            Piel = piel,
        };
        var kid = new Set
        {
            Cabeza = CrearMaterial("Kid_Cabeza", Cabeza(4, new Color(0.95f, 0.5f, 0.15f), 0.73f, 0.58f, 0.04f, Estilo.Kid)),
            Ropa = CrearMaterial("Kid_Ropa", Rayas(5)),
            Piel = piel,
        };

        Asignar("Assets/Prefabs/Mom.prefab", mom);
        Asignar("Assets/Prefabs/Kid.prefab", kid);
        AssetDatabase.SaveAssets();
    }

    public static Set MaterialesJugador()
    {
        Directory.CreateDirectory(TexFolder);
        Directory.CreateDirectory(MatFolder);
        return new Set
        {
            Cabeza = CrearMaterial("Player_Cabeza", Cabeza(6, new Color(0.15f, 0.12f, 0.1f), 0.75f, 0.5f, 0.02f, Estilo.Player)),
            Ropa = CrearMaterial("Player_Ropa", Sudadera(7)),
            Piel = CrearMaterial("Piel", Manos(1)),
        };
    }

    public class Set
    {
        public Material Cabeza, Ropa, Piel;
    }

    static void Asignar(string prefabPath, Set set)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        Asignar(root.transform, set);
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    public static void Asignar(Transform root, Set set)
    {
        foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            Material m = r.name switch
            {
                "Head" => set.Cabeza,
                "Body" or "Shoulder1" or "Shoulder2" => set.Ropa,
                "Hand1" or "Hand2" => set.Piel,
                _ => null,
            };
            if (m != null) r.sharedMaterial = m;
        }
    }

    enum Estilo { Mom, Kid, Player }

    static Texture2D Cabeza(int seed, Color pelo, float hairFront, float hairBack, float spikes, Estilo estilo)
    {
        var c = new Lienzo(512, 256, seed, wrapX: true);
        c.Fill(Piel, 0.18f);

        for (int y = 0; y < c.H; y++)
        for (int x = 0; x < c.W; x++)
        {
            float u = (float)x / c.W, v = (float)y / c.H;
            float du = Mathf.Abs(Mathf.Repeat(u - 0.25f + 0.5f, 1f) - 0.5f);
            float line = Mathf.Lerp(hairBack, hairFront, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.22f, 0.42f, du)));
            line += (Mathf.PerlinNoise(u * 18f + seed, 3f) - 0.5f) * 0.06f + Mathf.Sin(u * Mathf.PI * 2f * 24f) * spikes;
            if (v > line) c.Blend(x, y, pelo * (0.85f + Mathf.PerlinNoise(x * 0.08f, y * 0.3f) * 0.3f), 1f);
        }
        for (int i = 0; i < 40; i++)
        {
            float x = c.R(0, c.W), y = c.R(hairFront * c.H + 8, c.H);
            c.Line(x, y, x + c.R(-14, 14), y - c.R(6, 20), 1.5f, pelo * 0.6f);
        }

        float fx = 0.75f * c.W, ey = 0.56f * c.H;
        for (int s = -1; s <= 1; s += 2)
        {
            float ex = fx + s * 22f + c.R(-2, 2), eyy = ey + c.R(-3, 3);
            float er = estilo == Estilo.Kid ? 12f : 9f;
            c.Disc(ex, eyy, er + 2f, Tinta, 0.15f);
            c.Disc(ex, eyy, er, Color.white, 0.15f);
            c.Disc(ex + c.R(-3, 3), eyy + c.R(-3, 2), er * 0.5f, Tinta, 0.1f);
            if (estilo == Estilo.Mom)
                for (int l = -1; l <= 1; l++)
                    c.Line(ex + l * 5f, eyy + er, ex + l * 8f, eyy + er + 7f, 1.5f, Tinta);
            c.Line(ex - 10f, eyy + er + (estilo == Estilo.Player ? 6f : 10f), ex + 10f, eyy + er + (estilo == Estilo.Player ? 10f - s * 4f : 12f), 2.5f, pelo * 0.7f);
        }

        c.Disc(fx - 36f, ey - 22f, 10f, new Color(1f, 0.5f, 0.55f), 0.3f, 0.45f);
        c.Disc(fx + 36f, ey - 22f, 10f, new Color(1f, 0.5f, 0.55f), 0.3f, 0.45f);

        float my = 0.41f * c.H;
        switch (estilo)
        {
            case Estilo.Mom:
                c.Curve(fx - 14f, fx + 14f, my, -5f, 4.5f, new Color(0.85f, 0.1f, 0.2f));
                break;
            case Estilo.Kid:
                c.Curve(fx - 18f, fx + 18f, my + 2f, -10f, 6f, Tinta);
                c.Curve(fx - 12f, fx + 12f, my + 2f, -6f, 2f, Color.white);
                for (int i = 0; i < 6; i++) c.Disc(fx + c.R(-30, 30), ey - c.R(10, 26), 1.8f, new Color(0.7f, 0.4f, 0.25f), 0.2f);
                break;
            default:
                c.Curve(fx - 12f, fx + 16f, my, -3f, 2.5f, Tinta);
                break;
        }
        return c.Save("Cabeza_" + estilo);
    }

    static Texture2D Vestido(int seed)
    {
        var c = new Lienzo(256, 256, seed);
        c.Fill(new Color(0.93f, 0.45f, 0.62f), 0.25f);
        for (int i = 0; i < 9; i++)
        {
            float x = c.R(20, 236), y = c.R(20, 236);
            for (int p = 0; p < 5; p++)
            {
                float a = p * Mathf.PI * 2f / 5f + c.R(-0.3f, 0.3f);
                c.Disc(x + Mathf.Cos(a) * 9f, y + Mathf.Sin(a) * 9f, 7f, new Color(1f, 0.97f, 0.9f), 0.3f);
            }
            c.Disc(x, y, 5f, new Color(1f, 0.82f, 0.2f), 0.3f);
        }
        c.Border(7f, new Color(0.6f, 0.2f, 0.35f));
        return c.Save("Ropa_Mom");
    }

    static Texture2D Rayas(int seed)
    {
        var c = new Lienzo(256, 256, seed);
        var a = new Color(0.35f, 0.75f, 0.35f);
        var b = new Color(1f, 0.85f, 0.3f);
        for (int y = 0; y < c.H; y++)
        for (int x = 0; x < c.W; x++)
        {
            float wob = (Mathf.PerlinNoise(x * 0.03f, y * 0.01f + seed) - 0.5f) * 14f;
            bool band = Mathf.FloorToInt((y + wob) / 36f) % 2 == 0;
            c.Blend(x, y, (band ? a : b) * (0.9f + Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.2f), 1f);
        }
        c.Border(7f, new Color(0.2f, 0.4f, 0.2f));
        return c.Save("Ropa_Kid");
    }

    static Texture2D Sudadera(int seed)
    {
        var c = new Lienzo(256, 256, seed);
        c.Fill(new Color(0.25f, 0.45f, 0.85f), 0.25f);
        var linea = new Color(0.1f, 0.18f, 0.4f);
        c.Line(60, 40, 196, 46, 3f, linea);
        c.Line(60, 40, 72, 100, 3f, linea);
        c.Line(196, 46, 186, 104, 3f, linea);
        c.Line(72, 100, 186, 104, 3f, linea);
        c.Line(128, 104, 126, 256, 3f, new Color(0.75f, 0.75f, 0.8f));
        for (int y = 112; y < 256; y += 10) c.Line(122, y, 132, y + 2, 1.5f, new Color(0.75f, 0.75f, 0.8f));
        c.Border(7f, linea);
        return c.Save("Ropa_Player");
    }

    static Texture2D Manos(int seed)
    {
        var c = new Lienzo(128, 128, seed);
        c.Fill(Piel, 0.2f);
        for (int i = 1; i < 4; i++) c.Line(i * 32 + c.R(-3, 3), 0, i * 32 + c.R(-3, 3), 70, 1.5f, Piel * 0.75f);
        c.Border(5f, Piel * 0.7f);
        return c.Save("Piel");
    }

    static Material CrearMaterial(string name, Texture2D tex)
    {
        string path = $"{MatFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Smoothness", 0.1f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    class Lienzo
    {
        public readonly int W, H;
        readonly Color[] px;
        readonly System.Random rnd;
        readonly bool wrapX;

        public Lienzo(int w, int h, int seed, bool wrapX = false)
        {
            W = w;
            H = h;
            px = new Color[w * h];
            rnd = new System.Random(seed);
            this.wrapX = wrapX;
        }

        public float R(float min, float max) => min + (float)rnd.NextDouble() * (max - min);

        public void Fill(Color c, float patch)
        {
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.04f + 7f, y * 0.04f + 3f) - 0.5f;
                px[y * W + x] = c * (1f + n * patch + R(-0.03f, 0.03f));
                px[y * W + x].a = 1f;
            }
        }

        public void Blend(int x, int y, Color c, float alpha)
        {
            if (wrapX) x = (x % W + W) % W;
            if (x < 0 || x >= W || y < 0 || y >= H) return;
            int i = y * W + x;
            px[i] = Color.Lerp(px[i], c, alpha);
            px[i].a = 1f;
        }

        public void Disc(float cx, float cy, float r, Color c, float wobble, float alpha = 1f)
        {
            float phase = R(0, 6.28f);
            int k = rnd.Next(3, 6);
            int rr = Mathf.CeilToInt(r * (1f + wobble)) + 1;
            for (int y = -rr; y <= rr; y++)
            for (int x = -rr; x <= rr; x++)
            {
                float a = Mathf.Atan2(y, x);
                float edge = r * (1f + wobble * Mathf.Sin(a * k + phase));
                if (x * x + y * y <= edge * edge) Blend(Mathf.RoundToInt(cx + x), Mathf.RoundToInt(cy + y), c, alpha);
            }
        }

        public void Line(float x0, float y0, float x1, float y1, float w, Color c)
        {
            float len = Mathf.Max(1f, Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1)));
            Vector2 n = new Vector2(-(y1 - y0), x1 - x0) / len;
            float seed = R(0, 100);
            for (float t = 0; t <= 1f; t += 0.5f / len)
            {
                float j = (Mathf.PerlinNoise(t * len * 0.08f, seed) - 0.5f) * 3f;
                float ww = w * (0.8f + Mathf.PerlinNoise(t * len * 0.1f, seed + 5f) * 0.5f);
                Dot(Mathf.Lerp(x0, x1, t) + n.x * j, Mathf.Lerp(y0, y1, t) + n.y * j, ww, c);
            }
        }

        public void Curve(float x0, float x1, float y, float bend, float w, Color c)
        {
            const int steps = 12;
            for (int i = 0; i < steps; i++)
            {
                float a = (float)i / steps, b = (float)(i + 1) / steps;
                Line(Mathf.Lerp(x0, x1, a), y + bend * Mathf.Sin(a * Mathf.PI), Mathf.Lerp(x0, x1, b), y + bend * Mathf.Sin(b * Mathf.PI), w, c);
            }
        }

        void Dot(float cx, float cy, float r, Color c)
        {
            int rr = Mathf.CeilToInt(r);
            for (int y = -rr; y <= rr; y++)
            for (int x = -rr; x <= rr; x++)
                if (x * x + y * y <= r * r) Blend(Mathf.RoundToInt(cx + x), Mathf.RoundToInt(cy + y), c, 1f);
        }

        public void Border(float w, Color c)
        {
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float d = Mathf.Min(Mathf.Min(x, W - 1 - x), Mathf.Min(y, H - 1 - y));
                float edge = w * (0.6f + Mathf.PerlinNoise(x * 0.07f + 11f, y * 0.07f + 5f) * 0.9f);
                if (d < edge) Blend(x, y, c, 0.85f);
            }
        }

        public Texture2D Save(string name)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            string path = $"{TexFolder}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapModeU = wrapX ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}

using System;
using UnityEngine;

namespace ARdventure
{
    // Small shared renderer for the new screens; all art remains generated placeholder art.
    public sealed class AdventureArt : IDisposable
    {
        private readonly Transform root;
        private readonly Sprite square, circle;
        private readonly Material material;
        public Camera Camera { get; }

        public AdventureArt(Transform parent, Color background)
        {
            root = parent;
            square = MakeSprite(false); circle = MakeSprite(true);
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            material = new Material(shader);
            var cameraObject = new GameObject("Adventure camera");
            cameraObject.transform.SetParent(root, false);
            cameraObject.transform.position = new Vector3(0, 0, -10);
            Camera = cameraObject.AddComponent<Camera>();
            Camera.orthographic = true;
            Camera.backgroundColor = background;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();
            FitCamera();
        }
        public void FitCamera()
        {
            Camera.rect = new Rect(0, 0.235f, 1, 0.61f);
            float aspect = Screen.width / Mathf.Max(1, Screen.height * 0.61f);
            Camera.orthographicSize = Mathf.Max(6.7f, 12f / aspect);
        }
        public SpriteRenderer Shape(string name, Vector2 position, Vector2 scale, Color colour, int order = 1, bool round = false)
        {
            var item = new GameObject(name);
            item.transform.SetParent(root, false);
            item.transform.localPosition = position;
            item.transform.localScale = new Vector3(scale.x, scale.y, 1);
            var renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = round ? circle : square;
            renderer.sharedMaterial = material;
            renderer.color = colour;
            renderer.sortingOrder = order;
            return renderer;
        }
        public Transform Explorer(Vector2 position)
        {
            var hero = new GameObject("Explorer").transform;
            hero.SetParent(root, false);
            hero.position = position;
            var shadow = Shape("Explorer shadow", Vector2.zero, new Vector2(0.85f, 0.25f), Hex("182630"), 20, true);
            shadow.transform.SetParent(hero, false); shadow.transform.localPosition = new Vector2(0, -0.3f);
            var coat = Shape("Explorer coat", Vector2.zero, new Vector2(0.65f, 0.75f), Hex("5ABDC4"), 21, true);
            coat.transform.SetParent(hero, false);
            var face = Shape("Explorer face", Vector2.zero, Vector2.one * 0.55f, Hex("E9BE83"), 22, true);
            face.transform.SetParent(hero, false); face.transform.localPosition = new Vector2(0, 0.5f);
            var hat = Shape("Explorer hat", Vector2.zero, new Vector2(0.8f, 0.15f), Hex("E5CF94"), 23);
            hat.transform.SetParent(hero, false); hat.transform.localPosition = new Vector2(0, 0.75f);
            return hero;
        }
        public void Dispose()
        {
            UnityEngine.Object.Destroy(square.texture); UnityEngine.Object.Destroy(square);
            UnityEngine.Object.Destroy(circle.texture); UnityEngine.Object.Destroy(circle);
            UnityEngine.Object.Destroy(material);
        }
        private static Sprite MakeSprite(bool round)
        {
            int size = round ? 64 : 1;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                pixels[y * size + x] = new Color(1, 1, 1, !round ? 1 : Mathf.Clamp01(size * 0.5f - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), Vector2.one * size * 0.5f)));
            texture.SetPixels(pixels); texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }
        public static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var colour); return colour; }
    }

    public sealed class AdventureGui
    {
        public readonly GUIStyle Title, Heading, Body, Small, Button, Tag;
        public AdventureGui()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Body = new GUIStyle { font = font, fontSize = 19, wordWrap = true, normal = { textColor = AdventureArt.Hex("E2E9DE") } };
            Small = new GUIStyle(Body) { fontSize = 15, normal = { textColor = AdventureArt.Hex("ACC4C2") } };
            Heading = new GUIStyle(Body) { fontSize = 28, fontStyle = FontStyle.Bold };
            Title = new GUIStyle(Heading) { fontSize = 46 };
            Button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 18, fontStyle = FontStyle.Bold };
            Tag = new GUIStyle(Small) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        }
        public static void Panel(Rect rect, Color colour)
        {
            var previous = GUI.color;
            GUI.color = colour; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous;
        }
        public static Matrix4x4 Begin()
        {
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 800f, 1));
            return previous;
        }
    }
}

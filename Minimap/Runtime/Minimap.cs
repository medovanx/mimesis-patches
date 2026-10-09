// MIMESIS Minimap - bottom-left floor plan with your position
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// The map is drawn from the level's NavMesh (the walkable floor the game builds on every client at level
// load), so it never contains players, mimics or monsters: only the layout and your own arrow.
// Floors are separated by height, so only the floor you're on is shown.
// Settings (click the Minimap chip on the main menu to open the settings window; saved between sessions):
//   Reveal: Explored - the map appears as you walk near it (default) | Full - the whole map from the start
//   Style:  Plain    - floor plan drawn from the NavMesh (default)   | Graphic - real top-down view
//   Show:   Players / Monsters / Items - each off by default. Enabled categories appear as coloured dots
//           (and as models in Graphic); disabled ones are hidden from the minimap camera while it renders.
//           Mimics count as monsters. M toggles the minimap in game.

using System;
using System.Collections.Generic;
using Pathfinding;
using Mimic.Actors;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Minimap
{
    sealed class Minimap : MonoBehaviour
    {
        public enum Reveal { Explored, Full }
        public enum Style { Plain, Graphic }
        const string RevealKey = "medovanx.Minimap.Mode", StyleKey = "medovanx.Minimap.Style";

        public static Reveal CurrentReveal
        {
            get => (Reveal)PlayerPrefs.GetInt(RevealKey, (int)Reveal.Explored);
            set { PlayerPrefs.SetInt(RevealKey, (int)value); PlayerPrefs.Save(); }
        }

        public static Style CurrentStyle
        {
            get => (Style)PlayerPrefs.GetInt(StyleKey, (int)Style.Plain);
            set { PlayerPrefs.SetInt(StyleKey, (int)value); PlayerPrefs.Save(); }
        }

        public static bool ShowPlayers { get => GetBool("Players"); set => SetBool("Players", value); }
        public static bool ShowMonsters { get => GetBool("Monsters"); set => SetBool("Monsters", value); }
        public static bool ShowItems { get => GetBool("Items"); set => SetBool("Items", value); }
        static bool GetBool(string key) => PlayerPrefs.GetInt("medovanx.Minimap.Show" + key, 0) == 1;
        static void SetBool(string key, bool on) { PlayerPrefs.SetInt("medovanx.Minimap.Show" + key, on ? 1 : 0); PlayerPrefs.Save(); }

        /// <summary>Minimap shown in game (Patches window); M toggles it for the current session too.</summary>
        public static bool Visible
        {
            get => PlayerPrefs.GetInt("medovanx.Minimap.Visible", 1) == 1;
            set { PlayerPrefs.SetInt("medovanx.Minimap.Visible", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static string Summary => (CurrentReveal == Reveal.Full ? "Full" : "Explored") + " · " + CurrentStyle;

        const int MaxTexSize = 512;          // map texture resolution (longest side)
        const float ViewMeters = 50f;        // width of the area shown around you
        const float RevealRadius = 7f;       // Explored mode: how far around you the floor gets revealed
        const float FloorTolerance = 3f;     // vertical distance that still counts as "your floor"
        const float SameFloor = 2f;          // heights closer than this share one slot
        const float ScreenSize = 230f;

        static readonly Color32 Empty = new Color32(0, 0, 0, 0);
        static readonly Color32 Floor = new Color32(200, 200, 190, 220);
        static readonly Color32 OtherFloor = new Color32(200, 200, 190, 45);
        static readonly Color PlayerDot = new Color(0.3f, 0.65f, 1f, 1f);
        static readonly Color MonsterDot = new Color(1f, 0.25f, 0.2f, 1f);
        static readonly Color ItemDot = new Color(1f, 0.85f, 0.2f, 1f);
        const float DotSize = 12f;
        const float LegendHeight = 26f;
        static readonly Color32 Unexplored = new Color32(0, 0, 0, 235);   // graphic + explored: darkens unvisited areas

        const int GraphicSize = 400;           // render texture resolution for the graphic view (covers GraphicMargin x the visible area)
        const float GraphicMargin = 1.6f;      // rendered area vs visible area, so the image can slide smoothly between renders
        const float GraphicInterval = 0.15f;   // seconds between graphic renders (FPS cost)
        const float CameraAboveFeet = 2.2f;    // below typical ceilings, so indoor rooms stay visible

        // Up to two floor heights per pixel (NaN = none), plus what has been explored on each.
        float[] _h0, _h1;
        bool[] _seen0, _seen1;
        int _w, _h;
        float _metersPerPixel;
        Vector2 _origin;
        Texture2D _tex;
        Color32[] _pixels;
        object _builtFor;
        float _nextBuildLog;

        Canvas _canvas;
        RawImage _map, _graphic;
        Camera _cam;
        RenderTexture _rt;
        float _nextRender, _nextActorScan;
        Vector3 _renderedAt;
        // Cached once a second: what's in the scene, by minimap category.
        readonly List<(Transform t, Color color, bool shown)> _things = new List<(Transform, Color, bool)>();
        readonly List<Renderer> _hiddenRenderers = new List<Renderer>();
        readonly List<RawImage> _dots = new List<RawImage>();
        RectTransform _frame, _legend;
        string _legendKey;
        Texture2D _dotTex;
        RectTransform _arrow;
        bool _visible = true;
        float _nextDraw;

        public static void Create()
        {
            var go = new GameObject("MedovanxMinimap");
            DontDestroyOnLoad(go);
            go.AddComponent<Minimap>();
        }

        void Awake() => BuildUi();

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.mKey.wasPressedThisFrame) _visible = !_visible;

            // Any scene with a walkable floor: tram, lobby and levels.
            var scene = Hub.s != null && Hub.s.pdata != null ? Hub.s.pdata.main : null;
            var me = scene != null ? scene.GetMyAvatar() : null;
            bool show = Visible && _visible && me != null && !me.dead && !(Hub.s.uiman != null && Hub.s.uiman.isGameMenuOpen);
            if (scene == null) _builtFor = null;
            if (show && _builtFor != scene && !BuildMap(scene)) show = false;
            Diagnose(scene, me, show);
            _canvas.enabled = show;
            if (!show) return;

            var pos = me.transform.position;
            bool graphic = CurrentStyle == Style.Graphic;
            if (CurrentReveal == Reveal.Explored) RevealAround(pos);
            if (Time.unscaledTime >= _nextDraw)
            {
                _nextDraw = Time.unscaledTime + 0.2f;
                Draw(pos.y, graphic);
            }
            if (Time.unscaledTime >= _nextActorScan) Scan(me);
            Legend();
            Dots(pos);
            _graphic.enabled = graphic;
            _map.enabled = !graphic || CurrentReveal == Reveal.Explored;   // in graphic mode the map is only the fog overlay
            if (graphic)
            {
                // Each render covers GraphicMargin x the visible area, centred where you were; between renders the
                // image just slides with you every frame. Re-render on the timer, or early near the rendered edge.
                var offset = new Vector2(pos.x - _renderedAt.x, pos.z - _renderedAt.z);
                float slack = ViewMeters * (GraphicMargin - 1f) / 2f;
                if (Time.unscaledTime >= _nextRender || offset.magnitude > slack * 0.8f)
                {
                    _nextRender = Time.unscaledTime + GraphicInterval;
                    RenderGraphic(pos);
                    _renderedAt = pos;
                    offset = Vector2.zero;
                }
                float coverage = ViewMeters * GraphicMargin;
                var size = Vector2.one / GraphicMargin;
                _graphic.uvRect = new Rect(new Vector2(0.5f, 0.5f) + offset / coverage - size / 2f, size);
            }

            // Keep the player in the centre; the arrow shows where you face (north-up map).
            var uvSize = new Vector2(ViewMeters / (_w * _metersPerPixel), ViewMeters / (_h * _metersPerPixel));
            var uvCenter = new Vector2((pos.x - _origin.x) / (_w * _metersPerPixel), (pos.z - _origin.y) / (_h * _metersPerPixel));
            _map.uvRect = new Rect(uvCenter - uvSize / 2f, uvSize);
            _arrow.localEulerAngles = new Vector3(0f, 0f, -me.transform.eulerAngles.y);
        }

        // Logs once per scene why the minimap is hidden, to make "I don't see it" easy to debug.
        object _diagnosed;
        void Diagnose(object scene, ProtoActor me, bool show)
        {
            if (show || ReferenceEquals(_diagnosed, scene)) return;
            string why = scene == null ? "no game scene" : me == null ? "no local player yet" : me.dead ? "you are dead"
                : !_visible ? "hidden with M" : (Hub.s.uiman != null && Hub.s.uiman.isGameMenuOpen) ? "menu open"
                : _builtFor != scene ? "no NavMesh in this scene yet" : null;
            if (why == null) return;
            if (why == "no NavMesh in this scene yet") return;   // BuildMap logs that itself
            if (why == "no game scene" || why == "no local player yet") { if (Time.frameCount % 300 != 0) return; }
            else _diagnosed = scene;
            Debug.Log($"[Minimap] Hidden in {scene?.GetType().Name ?? "-"}: {why}");
        }

        bool BuildMap(object scene)
        {
            var (vertices, indices, source) = FloorTriangles();
            if (vertices.Length == 0)
            {
                if (Time.unscaledTime >= _nextBuildLog)
                {
                    _nextBuildLog = Time.unscaledTime + 5f;
                    Debug.Log($"[Minimap] Can't build map in {scene?.GetType().Name}: no NavMesh or A* graph yet (retrying)");
                }
                return false;
            }

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var v in vertices)
            {
                min = Vector2.Min(min, new Vector2(v.x, v.z));
                max = Vector2.Max(max, new Vector2(v.x, v.z));
            }
            var size = max - min;
            _metersPerPixel = Mathf.Max(size.x, size.y, 1f) / MaxTexSize;
            _w = Mathf.CeilToInt(size.x / _metersPerPixel) + 2;
            _h = Mathf.CeilToInt(size.y / _metersPerPixel) + 2;
            _origin = min - Vector2.one * _metersPerPixel;

            int n = _w * _h;
            _h0 = new float[n]; _h1 = new float[n];
            for (int i = 0; i < n; i++) _h0[i] = _h1[i] = float.NaN;
            _seen0 = new bool[n]; _seen1 = new bool[n];
            for (int t = 0; t + 2 < indices.Length; t += 3)
                Rasterize(vertices[indices[t]], vertices[indices[t + 1]], vertices[indices[t + 2]]);

            if (_tex != null) Destroy(_tex);
            _tex = new Texture2D(_w, _h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            _pixels = new Color32[n];
            _map.texture = _tex;
            _builtFor = scene;
            Debug.Log($"[Minimap] Map built: {_w}x{_h} px, {_metersPerPixel:0.00} m/px, {indices.Length / 3} triangles from {source}");
            return true;
        }

        // Walkable floor triangles: the Unity NavMesh (levels) or the A* Pathfinding recast graph
        // (tram/lobby scenes, which have no Unity NavMesh). Neither contains any actors.
        static (Vector3[] vertices, int[] indices, string source) FloorTriangles()
        {
            var tri = NavMesh.CalculateTriangulation();
            if (tri.vertices != null && tri.vertices.Length > 0) return (tri.vertices, tri.indices, "NavMesh");

            var verts = new List<Vector3>();
            var data = AstarPath.active != null ? AstarPath.active.data : null;
            if (data?.graphs != null)
                foreach (var graph in data.graphs)
                    graph?.GetNodes(node =>
                    {
                        if (node is TriangleMeshNode t && t.Walkable)
                            for (int i = 0; i < 3; i++) verts.Add((Vector3)t.GetVertex(i));
                    });
            var idx = new int[verts.Count];
            for (int i = 0; i < idx.Length; i++) idx[i] = i;
            return (verts.ToArray(), idx, "A* graph");
        }

        void Rasterize(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector2 P(Vector3 v) => new Vector2((v.x - _origin.x) / _metersPerPixel, (v.z - _origin.y) / _metersPerPixel);
            Vector2 pa = P(a), pb = P(b), pc = P(c);
            int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(pa.x, Mathf.Min(pb.x, pc.x))));
            int x1 = Mathf.Min(_w - 1, Mathf.CeilToInt(Mathf.Max(pa.x, Mathf.Max(pb.x, pc.x))));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(pa.y, Mathf.Min(pb.y, pc.y))));
            int y1 = Mathf.Min(_h - 1, Mathf.CeilToInt(Mathf.Max(pa.y, Mathf.Max(pb.y, pc.y))));
            float area = (pb.x - pa.x) * (pc.y - pa.y) - (pc.x - pa.x) * (pb.y - pa.y);
            if (Mathf.Abs(area) < 1e-6f) return;
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float w0 = ((pb.x - p.x) * (pc.y - p.y) - (pc.x - p.x) * (pb.y - p.y)) / area;
                float w1 = ((pc.x - p.x) * (pa.y - p.y) - (pa.x - p.x) * (pc.y - p.y)) / area;
                float w2 = 1f - w0 - w1;
                const float e = -0.35f;   // slight overlap so thin corridors don't break up
                if (w0 < e || w1 < e || w2 < e) continue;
                float height = w0 * a.y + w1 * b.y + w2 * c.y;
                int i = y * _w + x;
                if (float.IsNaN(_h0[i]) || Mathf.Abs(_h0[i] - height) < SameFloor) _h0[i] = float.IsNaN(_h0[i]) ? height : Mathf.Min(_h0[i], height);
                else if (float.IsNaN(_h1[i]) || Mathf.Abs(_h1[i] - height) < SameFloor) _h1[i] = float.IsNaN(_h1[i]) ? height : Mathf.Min(_h1[i], height);
            }
        }

        void RevealAround(Vector3 pos)
        {
            int cx = Mathf.RoundToInt((pos.x - _origin.x) / _metersPerPixel);
            int cy = Mathf.RoundToInt((pos.z - _origin.y) / _metersPerPixel);
            int r = Mathf.CeilToInt(RevealRadius / _metersPerPixel);
            for (int y = Mathf.Max(0, cy - r); y <= Mathf.Min(_h - 1, cy + r); y++)
            for (int x = Mathf.Max(0, cx - r); x <= Mathf.Min(_w - 1, cx + r); x++)
            {
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) > r * r) continue;
                int i = y * _w + x;
                if (Mathf.Abs(_h0[i] - pos.y) < FloorTolerance) _seen0[i] = true;
                if (Mathf.Abs(_h1[i] - pos.y) < FloorTolerance) _seen1[i] = true;
            }
        }

        void Draw(float myY, bool graphic)
        {
            bool whole = CurrentReveal == Reveal.Full;
            for (int i = 0; i < _pixels.Length; i++)
            {
                if (graphic)
                {
                    // Fog overlay over the camera view: dark where nothing has been explored yet.
                    _pixels[i] = _seen0[i] || _seen1[i] ? Empty : Unexplored;
                    continue;
                }
                bool on0 = !float.IsNaN(_h0[i]) && (whole || _seen0[i]);
                bool on1 = !float.IsNaN(_h1[i]) && (whole || _seen1[i]);
                if ((on0 && Mathf.Abs(_h0[i] - myY) < FloorTolerance) || (on1 && Mathf.Abs(_h1[i] - myY) < FloorTolerance)) _pixels[i] = Floor;
                else if (on0 || on1) _pixels[i] = OtherFloor;   // other floors, faint
                else _pixels[i] = Empty;
            }
            _tex.SetPixels32(_pixels);
            _tex.Apply(false);
        }

        void BuildUi()
        {
            _canvas = new GameObject("MinimapCanvas", typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
            _canvas.transform.SetParent(transform, false);
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 50;
            var scaler = _canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            // Dark frame in the bottom-left corner.
            var frame = new GameObject("Frame", typeof(RectTransform), typeof(Image), typeof(RectMask2D)).GetComponent<RectTransform>();
            frame.SetParent(_canvas.transform, false);
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0f, 0f);
            frame.anchoredPosition = new Vector2(24f, 24f + LegendHeight);
            frame.sizeDelta = new Vector2(ScreenSize, ScreenSize);
            _frame = frame;
            _dotTex = DotTexture();
            var bg = frame.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);
            bg.raycastTarget = false;

            _graphic = new GameObject("Graphic", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            var gRt = _graphic.rectTransform;
            gRt.SetParent(frame, false);
            gRt.anchorMin = Vector2.zero; gRt.anchorMax = Vector2.one;
            gRt.offsetMin = gRt.offsetMax = Vector2.zero;
            _graphic.raycastTarget = false;
            _graphic.enabled = false;

            _map = new GameObject("Map", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            var mapRt = _map.rectTransform;
            mapRt.SetParent(frame, false);
            mapRt.anchorMin = Vector2.zero; mapRt.anchorMax = Vector2.one;
            mapRt.offsetMin = mapRt.offsetMax = Vector2.zero;
            _map.raycastTarget = false;

            _arrow = new GameObject("You", typeof(RectTransform), typeof(RawImage)).GetComponent<RectTransform>();
            _arrow.SetParent(frame, false);
            _arrow.sizeDelta = new Vector2(18f, 18f);
            var arrowImg = _arrow.GetComponent<RawImage>();
            arrowImg.texture = ArrowTexture();
            arrowImg.raycastTarget = false;
            _canvas.enabled = false;
        }

        // Graphic style: an orthographic camera just above your head looking straight down (north-up, same
        // scale as the plain map). Actors are switched off for the duration of this camera render only.
        void RenderGraphic(Vector3 pos)
        {
            if (_cam == null)
            {
                _rt = new RenderTexture(GraphicSize, GraphicSize, 16, RenderTextureFormat.ARGB32);
                _cam = new GameObject("MinimapCamera").AddComponent<Camera>();
                _cam.transform.SetParent(transform, false);
                _cam.enabled = false;   // rendered manually, a few times per second
                _cam.orthographic = true;
                _cam.nearClipPlane = 0.05f;
                _cam.farClipPlane = 40f;
                _cam.clearFlags = CameraClearFlags.SolidColor;
                _cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                _cam.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));
                _cam.targetTexture = _rt;
                var urp = _cam.GetUniversalAdditionalCameraData();
                urp.renderPostProcessing = false;
                urp.renderShadows = false;
                _graphic.texture = _rt;
            }
            _cam.orthographicSize = ViewMeters * GraphicMargin / 2f;
            _cam.transform.SetPositionAndRotation(pos + Vector3.up * CameraAboveFeet, Quaternion.Euler(90f, 0f, 0f));

            var hidden = new List<Renderer>(_hiddenRenderers.Count);
            foreach (var r in _hiddenRenderers)
                if (r != null && !r.forceRenderingOff) { r.forceRenderingOff = true; hidden.Add(r); }
            // Unlit rooms would render black, so light the level evenly for this render only: a light shining
            // straight down, bright flat ambient and no fog. Everything is restored right after, so the game's
            // own view stays exactly as dark as before.
            if (_light == null)
            {
                _light = new GameObject("MinimapLight").AddComponent<Light>();
                _light.transform.SetParent(transform, false);
                _light.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                _light.type = LightType.Directional;
                _light.intensity = 1.1f;
                _light.shadows = LightShadows.None;
                _light.enabled = false;
            }
            var ambientMode = RenderSettings.ambientMode;
            var ambientLight = RenderSettings.ambientLight;
            var fog = RenderSettings.fog;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
            RenderSettings.fog = false;
            _light.enabled = true;
            try { _cam.Render(); }
            finally
            {
                _light.enabled = false;
                RenderSettings.ambientMode = ambientMode;
                RenderSettings.ambientLight = ambientLight;
                RenderSettings.fog = fog;
                foreach (var r in hidden) if (r != null) r.forceRenderingOff = false;
            }
        }

        Light _light;

        // Sorts everything that moves or can be picked up into players / monsters / items.
        // Disabled categories (and your own character) are hidden from the graphic camera; enabled ones get dots.
        void Scan(ProtoActor me)
        {
            _nextActorScan = Time.unscaledTime + 1f;
            _things.Clear();
            _hiddenRenderers.Clear();
            foreach (var actor in FindObjectsByType<ProtoActor>(FindObjectsSortMode.None))
            {
                if (actor == me) { _hiddenRenderers.AddRange(actor.GetComponentsInChildren<Renderer>(true)); continue; }
                bool player = actor.ActorType == ReluProtocol.Enum.ActorType.Player && !actor.IsMimic();
                bool shown = player ? ShowPlayers : ShowMonsters;
                if (!actor.dead) _things.Add((actor.transform, player ? PlayerDot : MonsterDot, shown));
                if (!shown) _hiddenRenderers.AddRange(actor.GetComponentsInChildren<Renderer>(true));
            }
            foreach (var item in FindObjectsByType<LootingLevelObject>(FindObjectsSortMode.None))
            {
                _things.Add((item.transform, ItemDot, ShowItems));
                if (!ShowItems) _hiddenRenderers.AddRange(item.GetComponentsInChildren<Renderer>(true));
            }
        }

        // Coloured dots for enabled categories, on your floor and inside the minimap's view.
        void Dots(Vector3 me)
        {
            int used = 0;
            float scale = _frame.rect.width / ViewMeters;
            foreach (var (t, color, shown) in _things)
            {
                if (!shown || t == null || !t.gameObject.activeInHierarchy) continue;
                var d = t.position - me;
                if (Mathf.Abs(d.y) > FloorTolerance) continue;
                var p = new Vector2(d.x, d.z) * scale;
                if (Mathf.Abs(p.x) > _frame.rect.width / 2f || Mathf.Abs(p.y) > _frame.rect.height / 2f) continue;
                if (used == _dots.Count)
                {
                    var dot = new GameObject("Dot", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                    dot.rectTransform.SetParent(_frame, false);
                    dot.rectTransform.sizeDelta = new Vector2(DotSize, DotSize);
                    dot.texture = _dotTex;
                    dot.raycastTarget = false;
                    _dots.Add(dot);
                }
                var img = _dots[used++];
                img.enabled = true;
                img.color = color;
                img.rectTransform.anchoredPosition = p;
            }
            for (int i = used; i < _dots.Count; i++) _dots[i].enabled = false;
            _arrow.SetAsLastSibling();
        }

        // Each enabled category with its colour, in one row under the map.
        void Legend()
        {
            var key = $"{ShowPlayers}{ShowMonsters}{ShowItems}";
            if (key == _legendKey) return;
            _legendKey = key;
            if (_legend == null)
            {
                _legend = new GameObject("Legend", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                _legend.SetParent(_canvas.transform, false);
                _legend.anchorMin = _legend.anchorMax = _legend.pivot = Vector2.zero;
                _legend.anchoredPosition = new Vector2(24f, 24f);
                _legend.sizeDelta = new Vector2(ScreenSize, LegendHeight - 4f);
                var bg = _legend.GetComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.55f);
                bg.raycastTarget = false;
                var layout = _legend.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.padding = new RectOffset(8, 8, 2, 2);
                layout.spacing = 4f;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            }
            foreach (Transform child in _legend) Destroy(child.gameObject);

            var font = FindAnyObjectByType<TMPro.TMP_Text>()?.font;
            void Entry(string label, Color color)
            {
                var dot = new GameObject("Dot", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                dot.transform.SetParent(_legend, false);
                dot.texture = _dotTex;
                dot.color = color;
                dot.raycastTarget = false;
                var el = dot.gameObject.AddComponent<LayoutElement>();
                el.preferredWidth = el.preferredHeight = 10f;
                var text = new GameObject("Label", typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>();
                text.transform.SetParent(_legend, false);
                if (font != null) text.font = font;
                text.fontSize = 13f;
                text.color = new Color(0.9f, 0.92f, 0.88f, 1f);
                text.text = label;
                text.raycastTarget = false;
                text.gameObject.AddComponent<LayoutElement>().preferredWidth = text.GetPreferredValues(label).x + 6f;
            }
            if (ShowPlayers) Entry("Players", PlayerDot);
            if (ShowMonsters) Entry("Monsters", MonsterDot);
            if (ShowItems) Entry("Items", ItemDot);

            // Nothing enabled: no legend, and the map sits back down in the corner.
            bool any = ShowPlayers || ShowMonsters || ShowItems;
            _legend.gameObject.SetActive(any);
            _frame.anchoredPosition = new Vector2(24f, 24f + (any ? LegendHeight : 0f));
        }

        static Texture2D DotTexture()
        {
            const int s = 16;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = new Vector2(x - 7.5f, y - 7.5f).magnitude;
                px[y * s + x] = d < 6.5f ? new Color32(255, 255, 255, 255) : d < 7.5f ? new Color32(0, 0, 0, 200) : new Color32(0, 0, 0, 0);
            }
            tex.SetPixels32(px);
            tex.Apply(false);
            return tex;
        }

        // A small upward-pointing triangle (rotated to your facing).
        static Texture2D ArrowTexture()
        {
            const int s = 32;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float halfWidth = (s - 1 - y) * 0.5f * 0.75f;   // wide at the bottom, point at the top
                bool inside = y > 2 && Math.Abs(x - (s - 1) / 2f) <= halfWidth;
                px[y * s + x] = inside ? new Color32(255, 196, 64, 255) : new Color32(0, 0, 0, 0);
            }
            tex.SetPixels32(px);
            tex.Apply(false);
            return tex;
        }
    }
}

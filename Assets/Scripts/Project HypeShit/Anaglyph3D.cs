using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Red/Cyan anaglyph. Four independent cameras: two for the world and two
/// for a separately composited stereo HUD. No camera stacking is used.
/// Works with Built-in RP and standard URP (not tested with HDRP/2D Renderer).
/// Press Backslash to toggle.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class Anaglyph3D : MonoBehaviour
{
    [Header("Управление")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.Backslash;
    [SerializeField] private bool _startEnabled = false;

    [Header("Стереоскопия")]
    [SerializeField, Range(0f, 0.1f)] private float _eyeDistance = 0.065f;
    [SerializeField, Min(0.1f)] private float _convergenceDistance = 4f;

    [Header("Интерфейс")]
    [Tooltip("Включить стереоскопию и для HUD. Если на вашей URP-конфигурации HUD пропадает, выключите: он останется обычным Overlay поверх 3D-сцены.")]
    [SerializeField] private bool _stereoHUD = true;
    [SerializeField] private Canvas[] _hudCanvases;
    [SerializeField, Min(0.1f)] private float _hudDistance = 2.5f;

    [Header("Шейдер")]
    [SerializeField] private Shader _compositeShader;

    private Camera _mainCamera;
    private Camera _leftWorld, _rightWorld;
    private Camera _leftHud, _rightHud;
    private RenderTexture _leftWorldRT, _rightWorldRT;
    private RenderTexture _leftHudRT, _rightHudRT;
    private Material _material;
    private GameObject _outputRoot;
    private RawImage _outputImage;

    private bool _initialized;
    private bool _enabled3D;
    private bool _hudInStereo;
    private int _originalMask;
    private CameraClearFlags _originalClearFlags;
    private Color _originalBackground;
    private int _hudLayer = -1;
    private float _nextLayerRefresh;

    private sealed class HudState
    {
        public Canvas Canvas;
        public CanvasScaler Scaler;
        public bool ScalerEnabled;
        public Camera WorldCamera;
        public RenderMode RenderMode;
        public RectTransform Rect;
        public Transform Parent;
        public int SiblingIndex;
        public Vector2 AnchorMin, AnchorMax, Pivot, SizeDelta, AnchoredPosition;
        public Vector3 LocalPosition, LocalScale;
        public Quaternion LocalRotation;
        public Vector2 LogicalSize;
    }

    private readonly List<HudState> _hudStates = new List<HudState>();
    private readonly Dictionary<GameObject, int> _savedLayers = new Dictionary<GameObject, int>();

    private void Start()
    {
        _mainCamera = GetComponent<Camera>();
        if (_mainCamera.orthographic)
        {
            Debug.LogError("Anaglyph3D: требуется перспективная (Perspective) камера.", this);
            enabled = false;
            return;
        }

        if (_compositeShader == null)
            _compositeShader = Shader.Find("Custom/AnaglyphComposite");

        if (_compositeShader == null || !_compositeShader.isSupported)
        {
            Debug.LogError("Anaglyph3D: не найден или не поддерживается шейдер Custom/AnaglyphComposite.", this);
            enabled = false;
            return;
        }

        _originalMask = _mainCamera.cullingMask;
        _originalClearFlags = _mainCamera.clearFlags;
        _originalBackground = _mainCamera.backgroundColor;

        _material = new Material(_compositeShader);
        _leftWorld = CreateCamera("Anaglyph / Left World", -8f, false);
        _rightWorld = CreateCamera("Anaglyph / Right World", -7f, false);
        _leftHud = CreateCamera("Anaglyph / Left HUD", -6f, true);
        _rightHud = CreateCamera("Anaglyph / Right HUD", -5f, true);

        CreateOutput();
        _initialized = true;
        SetStereo(_startEnabled);
    }

    private Camera CreateCamera(string cameraName, float depthOffset, bool transparentHUD)
    {
        GameObject obj = new GameObject(cameraName);
        obj.transform.SetParent(transform, false);
        Camera cam = obj.AddComponent<Camera>();
        cam.CopyFrom(_mainCamera);
        cam.stereoTargetEye = StereoTargetEyeMask.None;
        cam.enabled = false;
        cam.depth = _mainCamera.depth + depthOffset;
        cam.targetTexture = null;
        cam.transform.localPosition = Vector3.zero;
        cam.transform.localRotation = Quaternion.identity;
        cam.useOcclusionCulling = !transparentHUD;

        if (transparentHUD)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.allowHDR = false;
            cam.allowMSAA = false;
        }

        // URP: independent Base cameras, NO Overlay cameras or camera stacks.
        ConfigureStandaloneURPCamera(cam);
        return cam;
    }

    private static void ConfigureStandaloneURPCamera(Camera cam)
    {
        Type type = null;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
            if (type != null) break;
        }
        if (type == null) return; // Built-in RP

        try
        {
            Component data = cam.GetComponent(type);
            if (data == null) data = cam.gameObject.AddComponent(type);
            var renderType = type.GetProperty("renderType");
            if (renderType != null)
                renderType.SetValue(data, Enum.Parse(renderType.PropertyType, "Base"));
            var post = type.GetProperty("renderPostProcessing");
            if (post != null)
                post.SetValue(data, false); // transparent background must retain alpha
        }
        catch (Exception e)
        {
            Debug.LogWarning("Anaglyph3D: URP camera setup: " + e.Message);
        }
    }

    private void CreateOutput()
    {
        _outputRoot = new GameObject("Anaglyph Output", typeof(RectTransform), typeof(Canvas));
        Canvas outputCanvas = _outputRoot.GetComponent<Canvas>();
        outputCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Remains BEHIND any other Screen Space Overlay canvases.
        // Thus HUD is still visible when Stereo HUD is turned off.
        outputCanvas.sortingOrder = -32000;

        GameObject image = new GameObject("Anaglyph Composite", typeof(RectTransform), typeof(RawImage));
        image.transform.SetParent(_outputRoot.transform, false);
        RectTransform rect = image.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _outputImage = image.GetComponent<RawImage>();
        _outputImage.material = _material;
        _outputImage.color = Color.white;
        _outputImage.raycastTarget = false;
        _outputRoot.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
            SetStereo(!_enabled3D);
    }

    private void LateUpdate()
    {
        if (!_enabled3D) return;

        EnsureTextures();
        UpdateCameras();
        UpdateHUDTransform();

        if (_hudInStereo && Time.unscaledTime >= _nextLayerRefresh)
        {
            _nextLayerRefresh = Time.unscaledTime + 0.5f;
            foreach (HudState state in _hudStates)
                if (state.Canvas != null) SetHudLayerRecursively(state.Canvas.transform);
        }
    }

    public void SetStereo(bool enable)
    {
        if (!_initialized || enable == _enabled3D) return;

        if (enable)
        {
            // If stereo UI is not available, the original overlay canvas stays
            // unchanged and remains above the composite image.
            _hudInStereo = _stereoHUD && ConvertHUD();
            _enabled3D = true;
            EnsureTextures();
            UpdateCameras();
            UpdateHUDTransform();

            _mainCamera.cullingMask = 0;
            _mainCamera.clearFlags = CameraClearFlags.SolidColor;
            _mainCamera.backgroundColor = Color.black;

            _leftWorld.enabled = true;
            _rightWorld.enabled = true;
            _leftHud.enabled = _hudInStereo;
            _rightHud.enabled = _hudInStereo;
            _outputRoot.SetActive(true);
        }
        else
        {
            _enabled3D = false;
            _outputRoot.SetActive(false);
            _leftWorld.enabled = false;
            _rightWorld.enabled = false;
            _leftHud.enabled = false;
            _rightHud.enabled = false;

            _mainCamera.cullingMask = _originalMask;
            _mainCamera.clearFlags = _originalClearFlags;
            _mainCamera.backgroundColor = _originalBackground;
            RestoreHUD();
            _hudInStereo = false;
        }
    }

    private void UpdateCameras()
    {
        float half = _eyeDistance * 0.5f;
        Matrix4x4 left = _mainCamera.projectionMatrix;
        Matrix4x4 right = _mainCamera.projectionMatrix;
        float shift = left[0, 0] * half / Mathf.Max(0.1f, _convergenceDistance);
        left[0, 2] += shift;
        right[0, 2] -= shift;

        int worldMask = _hudInStereo ? (_originalMask & ~(1 << _hudLayer)) : _originalMask;
        SetEye(_leftWorld, -half, left, worldMask);
        SetEye(_rightWorld, half, right, worldMask);
        if (_hudInStereo)
        {
            SetEye(_leftHud, -half, left, 1 << _hudLayer);
            SetEye(_rightHud, half, right, 1 << _hudLayer);
        }
    }

    private void SetEye(Camera eye, float x, Matrix4x4 projection, int mask)
    {
        eye.transform.localPosition = new Vector3(x, 0f, 0f);
        eye.transform.localRotation = Quaternion.identity;
        eye.fieldOfView = _mainCamera.fieldOfView;
        eye.aspect = _mainCamera.aspect;
        eye.nearClipPlane = _mainCamera.nearClipPlane;
        eye.farClipPlane = _mainCamera.farClipPlane;
        eye.cullingMask = mask;
        eye.projectionMatrix = projection;
    }

    private void EnsureTextures()
    {
        int width = Mathf.Max(1, Screen.width);
        int height = Mathf.Max(1, Screen.height);
        if (_leftWorldRT != null && _leftWorldRT.width == width && _leftWorldRT.height == height &&
            (!_hudInStereo || _leftHudRT != null)) return;

        ReleaseTextures();
        _leftWorldRT = MakeTexture("Anaglyph Left World", width, height);
        _rightWorldRT = MakeTexture("Anaglyph Right World", width, height);
        _leftWorld.targetTexture = _leftWorldRT;
        _rightWorld.targetTexture = _rightWorldRT;

        if (_hudInStereo)
        {
            _leftHudRT = MakeTexture("Anaglyph Left HUD", width, height);
            _rightHudRT = MakeTexture("Anaglyph Right HUD", width, height);
            _leftHud.targetTexture = _leftHudRT;
            _rightHud.targetTexture = _rightHudRT;
        }

        _outputImage.texture = _leftWorldRT;
        _material.SetTexture("_RightTex", _rightWorldRT);
        _material.SetTexture("_LeftHudTex", _hudInStereo ? _leftHudRT : Texture2D.blackTexture);
        _material.SetTexture("_RightHudTex", _hudInStereo ? _rightHudRT : Texture2D.blackTexture);
        _material.SetFloat("_HasStereoHUD", _hudInStereo ? 1f : 0f);
    }

    private static RenderTexture MakeTexture(string name, int width, int height)
    {
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        rt.name = name;
        rt.filterMode = FilterMode.Bilinear;
        rt.Create();
        return rt;
    }

    private static int FindHudLayer()
    {
        int named = LayerMask.NameToLayer("AnaglyphHUD");
        if (named >= 8) return named;
        for (int i = 30; i >= 8; i--)
            if (string.IsNullOrEmpty(LayerMask.LayerToName(i))) return i;
        return -1;
    }

    private bool ConvertHUD()
    {
        if (_hudCanvases == null || _hudCanvases.Length == 0) return false;
        _hudLayer = FindHudLayer();
        if (_hudLayer < 0)
        {
            Debug.LogWarning("Anaglyph3D: нет свободного слоя для HUD. Интерфейс останется 2D Overlay.", this);
            return false;
        }

        Canvas.ForceUpdateCanvases();
        foreach (Canvas canvas in _hudCanvases)
        {
            if (canvas == null) continue;
            if (!canvas.isRootCanvas)
            {
                Debug.LogWarning("Anaglyph3D: укажи корневой Canvas: " + canvas.name, canvas);
                continue;
            }
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                Debug.LogWarning("Anaglyph3D: уже World Space Canvas не перемещаем: " + canvas.name, canvas);
                continue;
            }

            RectTransform rect = canvas.GetComponent<RectTransform>();
            if (rect == null) continue;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            Vector2 logicalSize = rect.rect.size;
            if (logicalSize.x < 1f || logicalSize.y < 1f)
                logicalSize = new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));

            _hudStates.Add(new HudState
            {
                Canvas = canvas, Scaler = scaler, ScalerEnabled = scaler != null && scaler.enabled,
                WorldCamera = canvas.worldCamera, RenderMode = canvas.renderMode,
                Rect = rect, Parent = rect.parent, SiblingIndex = rect.GetSiblingIndex(),
                AnchorMin = rect.anchorMin, AnchorMax = rect.anchorMax, Pivot = rect.pivot,
                SizeDelta = rect.sizeDelta, AnchoredPosition = rect.anchoredPosition,
                LocalPosition = rect.localPosition, LocalRotation = rect.localRotation,
                LocalScale = rect.localScale, LogicalSize = logicalSize
            });

            if (scaler != null) scaler.enabled = false;
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = _mainCamera; // Event camera for GraphicRaycaster
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = logicalSize;
            rect.localRotation = Quaternion.identity;
            SetHudLayerRecursively(canvas.transform);
        }

        if (_hudStates.Count == 0)
        {
            Debug.LogWarning("Anaglyph3D: не найдено корневых экранных Canvas. HUD останется обычным 2D.", this);
            return false;
        }
        return true;
    }

    private void SetHudLayerRecursively(Transform root)
    {
        if (root == null) return;
        GameObject go = root.gameObject;
        if (!_savedLayers.ContainsKey(go)) _savedLayers.Add(go, go.layer);
        go.layer = _hudLayer;
        for (int i = 0; i < root.childCount; i++)
            SetHudLayerRecursively(root.GetChild(i));
    }

    private void UpdateHUDTransform()
    {
        if (!_hudInStereo) return;
        float d = Mathf.Max(_hudDistance, _mainCamera.nearClipPlane + 0.05f);
        float h = 2f * d * Mathf.Tan(_mainCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
        foreach (HudState state in _hudStates)
        {
            if (state.Rect == null) continue;
            state.Rect.localPosition = Vector3.forward * d;
            state.Rect.localRotation = Quaternion.identity;
            state.Rect.localScale = Vector3.one * (h / Mathf.Max(1f, state.LogicalSize.y));
        }
    }

    private void RestoreHUD()
    {
        foreach (HudState state in _hudStates)
        {
            if (state.Canvas == null || state.Rect == null) continue;
            RectTransform rect = state.Rect;
            rect.SetParent(state.Parent, false);
            rect.SetSiblingIndex(state.SiblingIndex);
            rect.anchorMin = state.AnchorMin;
            rect.anchorMax = state.AnchorMax;
            rect.pivot = state.Pivot;
            rect.sizeDelta = state.SizeDelta;
            rect.anchoredPosition = state.AnchoredPosition;
            rect.localPosition = state.LocalPosition;
            rect.localRotation = state.LocalRotation;
            rect.localScale = state.LocalScale;
            state.Canvas.renderMode = state.RenderMode;
            state.Canvas.worldCamera = state.WorldCamera;
            if (state.Scaler != null) state.Scaler.enabled = state.ScalerEnabled;
        }
        _hudStates.Clear();

        foreach (var pair in _savedLayers)
            if (pair.Key != null) pair.Key.layer = pair.Value;
        _savedLayers.Clear();
    }

    private void ReleaseTextures()
    {
        if (_leftWorld != null) _leftWorld.targetTexture = null;
        if (_rightWorld != null) _rightWorld.targetTexture = null;
        if (_leftHud != null) _leftHud.targetTexture = null;
        if (_rightHud != null) _rightHud.targetTexture = null;
        Release(ref _leftWorldRT);
        Release(ref _rightWorldRT);
        Release(ref _leftHudRT);
        Release(ref _rightHudRT);
    }

    private void Release(ref RenderTexture texture)
    {
        if (texture == null) return;
        texture.Release();
        Destroy(texture);
        texture = null;
    }

    private void OnDisable()
    {
        if (_initialized && _enabled3D) SetStereo(false);
    }

    private void OnDestroy()
    {
        if (_initialized && _enabled3D) SetStereo(false);
        ReleaseTextures();
        if (_leftWorld != null) Destroy(_leftWorld.gameObject);
        if (_rightWorld != null) Destroy(_rightWorld.gameObject);
        if (_leftHud != null) Destroy(_leftHud.gameObject);
        if (_rightHud != null) Destroy(_rightHud.gameObject);
        if (_outputRoot != null) Destroy(_outputRoot);
        if (_material != null) Destroy(_material);
    }
}

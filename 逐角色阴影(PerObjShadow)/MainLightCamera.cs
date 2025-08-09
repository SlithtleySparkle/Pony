using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MainLightCamera : MonoBehaviour
{
    public GameObject Light;
    [Range(0.01f, 10.0f)]
    public float nearClipPane = 0.01f;
    public int RTSize = 1024;

    private Camera _camera = null;
    private RenderTexture _RT = null;

    private void Awake()
    {
        if (GameObject.Find("MainLightCamera") == null)
        {
            _camera = new GameObject("MainLightCamera").AddComponent<Camera>();
        }
    }
    void Start()
    {
        _camera.gameObject.transform.SetParent(Light.transform, true);
        _camera.gameObject.transform.localPosition = Vector3.zero;
        _camera.gameObject.transform.localRotation = Quaternion.identity;
        _camera.depth = Camera.main.depth;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = Color.black;
        _camera.aspect = 1;
        _camera.orthographic = true;
        _camera.nearClipPlane = nearClipPane;
        _camera.cullingMask = -1;
        _camera.GetUniversalAdditionalCameraData().renderShadows = false;

        _RT = new(RTSize, RTSize, 0)
        {
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            hideFlags = HideFlags.DontSave,
            filterMode = FilterMode.Bilinear
        };
        _camera.targetTexture = _RT;
    }
    private void OnDisable()
    {
        _RT.Release();
    }
}

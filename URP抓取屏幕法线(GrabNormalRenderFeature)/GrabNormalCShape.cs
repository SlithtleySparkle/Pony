using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GrabNormalCShape : MonoBehaviour
{
    private Camera _camera = null;
    private RenderTexture _RT = null;
    void Start()
    {
        if (GameObject.Find("MainCameraGrabNormal") == null)
        {
            _camera = new GameObject("MainCameraGrabNormal").AddComponent<Camera>();
        }
        _camera.gameObject.transform.SetParent(Camera.main.transform, true);
        _camera.gameObject.transform.localPosition = Vector3.zero;
        _camera.gameObject.transform.localRotation = Quaternion.identity;
        _camera.depth = Camera.main.depth;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = Color.black;
        _camera.aspect = Camera.main.aspect;
        _camera.orthographic = Camera.main.orthographic;
        _camera.orthographicSize = Camera.main.orthographicSize;
        _camera.nearClipPlane = Camera.main.nearClipPlane;
        _camera.farClipPlane = Camera.main.farClipPlane;
        _camera.cullingMask = Camera.main.cullingMask;
        _camera.GetUniversalAdditionalCameraData().renderShadows = false;

        _RT = new(Screen.width, Screen.height, 0)
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

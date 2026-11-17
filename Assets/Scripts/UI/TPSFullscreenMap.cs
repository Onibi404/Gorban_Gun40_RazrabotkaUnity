using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Opens a fullscreen view of the existing minimap RenderTexture on Tab.
/// Does not change the existing minimap scripts or player input actions.
/// </summary>
[DisallowMultipleComponent]
public sealed class TPSFullscreenMap : MonoBehaviour
{
    [SerializeField] private GameObject _mapPanel;
    [SerializeField] private Camera _minimapCamera;
    [SerializeField, Min(1f)] private float _zoomOutFactor = 2.2f;

    private float _normalOrthographicSize;
    private bool _isOpen;

    public void Configure(GameObject panel, Camera minimapCamera)
    {
        _mapPanel = panel;
        _minimapCamera = minimapCamera;
    }

    private void Awake()
    {
        if (_minimapCamera != null)
            _normalOrthographicSize = _minimapCamera.orthographicSize;
        if (_mapPanel != null)
            _mapPanel.SetActive(false);
    }

    private void Update()
    {
        if (_isOpen && Time.timeScale <= 0f)
            SetMapOpen(false);

        if (Time.timeScale <= 0f || Keyboard.current == null)
            return;

        if (Keyboard.current.tabKey.wasPressedThisFrame)
            SetMapOpen(!_isOpen);
    }

    private void OnDisable()
    {
        if (_isOpen)
            SetMapOpen(false);
    }

    private void SetMapOpen(bool open)
    {
        if (_mapPanel == null) return;
        _isOpen = open;
        _mapPanel.SetActive(open);

        if (_minimapCamera != null && _minimapCamera.orthographic)
        {
            if (_normalOrthographicSize <= 0f)
                _normalOrthographicSize = _minimapCamera.orthographicSize;
            _minimapCamera.orthographicSize = open
                ? _normalOrthographicSize * _zoomOutFactor
                : _normalOrthographicSize;
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

// F11 works independently of inventory and gameplay menus.
public class FullscreenToggle : MonoBehaviour
{
    int windowWidth = 1280, windowHeight = 720;
    bool requestedFullscreen;
    float nextToggle;
#if UNITY_EDITOR
    UnityEditor.EditorWindow gameView;
    bool originalMaximized, editorChanged;
#endif
    void Start() => requestedFullscreen = Screen.fullScreen;
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f11Key.wasPressedThisFrame)
            Toggle();
    }
    public void Toggle()
    {
        if (Time.unscaledTime < nextToggle) return;
        nextToggle = Time.unscaledTime + .2f;
#if UNITY_EDITOR
        // Editor play mode cannot change the OS player window; expand Game view instead.
        if (!gameView)
        {
            var type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            gameView = UnityEditor.EditorWindow.GetWindow(type);
            originalMaximized = gameView.maximized;
        }
        gameView.maximized = !gameView.maximized;
        editorChanged = true;
#else
        requestedFullscreen = !requestedFullscreen;
        if (requestedFullscreen)
        {
            windowWidth = Screen.width; windowHeight = Screen.height;
            var display = Screen.currentResolution;
            Screen.SetResolution(display.width, display.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            var display = Screen.currentResolution;
            int width = Mathf.Min(windowWidth, Mathf.Max(320, display.width - 80));
            int height = Mathf.Min(windowHeight, Mathf.Max(240, display.height - 80));
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
        }
#endif
    }
    void OnDisable()
    {
#if UNITY_EDITOR
        if (editorChanged && gameView) gameView.maximized = originalMaximized;
        editorChanged = false;
#endif
    }
}

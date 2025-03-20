using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class IsometricCameraController : MonoBehaviour
{
    [Header("相机引用")]
    [SerializeField] private CinemachineCamera orbitCamera;    // Orbit相机
    [SerializeField] private CinemachineCamera topdownCamera;  // 俯视相机
    
    [Header("输入控制")]
    [SerializeField] private CinemachineInputAxisController orbitXAxis;  // Orbit X轴控制器
    [SerializeField] private CinemachineInputAxisController orbitYAxis;  // Orbit Y轴控制器
    
    [Header("正交模式缩放设置")]
    [SerializeField] private float minOrthographicSize = 3f;   // 最小正交相机尺寸
    [SerializeField] private float maxOrthographicSize = 10f;  // 最大正交相机尺寸
    
    [Header("透视模式缩放设置")]
    [SerializeField] private float minFieldOfView = 20f;       // 最小视场角
    [SerializeField] private float maxFieldOfView = 60f;       // 最大视场角
    
    [Header("通用缩放设置")]
    [SerializeField] private float zoomSpeed = 1f;            // 缩放速度
    [SerializeField] private float smoothZoomSpeed = 10f;     // 平滑缩放速度
    
    private float targetOrthographicSize;                      // 目标正交相机尺寸
    private float targetFieldOfView;                          // 目标视场角
    private bool isOrbiting = false;                          // 是否正在进行轨道旋转
    private Camera mainCamera;                                // 主摄像机引用
    private bool isInTopdownMode = false;                     // 是否处于俯视模式

    private void Awake()
    {
        if (orbitCamera == null || topdownCamera == null)
        {
            Debug.LogError("IsometricCameraController: 未设置相机引用！");
            return;
        }

        // 获取主摄像机引用
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            var camObj = GameObject.FindWithTag("MainCamera");
            if (camObj != null)
            {
                mainCamera = camObj.GetComponent<Camera>();
            }
        }

        if (mainCamera == null)
        {
            Debug.LogError("IsometricCameraController: 未找到主摄像机！");
            return;
        }

        // 设置初始状态
        orbitCamera.Priority = 10;
        topdownCamera.Priority = 0;
        
        // 设置初始正交尺寸和视场角
        targetOrthographicSize = orbitCamera.Lens.OrthographicSize;
        targetFieldOfView = ConvertOrthographicSizeToFOV(targetOrthographicSize);
        
        // 设置相机投影模式
        SetCameraProjectionMode(true, false); // 轨道相机使用正交，俯视相机使用透视
        
        // 初始禁用轨道控制
        DisableOrbitControls();
    }

    private void Update()
    {
        // 处理鼠标右键按住状态
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            EnableOrbitControls();
        }
        else if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            DisableOrbitControls();
        }

        // 处理T键切换俯视角
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            ToggleTopdownCamera();
        }

        // 处理鼠标滚轮缩放
        float scrollValue = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scrollValue) > 0.01f)
        {
            HandleZoomInput(scrollValue);
        }

        // 平滑更新相机缩放
        UpdateCameraZoom();
    }

    private void HandleZoomInput(float scrollValue)
    {
        if (isInTopdownMode)
        {
            // 俯视模式（透视）下的缩放
            targetFieldOfView = Mathf.Clamp(
                targetFieldOfView + scrollValue * zoomSpeed * 4f, // 视场角变化速度调整
                minFieldOfView,
                maxFieldOfView
            );
            
            // 同步更新正交尺寸，以便切换回正交模式时保持视觉一致性
            targetOrthographicSize = ConvertFOVToOrthographicSize(targetFieldOfView);
        }
        else
        {
            // 轨道模式（正交）下的缩放
            targetOrthographicSize = Mathf.Clamp(
                targetOrthographicSize - scrollValue * zoomSpeed,
                minOrthographicSize,
                maxOrthographicSize
            );
            
            // 同步更新视场角，以便切换到透视模式时保持视觉一致性
            targetFieldOfView = ConvertOrthographicSizeToFOV(targetOrthographicSize);
        }
    }
    
    // 将正交尺寸转换为等效的视场角（FOV）
    private float ConvertOrthographicSizeToFOV(float orthographicSize)
    {
        // 正交尺寸越大，对应的视场角越大
        // 在这里使用线性映射，可以根据需要调整算法
        float normalizedSize = Mathf.InverseLerp(minOrthographicSize, maxOrthographicSize, orthographicSize);
        return Mathf.Lerp(minFieldOfView, maxFieldOfView, normalizedSize);
    }
    
    // 将视场角（FOV）转换为等效的正交尺寸
    private float ConvertFOVToOrthographicSize(float fieldOfView)
    {
        // 视场角越大，对应的正交尺寸越大
        // 在这里使用线性映射，可以根据需要调整算法
        float normalizedFOV = Mathf.InverseLerp(minFieldOfView, maxFieldOfView, fieldOfView);
        return Mathf.Lerp(minOrthographicSize, maxOrthographicSize, normalizedFOV);
    }

    private void EnableOrbitControls()
    {
        if (orbitXAxis != null) orbitXAxis.enabled = true;
        if (orbitYAxis != null) orbitYAxis.enabled = true;
        isOrbiting = true;
    }

    private void DisableOrbitControls()
    {
        if (orbitXAxis != null) orbitXAxis.enabled = false;
        if (orbitYAxis != null) orbitYAxis.enabled = false;
        isOrbiting = false;
    }

    private void UpdateCameraZoom()
    {
        // 更新Orbit相机缩放（正交模式）
        var orbitLens = orbitCamera.Lens;
        orbitLens.OrthographicSize = Mathf.Lerp(
            orbitLens.OrthographicSize,
            targetOrthographicSize,
            Time.unscaledDeltaTime * smoothZoomSpeed
        );
        orbitCamera.Lens = orbitLens;

        // 更新Topdown相机缩放（透视模式）
        var topdownLens = topdownCamera.Lens;
        topdownLens.FieldOfView = Mathf.Lerp(
            topdownLens.FieldOfView,
            targetFieldOfView,
            Time.unscaledDeltaTime * smoothZoomSpeed
        );
        topdownCamera.Lens = topdownLens;
    }

    public void ToggleTopdownCamera()
    {
        isInTopdownMode = topdownCamera.Priority == 0; // 切换前的状态取反
        
        if (isInTopdownMode)
        {
            topdownCamera.Priority = 11;
            orbitCamera.Priority = 0;
        }
        else
        {
            topdownCamera.Priority = 0;
            orbitCamera.Priority = 10;
        }
        
        // 切换摄像机投影模式
        if (mainCamera != null)
        {
            mainCamera.orthographic = !isInTopdownMode;
        }
    }
    
    // 设置相机投影模式
    private void SetCameraProjectionMode(bool orbitIsOrthographic, bool topdownIsOrthographic)
    {
        // 设置Orbit相机投影模式
        var orbitLens = orbitCamera.Lens;
        orbitLens.ModeOverride = orbitIsOrthographic ? 
            LensSettings.OverrideModes.Orthographic : 
            LensSettings.OverrideModes.Perspective;
        orbitCamera.Lens = orbitLens;
        
        // 设置Topdown相机投影模式
        var topdownLens = topdownCamera.Lens;
        topdownLens.ModeOverride = topdownIsOrthographic ? 
            LensSettings.OverrideModes.Orthographic : 
            LensSettings.OverrideModes.Perspective;
        topdownCamera.Lens = topdownLens;
        
        // 设置初始FOV
        if (!topdownIsOrthographic)
        {
            topdownLens.FieldOfView = targetFieldOfView;
            topdownCamera.Lens = topdownLens;
        }
    }
}
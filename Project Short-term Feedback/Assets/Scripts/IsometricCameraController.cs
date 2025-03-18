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
    
    [Header("缩放设置")]
    [SerializeField] private float minOrthographicSize = 3f;   // 最小正交相机尺寸
    [SerializeField] private float maxOrthographicSize = 10f;  // 最大正交相机尺寸
    [SerializeField] private float zoomSpeed = 1f;            // 缩放速度
    [SerializeField] private float smoothZoomSpeed = 10f;     // 平滑缩放速度
    
    private float targetOrthographicSize;                      // 目标正交相机尺寸
    private bool isOrbiting = false;                          // 是否正在进行轨道旋转

    private void Awake()
    {
        if (orbitCamera == null || topdownCamera == null)
        {
            Debug.LogError("IsometricCameraController: 未设置相机引用！");
            return;
        }

        // 设置初始状态
        orbitCamera.Priority = 10;
        topdownCamera.Priority = 0;
        
        // 设置初始正交尺寸
        targetOrthographicSize = orbitCamera.Lens.OrthographicSize;
        
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
            // 更新目标正交尺寸
            targetOrthographicSize = Mathf.Clamp(
                targetOrthographicSize - scrollValue * zoomSpeed,
                minOrthographicSize,
                maxOrthographicSize
            );
        }

        // 平滑更新相机正交尺寸
        UpdateCameraZoom();
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
        // 更新Orbit相机缩放
        var orbitLens = orbitCamera.Lens;
        orbitLens.OrthographicSize = Mathf.Lerp(
            orbitLens.OrthographicSize,
            targetOrthographicSize,
            Time.unscaledDeltaTime * smoothZoomSpeed
        );
        orbitCamera.Lens = orbitLens;

        // 同步更新俯视相机缩放
        var topdownLens = topdownCamera.Lens;
        topdownLens.OrthographicSize = orbitLens.OrthographicSize;
        topdownCamera.Lens = topdownLens;
    }

    public void ToggleTopdownCamera()
    {
        if (topdownCamera.Priority == 0)
        {
            topdownCamera.Priority = 11;
            orbitCamera.Priority = 0;
        }
        else
        {
            topdownCamera.Priority = 0;
            orbitCamera.Priority = 10;
        }
    }
}
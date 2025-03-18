using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class IsometricCameraController : MonoBehaviour
{
    [Header("虚拟相机引用")]
    [SerializeField] private CinemachineCamera[] virtualCameras = new CinemachineCamera[4];
    [SerializeField] private CinemachineCamera topdownCamera;
    
    [Header("切换设置")]
    private CinemachineBrain cinemachineBrain;
    private int currentCameraIndex = 0;                   // 当前激活的相机索引  
    
    [Header("缩放设置")]
    [SerializeField] private float minOrthographicSize = 3f;  // 最小正交相机尺寸
    [SerializeField] private float maxOrthographicSize = 10f; // 最大正交相机尺寸
    [SerializeField] private float zoomSpeed = 1f;           // 缩放速度
    [SerializeField] private float smoothZoomSpeed = 10f;    // 平滑缩放速度
    
    private float targetOrthographicSize;                 // 目标正交相机尺寸

    private void Awake()
    {
        // 获取CinemachineBrain组件
        cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();
        if (cinemachineBrain == null)
        {
            Debug.LogError("IsometricCameraController: 主相机上未找到CinemachineBrain组件！");
            return;
        }

        // 设置初始相机状态
        for (int i = 0; i < virtualCameras.Length; i++)
        {
            if (virtualCameras[i] != null)
            {
                virtualCameras[i].Priority = (i == currentCameraIndex) ? 1 : 0;
                // 设置初始正交尺寸
                targetOrthographicSize = virtualCameras[i].Lens.OrthographicSize;
            }
            else
            {
                Debug.LogWarning($"IsometricCameraController: 虚拟相机 {i} 未设置！");
            }
        }

        // 设置topdownCamera的优先级
        topdownCamera.Priority = 0;
    }

    private void Update()
    {
        // 检测输入并切换相机
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            RotateCamera(-1); // 逆时针旋转
        }
        else if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            RotateCamera(1);  // 顺时针旋转
        }

        // 按下t键切换到topdownCamera，按下t键切换回原来的相机状态
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

        // 平滑更新所有相机的正交尺寸
        foreach (var cam in virtualCameras)
        {
            if (cam != null)
            {
                var lens = cam.Lens;
                lens.OrthographicSize = Mathf.Lerp(
                    lens.OrthographicSize,
                    targetOrthographicSize,
                    Time.unscaledDeltaTime * smoothZoomSpeed
                );
                cam.Lens = lens;
            }
        }

        // 同步更新topdownCamera的正交尺寸
        if (topdownCamera != null)
        {
            var topdownLens = topdownCamera.Lens;
            topdownLens.OrthographicSize = Mathf.Lerp(
                topdownLens.OrthographicSize,
                targetOrthographicSize,
                Time.unscaledDeltaTime * smoothZoomSpeed
            );
            topdownCamera.Lens = topdownLens;
        }
    }

    private void RotateCamera(int direction)
    {
        if (virtualCameras == null || virtualCameras.Length == 0) return;

        // 计算新的相机索引（确保在0-3之间循环）
        int newIndex = (currentCameraIndex + direction + 4) % 4;
        
        // 确保目标相机存在
        if (virtualCameras[newIndex] == null)
        {
            Debug.LogWarning($"IsometricCameraController: 目标虚拟相机 {newIndex} 不存在！");
            return;
        }

        // 更新相机优先级
        virtualCameras[currentCameraIndex].Priority = 0;
        virtualCameras[newIndex].Priority = 10;

        // 更新当前相机索引
        currentCameraIndex = newIndex;
    }

    // 公共方法：直接切换到指定索引的相机
    public void SwitchToCamera(int index)
    {
        if (index < 0 || index >= 4 || virtualCameras[index] == null)
        {
            Debug.LogWarning($"IsometricCameraController: 无效的相机索引 {index}！");
            return;
        }

        // 更新相机优先级
        virtualCameras[currentCameraIndex].Priority = 0;
        virtualCameras[index].Priority = 10;
        currentCameraIndex = index;
    }

    // 公共方法：topdownCamera的toggle方法
    public void ToggleTopdownCamera()
    {
        topdownCamera.Priority = topdownCamera.Priority == 0 ? 11 : 0;
    }   
}
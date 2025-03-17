using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsometricCameraController : MonoBehaviour
{
    [Header("跟随设置")]
    [SerializeField] private Transform target;            // 跟随目标（通常是玩家）
    [SerializeField] private Vector3 offset = new Vector3(0, 10, -10); // 相机相对于目标的偏移量
    [SerializeField] private float smoothSpeed = 5.0f;    // 相机跟随平滑系数
    [SerializeField] private bool lookAtTarget = true;    // 是否始终看向目标

    [Header("正交相机设置")]
    [SerializeField] private bool useOrthographic = true; // 是否使用正交投影
    [SerializeField] private float orthographicSize = 5f; // 正交相机尺寸

    private Camera cam;

    private void Awake()
    {
        // 获取相机组件
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("IsometricCameraController: 未找到相机组件！");
            return;
        }

        // 设置相机为正交模式
        if (useOrthographic)
        {
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
        }
    }

    private void Start()
    {
        // 如果未指定目标，尝试找到带有PlayerController组件的物体
        if (target == null)
        {
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                Debug.LogWarning("IsometricCameraController: 未找到PlayerController！请手动指定目标。");
            }
        }

        // 立即更新相机位置
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;
            transform.position = desiredPosition;
            
            if (lookAtTarget)
            {
                transform.LookAt(target);
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // 计算期望位置
        Vector3 desiredPosition = target.position + offset;
        
        // 平滑移动相机
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // 相机始终看向目标
        if (lookAtTarget)
        {
            transform.LookAt(target);
        }
    }

    // 设置相机尺寸
    public void SetOrthographicSize(float size)
    {
        orthographicSize = size;
        if (cam != null && cam.orthographic)
        {
            cam.orthographicSize = orthographicSize;
        }
    }
} 
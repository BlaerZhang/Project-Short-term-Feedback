using UnityEngine;

public class LandingMarker : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 90f; // 每秒旋转的角度
    [SerializeField] private float pulseSpeed = 1f;    // 脉动速度
    [SerializeField] private float minScale = 0.8f;    // 最小缩放
    [SerializeField] private float maxScale = 1.2f;    // 最大缩放

    private Transform visualTransform;    // 视觉效果子物体的变换

    private void Awake()
    {
        // 如果没有子物体，就使用自身
        if (transform.childCount > 0)
        {
            visualTransform = transform.GetChild(0);
        }
        else
        {
            visualTransform = transform;
        }

        // 确保标记平躺在地面上
        visualTransform.localRotation = Quaternion.Euler(90, 0, 0);
    }

    private void Update()
    {
        // 绕Z轴旋转（因为我们已经旋转了90度，所以现在Z轴垂直于地面）
        visualTransform.Rotate(0, 0, rotationSpeed * Time.unscaledDeltaTime);
        
        // 脉动缩放
        float scale = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1) * 0.5f);
        visualTransform.localScale = new Vector3(scale, scale, scale);
    }
} 
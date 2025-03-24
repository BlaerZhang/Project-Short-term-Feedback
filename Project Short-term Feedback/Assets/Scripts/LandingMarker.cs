using UnityEngine;

public class LandingMarker : MonoBehaviour
{
    [SerializeField] private Color validColor = Color.green; // 有效位置的颜色
    [SerializeField] private Color invalidColor = Color.red; // 无效位置的颜色

    private Transform visualTransform;    // 视觉效果子物体的变换
    private Renderer markerRenderer;      // 标记的渲染器

    private void Awake()
    {
        // 如果没有子物体，就使用自身
        if (transform.childCount > 0)
        {
            visualTransform = transform.GetChild(0);
            markerRenderer = visualTransform.GetComponent<Renderer>();
        }
        else
        {
            visualTransform = transform;
            markerRenderer = GetComponent<Renderer>();
        }

        // 确保标记平躺在地面上
        visualTransform.localRotation = Quaternion.Euler(90, 0, 0);
    }

    // 设置标记颜色
    public void SetValid(bool isValid)
    {
        if (markerRenderer != null)
        {
            markerRenderer.material.color = isValid ? validColor : invalidColor;
        }
    }
    
    // 设置指向方向
    public void SetDirection(Vector3 direction)
    {
        if (visualTransform != null)
        {
            // 确保方向向量在水平面上
            direction.y = 0;
            
            if (direction.magnitude > 0.01f)
            {
                // 计算从(0,1)到目标方向的旋转
                // 假设箭头默认指向(0,1)，即Unity世界坐标中的Z轴正方向
                float angle = Vector3.SignedAngle(Vector3.forward, direction, Vector3.up);
                
                // 应用旋转：先重置到标准朝向，然后旋转到目标方向
                visualTransform.localRotation = Quaternion.Euler(90, angle, 0);
            }
        }
    }
} 
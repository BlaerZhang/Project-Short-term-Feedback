using UnityEngine;

public class LandingMarker : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 90f; // 每秒旋转的角度
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

    private void Update()
    {
        // 绕Z轴旋转（因为我们已经旋转了90度，所以现在Z轴垂直于地面）
        visualTransform.Rotate(0, 0, rotationSpeed * Time.unscaledDeltaTime);
    }

    // 设置标记颜色
    public void SetValid(bool isValid)
    {
        if (markerRenderer != null)
        {
            markerRenderer.material.color = isValid ? validColor : invalidColor;
        }
    }
} 
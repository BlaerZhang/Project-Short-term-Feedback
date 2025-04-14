using UnityEngine;

/// <summary>
/// UI初始化器，确保UI管理器在游戏开始时加载
/// </summary>
public class UIInitializer : MonoBehaviour
{
    [SerializeField] private GameObject uiManagerPrefab;

    private void Awake()
    {
        // 检查是否已经存在UIManager
        if (UIManager.Instance == null)
        {
            // 如果没有指定预制体，尝试从Resources加载
            if (uiManagerPrefab == null)
            {
                uiManagerPrefab = Resources.Load<GameObject>("UI/UIManager");
            }

            // 如果找到预制体，实例化它
            if (uiManagerPrefab != null)
            {
                Instantiate(uiManagerPrefab);
                Debug.Log("UIInitializer: 已实例化UIManager");
            }
            else
            {
                // 如果找不到预制体，创建一个空物体并添加UIManager组件
                GameObject uiManagerObj = new GameObject("UIManager");
                uiManagerObj.AddComponent<UIManager>();
                Debug.Log("UIInitializer: 已创建UIManager");
            }
        }
    }
} 
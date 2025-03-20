using UnityEngine;

/// <summary>
/// 障碍物属性接口，定义障碍物的各种属性
/// </summary>
public interface IObstacleProperties
{
    /// <summary>
    /// 获取障碍物是否不可跳过
    /// </summary>
    bool CannotJumpOver { get; }
    
    /// <summary>
    /// 获取障碍物是否可以俯身通过
    /// </summary>
    bool CanCrouchUnder { get; }
}

/// <summary>
/// 辅助工具类，用于检查障碍物属性
/// </summary>
public static class ObstaclePropertiesHelper
{
    /// <summary>
    /// 检查碰撞器是否具有不可跃过属性
    /// </summary>
    public static bool IsNotJumpable(Collider collider)
    {
        // 首先尝试获取IObstacleProperties接口
        IObstacleProperties props = collider.GetComponent<IObstacleProperties>();
        if (props != null)
        {
            return props.CannotJumpOver;
        }
        
        // 如果找不到接口，也可以尝试通过MonoBehaviour组件查找
        MonoBehaviour[] components = collider.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            // 检查组件是否实现了IObstacleProperties接口
            if (component is IObstacleProperties)
            {
                return ((IObstacleProperties)component).CannotJumpOver;
            }
        }
        
        // 默认返回false，表示可以跳过
        return false;
    }
} 
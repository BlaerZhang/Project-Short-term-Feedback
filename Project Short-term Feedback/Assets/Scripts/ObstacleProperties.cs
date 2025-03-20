using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Gameplay/Obstacle Properties")]
public class ObstacleProperties : MonoBehaviour, IObstacleProperties
{
    [Header("障碍物属性")]
    [Tooltip("如果勾选，该障碍物不能被跳跃越过")]
    [SerializeField] private bool cannotJumpOver = false;
    
    [Tooltip("如果勾选，该障碍物可以通过俯身通过")]
    [SerializeField] private bool canCrouchUnder = false;
    
    // 可以在这里添加更多的障碍物属性...
    
    // 获取是否不可跳跃越过
    public bool CannotJumpOver => cannotJumpOver;
    
    // 获取是否可以俯身通过
    public bool CanCrouchUnder => canCrouchUnder;
    
    // 静态辅助方法：检查碰撞器上的GameObject是否有指定属性
    public static bool HasProperty<T>(Collider collider, System.Func<ObstacleProperties, T> propertyGetter, T expectedValue)
    {
        ObstacleProperties props = collider.GetComponent<ObstacleProperties>();
        if (props == null)
            return false;
            
        return EqualityComparer<T>.Default.Equals(propertyGetter(props), expectedValue);
    }
    
    // 静态辅助方法：检查碰撞器是否不可跳跃越过
    public static bool IsNotJumpable(Collider collider)
    {
        ObstacleProperties props = collider.GetComponent<ObstacleProperties>();
        if (props == null)
            return false; // 如果没有属性组件，默认可跳跃
            
        return props.CannotJumpOver;
    }
} 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{

    [System.Serializable]
    public enum AttributeType
    {
        None = 0,           // 无
        Health = 2,         // 生命值
        Energy = 3,         // 能量
        Happiness = 4,      // 快乐值
        Hunger = 6,         // 饥饿度
        Thirst = 8,         // 口渴度
        Heat = 10,          // 热量

        // 通用属性，请根据需要重命名
        Attribute5 = 50,    // 属性5
        Attribute6 = 60,    // 属性6
        Attribute7 = 70,    // 属性7
        Attribute8 = 80,    // 属性8
        Attribute9 = 90,    // 属性9
    }

    /// <summary>
    /// 属性数据（生命值、能量、饥饿度等）
    /// </summary>

    [CreateAssetMenu(fileName = "AttributeData", menuName = "FarmingEngine/AttributeData", order = 11)]
    public class AttributeData : ScriptableObject
    {
        public AttributeType type;      // 属性类型
        public string title;            // 属性标题

        [Space(5)]

        public float start_value = 100f;    // 初始值
        public float max_value = 100f;      // 最大值

        public float value_per_hour = -100f;    // 每游戏小时增加（或减少）的值

        [Header("数值降至零时")]
        public float deplete_hp_loss = -100f;   // 每小时减少的生命值
        public float deplete_move_mult = 1f;    // 移动倍率
        public float deplete_attack_mult = 1f;  // 攻击倍率

    }

}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{

    [System.Serializable]
    public enum BonusType
    {
        None = 0,           // 无

        SpeedBoost = 5,     // 值为百分比的速度提升
        AttackBoost = 7,    // 值为百分比的攻击提升
        ArmorBoost = 8,     // 值为百分比的护甲提升

        HealthUp = 10,      // 每游戏小时增加的生命值
        HungerUp = 11,      // 每游戏小时增加的饥饿度
        ThirstUp = 12,      // 每游戏小时增加的口渴度
        HappyUp = 13,       // 每游戏小时增加的快乐值
        EnergyUp = 14,      // 每游戏小时增加的能量值

        HealthMax = 20,     // 每游戏小时增加的最大生命值
        HungerMax = 21,     // 每游戏小时增加的最大饥饿度
        ThirstMax = 22,     // 每游戏小时增加的最大口渴度
        HappyMax = 23,      // 每游戏小时增加的最大快乐值
        EnergyMax = 24,     // 每游戏小时增加的最大能量值

        ColdResist = 30,    // 增加抗寒能力

        Invulnerable = 40,  // 伤害免疫百分比，0.5 表示减少一半伤害，1 表示完全免疫
    }

    /// <summary>
    /// 奖励效果数据文件（应用于角色的持续效果，例如装备物品或靠近建筑时应用的效果）
    /// </summary>
    
    [CreateAssetMenu(fileName = "BonusEffect", menuName = "FarmingEngine/BonusEffect", order = 7)]
    public class BonusEffectData : ScriptableObject
    {
        public string effect_id;    // 效果ID
        public BonusType type;      // 奖励类型
        public GroupData target;    // 目标物品组数据
        public float value;         // 值


        public static BonusType GetAttributeBonusType(AttributeType type)
        {
            if (type == AttributeType.Health)
                return BonusType.HealthUp;
            if (type == AttributeType.Hunger)
                return BonusType.HungerUp;
            if (type == AttributeType.Thirst)
                return BonusType.ThirstUp;
            if (type == AttributeType.Happiness)
                return BonusType.HappyUp;
            if (type == AttributeType.Energy)
                return BonusType.EnergyUp;
            if (type == AttributeType.Heat)
                return BonusType.ColdResist;
            return BonusType.None;
        }

        public static BonusType GetAttributeMaxBonusType(AttributeType type)
        {
            if (type == AttributeType.Health)
                return BonusType.HealthMax;
            if (type == AttributeType.Hunger)
                return BonusType.HungerMax;
            if (type == AttributeType.Thirst)
                return BonusType.ThirstMax;
            if (type == AttributeType.Happiness)
                return BonusType.HappyMax;
            if (type == AttributeType.Energy)
                return BonusType.EnergyMax;
            return BonusType.None;
        }
    }

}

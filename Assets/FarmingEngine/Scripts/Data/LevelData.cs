using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 可增加的等级类型（如狩猎、采矿等）。
    /// </summary>

    [CreateAssetMenu(fileName = "LevelData", menuName = "SurvivalEngine/LevelData", order = 11)]
    public class LevelData : ScriptableObject
    {
        public string id;               // 等级数据的唯一标识符
        public int level;               // 等级
        public int xp_required;         // 所需经验值

        [Space(5)]
        public LevelUnlockBonus[] unlock_bonuses;   // 解锁奖励
        public CraftData[] unlock_craft;            // 解锁的制作数据

        private static List<LevelData> level_data = new List<LevelData>();  // 等级数据的静态列表

        public static void Load(string folder = "")
        {
            level_data.Clear();
            level_data.AddRange(Resources.LoadAll<LevelData>(folder));
        }

        public static LevelData GetLevel(string id, int level)
        {
            foreach (LevelData data in level_data)
            {
                if (data.id == id && data.level == level)
                {
                    return data;
                }
            }
            return GetMaxLevel(id);
        }

        public static LevelData GetMaxLevel(string id)
        {
            LevelData max = null;
            foreach (LevelData level in level_data)
            {
                if (level.id == id)
                {
                    if (max == null || level.level > max.level)
                        max = level;
                }
            }
            return max;
        }

        public static LevelData GetLevelByXP(string id, int xp)
        {
            foreach (LevelData current in level_data)
            {
                if (current.id == id)
                {
                    LevelData next = GetLevel(id, current.level + 1);
                    if (next != null && xp >= current.xp_required && xp < next.xp_required)
                    {
                        return current;
                    }
                }
            }
            return GetMaxLevel(id);
        }
    }

    [System.Serializable]
    public class LevelUnlockBonus
    {
        public BonusType bonus;          // 奖励类型
        public float bonus_value;        // 奖励数值
        public GroupData target_group;   // 目标组
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// 玩家角色属性管理器
    /// </summary>
    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterAttribute : MonoBehaviour
    {
        [Header("属性")]
        public AttributeData[] attributes;   // 属性数据数组（Z 这里设置的是能量和血量）

        public UnityAction onGainLevel;     // 获得等级时的事件

        private PlayerCharacter character;  // 玩家角色对象

        private float move_speed_mult = 1f; // 移动速度倍率
        private float attack_mult = 1f;     // 攻击倍率
        private bool depleting = false;     // 是否正在扣除属性值导致生命值下降

        private void Awake()
        {
            character = GetComponent<PlayerCharacter>();  // 获取玩家角色组件
        }

        void Start()
        {
            // 初始化属性（Z 这里没看懂）
            foreach (AttributeData attr in attributes)
            {
                if (!CharacterData.HasAttribute(attr.type))
                    CharacterData.SetAttributeValue(attr.type, attr.start_value, attr.max_value);
            }
        }


        //Z 先判断是否符合条件，符合处理属性值等
        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (character.IsDead())
                return;
            //TODO：2025.05.25
            // 更新属性
            float game_speed = TheGame.Get().GetGameTimeSpeedPerSec();//Z 这里似乎是获取游戏世界相对于现实世界的时间流速
            // 更新每小时属性值
            foreach (AttributeData attr in attributes)
            {
                float update_value = attr.value_per_hour + GetBonusEffectTotal(BonusEffectData.GetAttributeBonusType(attr.type));
                update_value = update_value * game_speed * Time.deltaTime;
                CharacterData.AddAttributeValue(attr.type, update_value, GetAttributeMax(attr.type));//  Z 感觉这里不对劲，这样不就会是时间越长属性越高吗？？？
            }

            // 处理属性耗尽惩罚
            move_speed_mult = 1f;
            attack_mult = 1f;
            depleting = false;

            foreach (AttributeData attr in attributes)
            {
                if (GetAttributeValue(attr.type) < 0.01f)
                {
                    move_speed_mult = move_speed_mult * attr.deplete_move_mult;
                    attack_mult = attack_mult * attr.deplete_attack_mult;
                    float update_value = attr.deplete_hp_loss * game_speed * Time.deltaTime;
                    AddAttribute(AttributeType.Health, update_value);
                    if (attr.deplete_hp_loss < 0f)
                        depleting = true;
                }
            }

            // 处理生命值耗尽
            float health = GetAttributeValue(AttributeType.Health);
            if (health < 0.01f)
                character.Kill();

            // 睡眠增加属性值
            if (character.IsSleeping())
            {
                ActionSleep sleep_target = character.GetSleepTarget();
                AddAttribute(AttributeType.Health, sleep_target.sleep_hp_hour * game_speed * Time.deltaTime);
                AddAttribute(AttributeType.Energy, sleep_target.sleep_energy_hour * game_speed * Time.deltaTime);
                AddAttribute(AttributeType.Hunger, sleep_target.sleep_hunger_hour * game_speed * Time.deltaTime);
                AddAttribute(AttributeType.Happiness, sleep_target.sleep_happiness_hour * game_speed * Time.deltaTime);
            }
        }

        // 添加属性值
        public void AddAttribute(AttributeType type, float value)
        {
            if (HasAttribute(type))
                CharacterData.AddAttributeValue(type, value, GetAttributeMax(type));
        }

        // 设置属性值
        public void SetAttribute(AttributeType type, float value)
        {
            if(HasAttribute(type))
                CharacterData.SetAttributeValue(type, value, GetAttributeMax(type));
        }

        // 重置属性值为初始值
        public void ResetAttribute(AttributeType type)
        {
            AttributeData adata = GetAttribute(type);
            if(adata != null)
                CharacterData.SetAttributeValue(type, adata.start_value, GetAttributeMax(type));
        }

        // 获取当前属性值
        public float GetAttributeValue(AttributeType type)
        {
            return CharacterData.GetAttributeValue(type);
        }

        // 获取属性的最大值
        public float GetAttributeMax(AttributeType type)
        {
            AttributeData adata = GetAttribute(type);
            if (adata != null)
                return adata.max_value + GetBonusEffectTotal(BonusEffectData.GetAttributeMaxBonusType(type));
            return 100f; // 默认返回100，如果属性未找到
        }

        // 根据属性类型获取属性数据
        public AttributeData GetAttribute(AttributeType type)
        {
            foreach (AttributeData attr in attributes)
            {
                if (attr.type == type)
                    return attr;
            }
            return null;
        }

        // 检查是否存在指定类型的属性
        public bool HasAttribute(AttributeType type)
        {
            return GetAttribute(type) != null;
        }

        // 增加经验值
        public void GainXP(string id, int xp)
        {
            if (xp > 0)
            {
                CharacterData.GainXP(id, xp);
                CheckLevel(id); // 检查是否升级
            }
        }

        // 检查是否升级
        private void CheckLevel(string id)
        {
            PlayerLevelData ldata = CharacterData.GetLevelData(id);
            LevelData current = LevelData.GetLevel(id, ldata.level);
            LevelData next = LevelData.GetLevel(id, ldata.level + 1);
            if (current != null && next != null && current != next && ldata.xp >= next.xp_required)
            {
                GainLevel(id); // 触发升级
                CheckLevel(id); // 再次检查，可能连续升多级
            }
        }

        // 触发升级
        public void GainLevel(string id)
        {
            CharacterData.GainLevel(id); // 触发升级操作

            int alevel = CharacterData.GetLevel(id);
            LevelData level = LevelData.GetLevel(id, alevel);
            if (level != null)
            {
                foreach (CraftData unlock in level.unlock_craft)
                    character.Crafting.LearnCraft(unlock.id); // 解锁升级后的制作配方
            }

            onGainLevel?.Invoke(); // 触发升级事件
        }

        // 获取当前等级
        public int GetLevel(string id)
        {
            return CharacterData.GetLevel(id);
        }

        // 获取当前经验值
        public int GetXP(string id)
        {
            return CharacterData.GetXP(id);
        }

        // 获取加成效果的总和
        public float GetBonusEffectTotal(BonusType type, GroupData[] targets = null)
        {
            float value = GetBonusEffectTotalSingle(type, null);
            if (targets != null)
            {
                foreach (GroupData target in targets)
                    value += GetBonusEffectTotalSingle(type, target);
            }
            return value;
        }

        // 获取单个加成效果的总和
        public float GetBonusEffectTotalSingle(BonusType type, GroupData target)
        {
            float value = 0f;

            // 等级加成
            value += CharacterData.GetLevelBonusValue(type, target);

            // 装备加成
            foreach (KeyValuePair<int, InventoryItemData> pair in character.EquipData.items)
            {
                ItemData idata = ItemData.Get(pair.Value?.item_id);
                if (idata != null)
                {
                    foreach (BonusEffectData bonus in idata.equip_bonus)
                    {
                        if (bonus.type == type && bonus.target == target)
                            value += bonus.value;
                    }
                }
            }

            // 光环加成
            foreach (BonusAura aura in BonusAura.GetAll())
            {
                float dist = (aura.transform.position - transform.position).magnitude;
                if (aura.effect.type == type && aura.effect.target == target && dist < aura.range)
                    value += aura.effect.value;
            }

            // 时效加成
            if (target == null)
                value += CharacterData.GetTotalTimedBonus(type);

            return value;
        }

        // 获取移动速度倍率
        public float GetSpeedMult()
        {
            return Mathf.Max(move_speed_mult, 0.01f);
        }

        // 获取攻击倍率
        public float GetAttackMult()
        {
            return Mathf.Max(attack_mult, 0.01f);
        }

        // 是否正在因属性耗尽导致生命值下降
        public bool IsDepletingHP()
        {
            return depleting;
        }

        // 获取角色数据
        public PlayerCharacterData CharacterData
        {
            get { return character.SaveData; }
        }

        // 获取角色对象
        public PlayerCharacter GetCharacter()
        {
            return character;
        }
    }
}

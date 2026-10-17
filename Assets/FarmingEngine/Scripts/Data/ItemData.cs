using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{

    public enum ItemType
    {
        Basic = 0,          // 基础物品
        Consumable = 10,    // 消耗品
        Equipment = 20,     // 装备
    }

    public enum WeaponType
    {
        None = 0,           // 无
        WeaponMelee = 10,   // 近战武器
        WeaponRanged = 20,  // 远程武器
    }

    public enum DurabilityType
    {
        None = 0,           // 无耐久度
        UsageCount = 5,     // 使用次数，每次使用减少耐久度，值为使用次数
        UsageTime = 8,      // 使用时间，类似于腐烂，只有在装备时减少，值为游戏小时
        Spoilage = 10,      // 腐烂，随时间减少耐久度，即使不在库存中，值为游戏小时
    }
    /// <summary>
    /// 枚举：标识是哪个部位的装备
    /// </summary>
    public enum EquipSlot
    {
        None = 0,           // 无
        Hand = 10,          // 手部
        Head = 20,          // 头部
        Body = 30,          // 身体
        Feet = 40,          // 脚部
        Backpack = 50,      // 背包
        Accessory = 60,     // 饰品
        Shield = 70,        // 盾牌

        // 通用槽位，根据需求重命名
        Slot8 = 80,         
        Slot9 = 90,         
        Slot10 = 100,       
    }

    public enum EquipSide
    {
        Default = 0,        // 默认
        Right = 2,          // 右手
        Left = 4,           // 左手
    }

    /// <summary>
    /// 物品数据文件 [Z：记录了它的类型、属性、功能和行为规则。而CraftData更像 “制作手册”，记录物品 “怎么造出来]
    /// </summary>

    [CreateAssetMenu(fileName = "ItemData", menuName = "FarmingEngine/ItemData", order = 2)]
    public class ItemData : CraftData
    {
        //分部分来看
        //1、先给物品 “分类贴标签”
        //2、记录物品的 “详细属性”（字段部分）
        [Header("--- 物品数据 ------------------")]
        public ItemType type;                       // 物品类型（Z：标明是基础物品、消耗物品、还是装备）

        [Header("属性")]
        public int inventory_max = 20;              // 库存最大容量
        public DurabilityType durability_type;      // 耐久度类型
        public float durability = 0f;               // 耐久度，0表示无限，消耗品每小时减少1，装备每次使用减少1

        [Header("装备属性")]
        public EquipSlot equip_slot;                // 装备槽位（Z 手、头、等）
        public EquipSide equip_side;                // 装备位置（Z 左手/右手）
        public int armor = 0;                       // 护甲值
        public int bag_size = 0;                    // 背包大小
        public BonusEffectData[] equip_bonus;       // 装备奖励效果

        [Header("武器装备属性")]
        public WeaponType weapon_type;              // 武器类型（无、近战、远程）
        public int damage = 0;                      // 伤害值
        public float range = 1f;                    // 射程
        public float attack_speed = 1f;             // 攻击速度，将乘以动画/起飞/落地时间
        public float attack_cooldown = 1f;          // 每次攻击之间的等待时间（秒）
        public int strike_per_attack = 0;           // 每次攻击的打击次数，最少为1，如果设置为3，则每次攻击将击中3次或射出3个投射物
        public float strike_interval = 0f;          // 单次攻击中每次打击之间的间隔（秒）

        [Header("消耗品属性")]
        public int eat_hp = 0;                      // 恢复生命值
        public int eat_energy = 0;                  // 恢复能量值
        public int eat_hunger = 0;                  // 满足饥饿度
        public int eat_thirst = 0;                  // 满足口渴度
        public int eat_happiness = 0;               // 提升幸福感
        public BonusEffectData[] eat_bonus;         // 消耗品额外效果
        public float eat_bonus_duration = 0f;        // 消耗品额外效果持续时间

        [Header("动作")]
        public SAction[] actions;                   // 动作（Z 这里就是右键格子，会出现的如Equip、Drop操作UI的Action）

        [Header("商店")]
        public int buy_cost = 0;                    // 购买价格
        public int sell_cost = 0;                   // 出售价格

        [Header("关联数据")]
        public ItemData container_data;             // 容器数据
        public PlantData plant_data;                // 植物数据
        public ConstructionData construction_data;  // 建筑数据
        public CharacterData character_data;        // 角色数据
        public GroupData projectile_group;          // 投射物组

        [Header("预制体")]
        public GameObject item_prefab;              // 物品预制体
        public GameObject equipped_prefab;          // 装备后的预制体
        public GameObject projectile_prefab;        // 投射物预制体


        private static List<ItemData> item_data = new List<ItemData>();    // 用于循环的列表
        private static Dictionary<string, ItemData> item_dict = new Dictionary<string, ItemData>();   // 更快的访问

        //Z：判断两个物品能不能合并
        public MAction FindMergeAction(ItemData other)
        {
            if (other == null)
                return null;

            foreach (SAction action in actions)
            {
                if (action != null && action is MAction)
                {
                    MAction maction = (MAction)action;
                    if (maction.merge_target == null || other.HasGroup(maction.merge_target))
                    {
                        return maction;
                    }
                }
            }
            return null;
        }

        public MAction FindMergeAction(Selectable other)
        {
            if (other == null)
                return null;

            foreach (SAction action in actions)
            {
                if (action != null && action is MAction)
                {
                    MAction maction = (MAction)action;
                    if (maction.merge_target == null || other.HasGroup(maction.merge_target))
                    {
                        return maction;
                    }
                }
            }
            return null;
        }

        public AAction FindAutoAction(PlayerCharacter character, ItemSlot islot)
        {
            foreach (SAction action in actions)
            {
                if (action != null && action is AAction)
                {
                    AAction aaction = (AAction)action;
                    if (aaction.CanDoAction(character, islot))
                        return aaction;
                }
            }
            return null;
        }

        //3、给物品 “定义行为”（方法部分）[]

        /// <summary>
        /// Z：获取格子数据中包含的可建造物品（建筑、植物等）的数据
        /// </summary>
        /// <returns></returns>
        public CraftData GetBuildData()
        {
            if (construction_data != null)
                return construction_data;
            else if (plant_data != null)
                return plant_data;
            else if (character_data != null)
                return character_data;
            return null;
        }

        public bool CanBeDropped()
        {
            return item_prefab != null;
        }

        public bool CanBeBuilt()
        {
            return construction_data != null || character_data != null || plant_data != null;
        }
        //Z：是不是武器
        public bool IsWeapon()
        {
            return type == ItemType.Equipment && weapon_type != WeaponType.None;
        }

        public bool IsMeleeWeapon()
        {
            return type == ItemType.Equipment && weapon_type == WeaponType.WeaponMelee;
        }

        public bool IsRangedWeapon()
        {
            return type == ItemType.Equipment && weapon_type == WeaponType.WeaponRanged;
        }

        public bool HasDurability()
        {
            return durability_type != DurabilityType.None && durability >= 0.1f;
        }

        public bool IsBag()
        {
            return type == ItemType.Equipment && bag_size > 0;
        }

        // 从0到100的耐久度百分比（Z：计算当前耐久度百分比）
        public int GetDurabilityPercent(float current_durability)
        {
            float perc = durability > 0.01f ? Mathf.Clamp01(current_durability / durability) : 0f;
            return Mathf.RoundToInt(perc * 100f);
        }

        //Z：静态方法Load()/Get()：加载所有物品数据，方便游戏快速找到某个物品（比如通过 ID “apple” 找到苹果的信息）

        //Z 显式隐藏了从基类继承的同名成员，不需要基类方法是virtual/abstract	
        public static new void Load(string folder = "")
        {
            item_data.Clear();
            item_dict.Clear();
            item_data.AddRange(Resources.LoadAll<ItemData>(folder));

            foreach (ItemData item in item_data)
            {
                if (!item_dict.ContainsKey(item.id))
                    item_dict.Add(item.id, item);
                else
                    Debug.LogError("有两个具有相同ID的物品: " + item.id);
            }
        }

        public new static ItemData Get(string item_id)
        {
            if (item_id != null && item_dict.ContainsKey(item_id))
                return item_dict[item_id];
            return null;
        }

        public new static List<ItemData> GetAll()
        {
            return item_data;
        }
    }

    [System.Serializable]
    public struct ItemDataValue
    {
        public ItemData item;    // 物品
        public int quantity;     // 数量
    }
}

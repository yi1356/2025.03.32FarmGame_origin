using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 制作成本数据
    /// </summary>
    [System.Serializable]
    public class CraftCostData
    {
        public Dictionary<ItemData, int> craft_items = new Dictionary<ItemData, int>(); // 制作所需物品及其数量
        public Dictionary<GroupData, int> craft_fillers = new Dictionary<GroupData, int>(); // 制作填充物（可以是组内任何物品）及其数量
        public Dictionary<CraftData, int> craft_requirements = new Dictionary<CraftData, int>(); // 制作先决条件及其数量
        public GroupData craft_near; // 制作时需要附近的可选物体组（例如：火源、水源）
    }

    /// <summary>
    /// 可制作物品（物品、建筑、植物）的数据类[Z：主要记录制作这个物品需要什么材料和条件，并且封装了一些方便使用的方法]
    /// </summary>
    public class CraftData : IdData
    {
        //Z：感觉很适合分部分来看
        //1、基础信息（卡片上的 “名字和图片”）
        [Header("显示")]
        public string title; // 标题
        public Sprite icon; // 图标
        [TextArea(3, 5)]
        public string desc; // 描述

        //2、分组信息（给物品分类，方便查找）
        [Header("分组")]
        public GroupData[] groups; // 所属分组（Z 这个要重视下）

        //3、制作规则（核心！能不能做、怎么做）
        [Header("制作")]
        public bool craftable; // 可以制作吗？如果为 false，则可以通过学习动作学习
        public int craft_quantity = 1; // 制作数量(Z：就是一次制作几个)
        public float craft_duration = 0f; // 制作所需时间
        public int craft_sort_order = 0; // 制作菜单中显示顺序（Z 好像是为了在建造面板排序用???）

        //4、制作成本（做这个东西需要啥）
        [Header("制作成本")]
        public GroupData craft_near; // 制作时需要附近玩家的可选物体组（例如：火源、水源）（Z 可以理解为环境条件）
        public ItemData[] craft_items; // 制作所需物品
        public GroupData[] craft_fillers; // 制作所需填充物（但可以是该组中的任何物品）（Z 不太理解，但是看了下，应该是用不到的）
        public CraftData[] craft_requirements; // 制作前需要建造的物品

        //5、其他附加信息
        [Header("经验")]//（Z这两个好像也没有看到哪里用到了）
        public int craft_xp = 0; // 制作时获得的经验值
        public string craft_xp_type; // 经验类型

        [Header("特效")]
        public AudioClip craft_sound; // 制作时播放的音效

        protected static List<CraftData> craft_data = new List<CraftData>(); // 制作数据列表(Z 会从resources文件夹中加载，并存储到craft_data中)


        //6、方法（让 “配方卡片” 能被游戏使用的功能）
        //[这些方法是 “配方数据的‘操作接口’” ：让游戏能 “找到配方、判断能不能用、在场景里生成 / 统计对应物品”]
        // 检查是否属于指定分组
        public bool HasGroup(GroupData group)
        {
            foreach (GroupData agroup in groups)
            {
                if (agroup == group)
                    return true;
            }
            return false;
        }

        // 检查是否属于指定分组列表中的任意一个
        public bool HasGroup(GroupData[] mgroups)
        {
            foreach (GroupData mgroup in mgroups)
            {
                foreach (GroupData agroup in groups)
                {
                    if (agroup == mgroup)
                        return true;
                }
            }
            return false;
        }

        // 获取物品数据（如果是物品类型）
        public ItemData GetItem()
        {
            if (this is ItemData)
                return (ItemData)this;
            return null;
        }

        // 获取建筑数据（如果是建筑类型）
        public ConstructionData GetConstruction()
        {
            if (this is ConstructionData)
                return (ConstructionData)this;
            return null;
        }

        // 获取植物数据（如果是植物类型）
        public PlantData GetPlant()
        {
            if (this is PlantData)
                return (PlantData)this;
            return null;
        }

        // 获取角色数据（如果是角色类型）
        public CharacterData GetCharacter()
        {
            if (this is CharacterData)
                return (CharacterData)this;
            return null;
        }

        // 获取制作成本数据
        public CraftCostData GetCraftCost()
        {
            CraftCostData cost = new CraftCostData();
            foreach (ItemData item in craft_items)
            {
                if (!cost.craft_items.ContainsKey(item))
                    cost.craft_items[item] = 1;
                else
                    cost.craft_items[item] += 1;
            }

            foreach (GroupData group in craft_fillers)
            {
                if (!cost.craft_fillers.ContainsKey(group))
                    cost.craft_fillers[group] = 1;
                else
                    cost.craft_fillers[group] += 1;
            }

            foreach (CraftData cdata in craft_requirements)
            {
                if (!cost.craft_requirements.ContainsKey(cdata))
                    cost.craft_requirements[cdata] = 1;
                else
                    cost.craft_requirements[cdata] += 1;
            }

            if (craft_near != null)
                cost.craft_near = craft_near;

            return cost;
        }

        // 加载指定文件夹中的所有数据
        public static void Load(string folder = "")
        {
            craft_data.Clear();
            craft_data.AddRange(Resources.LoadAll<CraftData>(folder));
        }

        // 获取属于指定分组的所有数据
        public static List<CraftData> GetAllInGroup(GroupData group)
        {
            List<CraftData> olist = new List<CraftData>();
            foreach (CraftData item in craft_data)
            {
                if (item.HasGroup(group))
                    olist.Add(item);
            }
            return olist;
        }

        // 获取指定分组中所有可制作的数据
        public static List<CraftData> GetAllCraftableInGroup(PlayerCharacter character, GroupData group)
        {
            List<CraftData> olist = new List<CraftData>();
            foreach (CraftData item in craft_data)
            {
                if (item.craft_quantity > 0 && item.HasGroup(group))
                {
                    bool learnt = item.craftable || character.SaveData.IsIDUnlocked(item.id);
                    if (learnt)
                        olist.Add(item);
                }
            }
            return olist;
        }

        // 根据ID获取数据
        public static CraftData Get(string id)
        {
            foreach (CraftData item in craft_data)
            {
                if (item.id == id)
                    return item;
            }
            return null;
        }

        // 获取所有数据
        public static List<CraftData> GetAll()
        {
            return craft_data;
        }

        // 计算场景中特定类型物体的数量
        public static int CountSceneObjects(CraftData data)
        {
            return Craftable.CountSceneObjects(data); // 场景中的所有对象
        }

        // 计算场景中特定类型物体在指定位置和范围内的数量
        public static int CountSceneObjects(CraftData data, Vector3 pos, float range)
        {
            return Craftable.CountSceneObjects(data, pos, range);
        }

        // 与旧版本兼容
        public static int CountObjectInRadius(CraftData data, Vector3 pos, float radius) { return CountSceneObjects(data, pos, radius); }

        // 返回所有具有指定数据的场景对象
        public static List<GameObject> GetAllObjectsOf(CraftData data)
        {
            return Craftable.GetAllObjectsOf(data);
        }

        // 在指定位置创建对象
        public static GameObject Create(CraftData data, Vector3 pos)
        {
            return Craftable.Create(data, pos);
        }
    }
}

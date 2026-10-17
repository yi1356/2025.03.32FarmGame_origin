using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 物品、建筑、角色、植物的基类
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(UniqueID))]
    public abstract class Craftable : SObject
    {
        private Selectable cselect; // 可选择的组件
        private Destructible cdestruct; // 可破坏的组件
        private Buildable cbuildable; // 可建造的组件

        private static List<Craftable> craftable_list = new List<Craftable>(); // 所有Craftable对象的列表

        protected virtual void Awake()
        {
            craftable_list.Add(this);
            cselect = GetComponent<Selectable>();
            cdestruct = GetComponent<Destructible>();
            cbuildable = GetComponent<Buildable>();
        }

        protected virtual void OnDestroy()
        {
            craftable_list.Remove(this);
        }

        // 根据对象类型获取数据
        public new CraftData GetData()
        {
            if (this is Item)
                return ((Item)this).data;
            if (this is Plant)
                return ((Plant)this).data;
            if (this is Construction)
                return ((Construction)this).data;
            if (this is Character)
                return ((Character)this).data;
            return null;
        }

        // 销毁对象
        public void Destroy()
        {
            Destructible destruct = GetComponent<Destructible>();
            Item item = GetComponent<Item>();
            if (destruct != null)
                destruct.Kill(); // 摧毁可破坏组件以生成掉落并保存数据
            else if (item != null)
                item.DestroyItem(); // 如果是物品，销毁物品
            else if (cselect != null)
                cselect.Destroy(); // 否则，销毁可选择组件
        }

        // 获取可选择组件
        public Selectable Selectable { get { return cselect; } }
        
        // 获取可破坏组件（可能为空）
        public Destructible Destructible { get { return cdestruct; } }

        // 获取可建造组件（可能为空）
        public Buildable Buildable { get { return cbuildable; } }

        //--- 静态函数，便于访问

        // 获取距离最近的Craftable对象
        public static Craftable GetNearest(Vector3 pos, float range = 999f)
        {
            Craftable nearest = null;
            float min_dist = range;
            foreach (Craftable item in craftable_list)
            {
                float dist = (item.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = item;
                }
            }
            return nearest;
        }

        // 获取所有Craftable对象的列表
        public static List<Craftable> GetAll()
        {
            return craftable_list;
        }

        // 计算场景中特定类型对象的数量
        public static int CountSceneObjects(CraftData data)
        {
            return CountSceneObjects(data, Vector3.zero, float.MaxValue); // 场景中所有对象
        }

        // 在指定位置和范围内计算特定类型对象的数量
        public static int CountSceneObjects(CraftData data, Vector3 pos, float range)
        {
            int count = 0;
            if (data is CharacterData)
            {
                count += Character.CountInRange((CharacterData)data, pos, range);
            }
            if (data is PlantData)
            {
                count += Plant.CountInRange((PlantData)data, pos, range);
            }
            if (data is ConstructionData)
            {
                count += Construction.CountInRange((ConstructionData)data, pos, range);
            }
            if (data is ItemData)
            {
                count += Item.CountInRange((ItemData)data, pos, range);
            }
            return count;
        }

        // 兼容旧版本
        public static int CountObjectInRadius(CraftData data, Vector3 pos, float radius) { return CountSceneObjects(data, pos, radius); }

        // 返回所有指定类型对象的GameObject列表
        public static List<GameObject> GetAllObjectsOf(CraftData data)
        {
            List<GameObject> valid_list = new List<GameObject>();
            if (data is ItemData)
            {
                List<Item> items = Item.GetAllOf((ItemData)data);
                foreach (Item item in items)
                    valid_list.Add(item.gameObject);
            }

            if (data is PlantData)
            {
                List<Plant> items = Plant.GetAllOf((PlantData)data);
                foreach (Plant plant in items)
                    valid_list.Add(plant.gameObject);
            }

            if (data is ConstructionData)
            {
                List<Construction> items = Construction.GetAllOf((ConstructionData)data);
                foreach (Construction construct in items)
                    valid_list.Add(construct.gameObject);
            }

            if (data is CharacterData)
            {
                List<Character> items = Character.GetAllOf((CharacterData)data);
                foreach (Character character in items)
                    valid_list.Add(character.gameObject);
            }
            return valid_list;
        }

        // 根据CraftData创建对象
        public static GameObject Create(CraftData data, Vector3 pos)
        {
            if (data == null)
                return null;

            if (data is ItemData)
            {
                ItemData item = (ItemData)data;
                Item obj = Item.Create(item, pos, 1);
                return obj.gameObject;
            }

            if (data is PlantData)
            {
                PlantData item = (PlantData)data;
                Plant obj = Plant.Create(item, pos, -1);
                return obj.gameObject;
            }

            if (data is ConstructionData)
            {
                ConstructionData item = (ConstructionData)data;
                Construction obj = Construction.Create(item, pos);
                return obj.gameObject;
            }

            if (data is CharacterData)
            {
                CharacterData item = (CharacterData)data;
                Character obj = Character.Create(item, pos);
                return obj.gameObject;
            }

            return null;
        }
    }
}

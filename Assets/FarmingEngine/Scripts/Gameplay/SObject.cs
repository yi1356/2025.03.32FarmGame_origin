using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// Craftables或Spawnables的基类，具有通用的静态实用函数(Z：是基础 “物体模板”，即场景中的物体)
    /// </summary>
    public abstract class SObject : MonoBehaviour
    {
        // 获取对象的数据
        public IdData GetData()
        {
            if (this is Spawnable)
                return ((Spawnable)this).data;
            if (this is Craftable)
                return ((Craftable)this).GetData();
            return null;
        }

        // 计算场景中特定数据类型的对象数量
        public static int CountSceneObjects(IdData data)
        {
            return CountSceneObjects(data, Vector3.zero, float.MaxValue); // 统计场景中所有对象
        }

        // 计算场景中特定数据类型的对象数量
        public static int CountSceneObjects(IdData data, Vector3 pos, float range)
        {
            int count = 0;
            if (data is CraftData)
            {
                count += Craftable.CountSceneObjects((CraftData)data, pos, range);
            }
            if (data is SpawnData)
            {
                count += Spawnable.CountInRange((SpawnData)data, pos, range);
            }
            return count;
        }

        // 在存储文件中创建并生成一个新的生成对象，并自动确定其类型（物品、植物或只是生成物体...）
        public static GameObject Create(SData data, Vector3 pos)
        {
            if (data == null)
                return null;

            if (data is CraftData)
            {
                CraftData cdata = (CraftData)data;
                return Craftable.Create(cdata, pos);
            }
            if (data is SpawnData)
            {
                SpawnData spawn_data = (SpawnData)data;
                return Spawnable.Create(spawn_data, pos);
            }
            if (data is LootData)
            {
                LootData loot = (LootData)data;
                if (Random.value <= loot.probability)
                {
                    Item item = Item.Create(loot.item, pos, loot.quantity);
                    return item.gameObject;
                }
            }
            return null;
        }
    }
}

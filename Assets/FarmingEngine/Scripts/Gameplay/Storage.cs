using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(UniqueID))]
    public class Storage : MonoBehaviour
    {
        public int storage_size = 10; // 存储空间大小
        public SData[] starting_items; // 初始物品列表

        private UniqueID unique_id; // 唯一标识

        private static List<Storage> storage_list = new List<Storage>(); // 所有存储对象的列表

        void Awake()
        {
            storage_list.Add(this); // 添加到存储对象列表
            unique_id = GetComponent<UniqueID>(); // 获取唯一标识组件
        }

        private void OnDestroy()
        {
            storage_list.Remove(this); // 从存储对象列表移除
        }

        private void Start()
        {
            // 添加初始物品
            if (!string.IsNullOrEmpty(unique_id.unique_id))
            {
                bool has_inventory = InventoryData.Exists(unique_id.unique_id); // 检查存储数据是否已存在
                if (!has_inventory)
                {
                    InventoryData invdata = InventoryData.Get(InventoryType.Storage, unique_id.unique_id); // 获取存储类型的存储数据
                    foreach (SData data in starting_items)
                    {
                        if (data != null && data is ItemData)
                        {
                            ItemData item = (ItemData)data;
                            invdata.AddItem(item.id, 1, item.durability, UniqueID.GenerateUniqueID()); // 添加物品到存储数据
                        }
                        if (data != null && data is LootData)
                        {
                            LootData loot = (LootData)data;
                            if (Random.value <= loot.probability)
                                invdata.AddItem(loot.item.id, loot.quantity, loot.item.durability, UniqueID.GenerateUniqueID()); // 根据概率添加战利品到存储数据
                        }
                    }
                }
            }
        }

        void Update()
        {
            if (TheGame.Get().IsPaused()) // 如果游戏暂停
                return;
        }

        // 打开存储
        public void OpenStorage(PlayerCharacter player)
        {
            if (!string.IsNullOrEmpty(unique_id.unique_id))
                StoragePanel.Get(player.player_id).ShowStorage(player, unique_id.unique_id, storage_size); // 显示存储界面
            else
                Debug.LogError("You must generate the UID to use the storage feature."); // 错误日志，未生成唯一标识无法使用存储功能
        }

        // 获取最近的存储对象
        public static Storage GetNearest(Vector3 pos, float range = 999f)
        {
            float min_dist = range; // 初始化最小距离为指定范围
            Storage nearest = null; // 最近的存储对象
            foreach (Storage storage in storage_list)
            {
                float dist = (pos - storage.transform.position).magnitude; // 计算与目标位置的距离
                if (dist < min_dist) // 如果距离小于最小距离
                {
                    min_dist = dist; // 更新最小距离
                    nearest = storage; // 更新最近的存储对象
                }
            }
            return nearest; // 返回最近的存储对象
        }

        // 获取所有存储对象列表
        public static List<Storage> GetAll()
        {
            return storage_list; // 返回所有存储对象列表
        }
    }
}

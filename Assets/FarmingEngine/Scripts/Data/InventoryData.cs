using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// Z：标识是哪一种库存数据(背包、装备、箱子)
    /// </summary>
    public enum InventoryType
    {
        None = 0,           // 无
        Inventory = 5,      // 背包
        Equipment = 10,     // 装备
        Storage = 15,       // 存储
        Bag = 20,           // 袋子
    }

    //Z：单个格子的数据
    [System.Serializable]
    public class InventoryItemData
    {
        [Tooltip("这个ID是为了方便从InventoryItemData获取ItemData")]
        public string item_id;       // 物品ID
        public int quantity;         // 数量
        public float durability;     // 耐久度
        public string uid;           // 唯一ID

        public InventoryItemData(string id, int q, float dura, string uid) { item_id = id; quantity = q; durability = dura; this.uid = uid; }
        public ItemData GetItem() { return ItemData.Get(item_id); } // 获取物品数据
    }


    /// <summary>
    /// Z：库存数据类，背包、装备背包、等都是这个类的实例
    /// </summary>
    [System.Serializable]
    public class InventoryData
    {
        public Dictionary<int, InventoryItemData> items;    // 物品字典(Z：对于背包来说，key是格子的索引)
        public InventoryType type;                          // 库存类型
        public string uid;                                  // 唯一ID
        public int size = 99;                               // 大小

        public InventoryData(InventoryType type, string uid)
        {
            this.type = type;
            this.uid = uid;
            items = new Dictionary<int, InventoryItemData>(); // 初始化物品字典
        }

        public void FixData()
        {
            // 修复数据以确保旧保存文件与新游戏版本兼容
            if (items == null)
                items = new Dictionary<int, InventoryItemData>();
        }

        // ---- 物品操作 -----

        // 添加物品（Z：比如从地上拾取物品）
        public int AddItem(string item_id, int quantity, float durability, string uid)
        {
            if (!string.IsNullOrEmpty(item_id) && quantity > 0)
            {
                ItemData idata = ItemData.Get(item_id);
                int max = idata != null ? idata.inventory_max : 999;
                int slot = GetFirstItemSlot(item_id, max - quantity);

                if (slot >= 0)
                {
                    AddItemAt(item_id, slot, quantity, durability, uid);
                }
                return slot;
            }
            return -1;
        }

        // 移除物品
        public void RemoveItem(string item_id, int quantity)
        {
            if (!string.IsNullOrEmpty(item_id) && quantity > 0)
            {
                Dictionary<int, int> remove_list = new Dictionary<int, int>(); // 槽位，数量
                foreach (KeyValuePair<int, InventoryItemData> pair in items)
                {
                    if (pair.Value != null && pair.Value.item_id == item_id && pair.Value.quantity > 0 && quantity > 0)
                    {
                        int remove = Mathf.Min(quantity, pair.Value.quantity);
                        remove_list.Add(pair.Key, remove);
                        quantity -= remove;
                    }
                }

                foreach (KeyValuePair<int, int> pair in remove_list)
                {
                    RemoveItemAt(pair.Key, pair.Value);
                }
            }
        }

        // 在指定槽位添加物品
        public void AddItemAt(string item_id, int slot, int quantity, float durability, string uid)
        {
            if (!string.IsNullOrEmpty(item_id) && slot >= 0 && quantity > 0)
            {
                InventoryItemData invt_slot = GetInventoryItem(slot);
                if (invt_slot != null && invt_slot.item_id == item_id)
                {
                    int amount = invt_slot.quantity + quantity;
                    float durabi = ((invt_slot.durability * invt_slot.quantity) + (durability * quantity)) / (float)amount;
                    items[slot] = new InventoryItemData(item_id, amount, durabi, uid);
                }
                else if (invt_slot == null || invt_slot.quantity <= 0)
                {
                    items[slot] = new InventoryItemData(item_id, quantity, durability, uid);
                }
            }
        }

        // 移除指定槽位的物品
        public void RemoveItemAt(int slot, int quantity)
        {
            if (slot >= 0 && quantity >= 0)
            {
                InventoryItemData invt_slot = GetInventoryItem(slot);
                if (invt_slot != null && invt_slot.quantity > 0)
                {
                    int amount = invt_slot.quantity - quantity;
                    if (amount <= 0)
                        items.Remove(slot);
                    else
                        items[slot] = new InventoryItemData(invt_slot.item_id, amount, invt_slot.durability, invt_slot.uid);
                }
            }
        }

        // 交换两个槽位的物品
        public void SwapItemSlots(int slot1, int slot2)
        {
            InventoryItemData invt_slot1 = GetInventoryItem(slot1);
            InventoryItemData invt_slot2 = GetInventoryItem(slot2);
            items[slot1] = invt_slot2;
            items[slot2] = invt_slot1;

            if (invt_slot2 == null)
                items.Remove(slot1);
            if (invt_slot1 == null)
                items.Remove(slot2);
        }

        // 增加物品耐久度
        public void AddItemDurability(int slot, float value)
        {
            if (items.ContainsKey(slot))
            {
                InventoryItemData invdata = items[slot];
                invdata.durability += value;
            }
        }

        // 更新所有物品的耐久度
        public void UpdateAllDurability(float game_hours)
        {
            List<int> remove_items = new List<int>();

            foreach (KeyValuePair<int, InventoryItemData> pair in items)
            {
                InventoryItemData invdata = pair.Value;
                ItemData idata = ItemData.Get(invdata?.item_id);

                if (idata != null && invdata != null)
                {
                    if (idata.durability_type == DurabilityType.Spoilage)
                        invdata.durability -= game_hours;
                    if (idata.durability_type == DurabilityType.UsageTime && type == InventoryType.Equipment)
                        invdata.durability -= game_hours;
                }

                if (idata != null && invdata != null && idata.HasDurability() && invdata.durability <= 0f)
                    remove_items.Add(pair.Key);
            }

            foreach (int slot in remove_items)
            {
                InventoryItemData invdata = GetInventoryItem(slot);
                ItemData idata = ItemData.Get(invdata?.item_id);
                RemoveItemAt(slot, invdata.quantity);
                if (idata.container_data)
                    AddItemAt(idata.container_data.id, slot, invdata.quantity, idata.container_data.durability, UniqueID.GenerateUniqueID());
            }
            remove_items.Clear();
        }

        // 移除所有物品
        public void RemoveAll()
        {
            items.Clear();
        }

        // ----- 查询物品 ------

        // 是否拥有指定物品
        public bool HasItem(string item_id, int quantity = 1)
        {
            return CountItem(item_id) >= quantity;
        }

        // 指定槽位是否有物品
        public bool HasItemIn(int slot)
        {
            return items.ContainsKey(slot) && items[slot].quantity > 0;
        }

        // 指定槽位是否有指定物品
        public bool IsItemIn(string item_id, int slot)
        {
            return items.ContainsKey(slot) && items[slot].item_id == item_id && items[slot].quantity > 0;
        }

        // 统计指定物品的总数量
        public int CountItem(string item_id)
        {
            int value = 0;
            foreach (KeyValuePair<int, InventoryItemData> pair in items)
            {
                if (pair.Value != null && pair.Value.item_id == item_id)
                    value += pair.Value.quantity;
            }
            return value;
        }

        // 是否有空槽位
        public bool HasEmptySlot()
        {
            return GetFirstEmptySlot() >= 0;
        }

        // 获取第一个空槽位
        public int GetFirstEmptySlot()
        {
            for (int i = 0; i < size; i++)
            {
                InventoryItemData invdata = GetInventoryItem(i);
                if (invdata == null || invdata.quantity <= 0)
                    return i;
            }
            return -1;
        }

        // 获取第一个指定物品的槽位
        public int GetFirstItemSlot(string item_id, int slot_max)
        {
            foreach (KeyValuePair<int, InventoryItemData> pair in items)
            {
                if (pair.Value != null && pair.Value.item_id == item_id && pair.Value.quantity <= slot_max)
                    return pair.Key;
            }
            return GetFirstEmptySlot();
        }

        // 是否有指定组中的物品
        public bool HasItemInGroup(GroupData group, int quantity = 1)
        {
            return CountItemInGroup(group) >= quantity;
        }

        // 统计指定组中物品的总数量
        public int CountItemInGroup(GroupData group)
        {
            int value = 0;
            foreach (KeyValuePair<int, InventoryItemData> pair in items)
            {
                if (pair.Value != null)
                {
                    ItemData idata = ItemData.Get(pair.Value.item_id);
                    if (idata != null && idata.HasGroup(group))
                        value += pair.Value.quantity;
                }
            }
            return value;
        }

        // 获取指定组中的第一个物品
        public InventoryItemData GetFirstItemInGroup(GroupData group)
        {
            foreach (KeyValuePair<int, InventoryItemData> pair in items)
            {
                if (pair.Value != null)
                {
                    ItemData idata = ItemData.Get(pair.Value.item_id);
                    if (idata != null && pair.Value.quantity > 0)
                    {
                        if (idata.HasGroup(group))
                            return pair.Value;
                    }
                }
            }
            return null;
        }

        // 获取指定槽位的物品数据
        public InventoryItemData GetInventoryItem(int slot)
        {
            if (items.ContainsKey(slot))
                return items[slot];
            return null;
        }

        // 获取指定槽位的物品
        public ItemData GetItem(int slot)
        {
            InventoryItemData idata = GetInventoryItem(slot);
            return idata?.GetItem();
        }

        // 是否可以拿取指定数量的物品
        public bool CanTakeItem(string item_id, int quantity)
        {
            ItemData idata = ItemData.Get(item_id);
            int max = idata != null ? idata.inventory_max : 999;
            int slot = GetFirstItemSlot(item_id, max - quantity);
            return slot >= 0;
        }

        // ----- 装备物品操作 ------

        // 装备物品
        public void EquipItem(EquipSlot equip_slot, string item_id, float durability, string uid)
        {
            int eslot = (int)equip_slot;
            InventoryItemData idata = new InventoryItemData(item_id, 1, durability, uid);
            items[eslot] = idata;
        }

        // 卸下物品
        public void UnequipItem(EquipSlot equip_slot)
        {
            int eslot = (int)equip_slot;
            if (items.ContainsKey(eslot))
                items.Remove(eslot);
        }

        // 是否装备了指定槽位的物品
        public bool HasEquippedItem(EquipSlot equip_slot)
        {
            return GetEquippedItem(equip_slot) != null;
        }

        // 获取已装备的第一个武器
        public InventoryItemData GetEquippedWeapon()
        {
            foreach (KeyValuePair<int, InventoryItemData> item in items)
            {
                ItemData idata = ItemData.Get(item.Value.item_id);
                if (idata && idata.IsWeapon())
                    return item.Value;
            }
            return null;
        }

        // 获取已装备的第一个武器槽位
        public EquipSlot GetEquippedWeaponSlot()
        {
            foreach (KeyValuePair<int, InventoryItemData> item in items)
            {
                ItemData idata = ItemData.Get(item.Value.item_id);
                if (idata && idata.IsWeapon())
                    return (EquipSlot)item.Key;
            }
            return EquipSlot.None;
        }

        // 获取已装备的武器数据
        public ItemData GetEquippedWeaponData()
        {
            InventoryItemData idata = GetEquippedWeapon();
            return idata?.GetItem();
        }

        // 获取已装备的指定槽位的物品数据
        public InventoryItemData GetEquippedItem(EquipSlot equip_slot)
        {
            int slot = (int)equip_slot;
            if (items.ContainsKey(slot))
                return items[slot];
            return null;
        }

        // 获取已装备的指定槽位的物品数据
        public ItemData GetEquippedItemData(EquipSlot equip_slot)
        {
            InventoryItemData idata = GetEquippedItem(equip_slot);
            return idata?.GetItem();
        }

        // 获取指定类型和唯一ID的库存数据
        public static InventoryData Get(InventoryType type, string uid)
        {
            return PlayerData.Get().GetInventory(type, uid);
        }

        // 获取指定类型和玩家ID的库存数据
        public static InventoryData Get(InventoryType type, int player_id)
        {
            return PlayerData.Get().GetInventory(type, player_id);
        }

        // 获取指定类型和玩家ID的装备库存数据
        public static InventoryData GetEquip(InventoryType type, int player_id)
        {
            return PlayerData.Get().GetEquipInventory(type, player_id);
        }

        // 检查是否存在指定唯一ID的库存
        public static bool Exists(string uid)
        {
            return PlayerData.Get().HasInventory(uid);
        }
    }

    // 库存槽位类
    [System.Serializable]
    public class InventorySlot
    {
        public InventoryData inventory;     // 库存数据
        public int slot;                    // 槽位

        public InventorySlot() { }
        public InventorySlot(InventoryData inv, int s) { inventory = inv; slot = s; }

        // 获取槽位中的库存物品数据
        public InventoryItemData GetInventoryItem()
        {
            return inventory.GetInventoryItem(slot);
        }

        // 获取槽位中的物品数据
        public ItemData GetItem()
        {
            return inventory.GetItem(slot);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 将物品堆分割成两堆的动作
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Split", order = 50)]
    public class ActionSplit : SAction
    {
        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            int half = slot.GetQuantity() / 2; // 计算堆的一半数量
            ItemData item = slot.GetItem(); // 获取物品数据
            InventoryData inventory = slot.GetInventory(); // 获取物品所在的背包数据
            InventoryItemData item_data = inventory.GetInventoryItem(slot.index); // 获取物品在背包中的数据

            inventory.RemoveItemAt(slot.index, half); // 从原始堆中移除一半数量的物品

            bool can_take = inventory.CanTakeItem(item.id, half); // 检查背包是否能够接受这一半数量的物品
            InventoryData ninventory = can_take ? inventory : character.Inventory.GetValidInventory(item, half); // 如果不能接受，找一个有效的背包
            int new_slot = ninventory.GetFirstEmptySlot(); // 获取新背包中的第一个空槽位
            ninventory.AddItemAt(item.id, new_slot, half, item_data.durability, UniqueID.GenerateUniqueID()); // 将一半数量的物品放入新的背包槽位中
        }

        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            ItemData item = slot.GetItem(); // 获取物品数据
            InventoryData inventory = slot.GetInventory(); // 获取物品所在的背包数据
            return item != null && inventory != null && slot.GetQuantity() > 1 && inventory.HasEmptySlot(); // 检查是否满足分割物品堆的条件：有物品、有背包、数量大于1、有空槽位
        }
    }

}
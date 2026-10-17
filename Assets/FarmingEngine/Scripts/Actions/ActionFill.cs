using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 填充一个罐子（或其他容器）的水
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Fill", order = 50)]
    public class ActionFill : MAction
    {
        public ItemData filled_item; // 填充后的物品数据（Z：其实就是水壶数据）

        // 执行合并操作
        public override void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            if (select.HasGroup(merge_target)) // 如果选择对象拥有指定的合并目标组
            {
                InventoryData inventory = slot.GetInventory(); // 获取物品槽对应的物品栏数据
                inventory.RemoveItemAt(slot.index, 1); // 移除物品槽中的物品
                character.Inventory.GainItem(inventory, filled_item, 1); // 角色获得填充后的物品
            }
        }

    }

}
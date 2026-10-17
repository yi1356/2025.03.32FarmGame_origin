using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 添加燃料到火堆（木材、草等）
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/AddFuel", order = 50)]
    public class ActionAddFuel : MAction
    {
        public float range = 2f; // 操作范围

        // 合并操作
        public override void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            Firepit fire = select.GetComponent<Firepit>(); // 获取火堆组件
            InventoryData inventory = slot.GetInventory(); // 获取物品栏数据
            if (fire != null && slot.GetItem() && inventory.HasItem(slot.GetItem().id))
            {
                fire.AddFuel(fire.wood_add_fuel); // 向火堆添加燃料
                inventory.RemoveItemAt(slot.index, 1); // 从物品栏移除一个物品
            }
        }

    }

}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 从物品提供者处用物品填充一个罐子（或其他容器）
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/FillProvider", order = 50)]
    public class ActionFillProvider : MAction
    {
        public ItemData filled_item; // 填充后的物品数据

        // 执行合并操作
        public override void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            if (select.HasGroup(merge_target)) // 如果选择对象拥有指定的合并目标组
            {
                ItemProvider provider = select.GetComponent<ItemProvider>(); // 获取选择对象的物品提供者组件
                InventoryData inventory = slot.GetInventory(); // 获取物品槽对应的物品栏数据

                provider.RemoveItem(); // 移除物品提供者中的物品
                provider.PlayTakeSound(); // 播放物品获取音效
                inventory.RemoveItemAt(slot.index, 1); // 移除物品槽中的物品
                character.Inventory.GainItem(inventory, filled_item, 1); // 角色获得填充后的物品
            }
        }

        // 判断是否可以进行操作的条件方法
        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            ItemProvider provider = select != null ? select.GetComponent<ItemProvider>() : null; // 获取选择对象的物品提供者组件
            return provider != null && provider.HasItem(); // 只有当物品提供者存在且有物品时才能进行操作
        }
    }

}
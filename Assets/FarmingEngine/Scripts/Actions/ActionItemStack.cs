using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 将物品添加到堆叠容器中
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/ItemStack", order = 50)]
    public class ActionItemStack : MAction
    {
        // 合并操作
        public override void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            InventoryData inventory = slot.GetInventory(); // 获取物品槽的库存数据
            InventoryItemData iidata = inventory.GetInventoryItem(slot.index); // 获取物品槽中的物品数据
            inventory.RemoveItemAt(slot.index, iidata.quantity); // 从库存中移除物品

            ItemStack stack = select.GetComponent<ItemStack>(); // 获取堆叠容器组件
            stack.AddItem(iidata.quantity); // 向堆叠容器中添加物品
        }

        // 判断是否可以执行操作的条件方法
        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            ItemStack stack = select.GetComponent<ItemStack>(); // 获取堆叠容器组件
            return stack != null && stack.item != null && stack.item.id == slot.GetItem().id && stack.GetItemCount() < stack.item_max; // 检查堆叠容器是否存在，并且物品ID匹配且未达到堆叠上限
        }
    }

}
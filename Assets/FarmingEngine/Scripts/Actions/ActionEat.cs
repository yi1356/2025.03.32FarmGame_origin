using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 吃掉物品的操作
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Eat", order = 50)]
    public class ActionEat : SAction
    {
        // 执行吃掉物品操作
        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            InventoryData inventory = slot.GetInventory(); // 获取物品槽对应的物品栏数据
            character.Inventory.EatItem(inventory, slot.index); // 角色吃掉物品
        }

        // 判断是否可以进行吃掉物品操作的条件方法
        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            ItemData item = slot.GetItem(); // 获取物品槽中的物品数据
            return item != null && item.type == ItemType.Consumable; // 只有当物品存在且类型为可消耗品时才能执行吃掉操作
        }
    }

}
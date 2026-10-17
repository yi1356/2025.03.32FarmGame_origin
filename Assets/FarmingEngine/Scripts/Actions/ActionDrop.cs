using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 丢弃物品的操作
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Drop", order = 50)]
    public class ActionDrop : SAction
    {
        // 执行丢弃物品操作
        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            InventoryData inventory = slot.GetInventory(); // 获取物品槽对应的物品栏数据
            character.Inventory.DropItem(inventory, slot.index); // 角色丢弃物品
        }
    }

}
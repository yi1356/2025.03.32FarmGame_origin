using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 使用另一个物品切割物品的操作（例如用斧头开椰子）
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Cut", order = 50)]
    public class ActionCut : MAction
    {
        public ItemData cut_item; // 切割后获得的物品数据

        // 执行切割操作
        public override void DoAction(PlayerCharacter character, ItemSlot slot1, ItemSlot slot2)
        {
            InventoryData inventory = slot1.GetInventory(); // 获取第一个物品槽的物品栏数据
            inventory.RemoveItemAt(slot1.index, 1); // 从物品栏移除第一个物品槽中的一个物品
            character.Inventory.GainItem(cut_item, 1); // 角色获得切割后的物品
        }
    }

}
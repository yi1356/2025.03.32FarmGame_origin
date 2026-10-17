using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 在地面上播种种子。
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Plant", order = 50)]
    public class ActionPlant : SAction
    {
        //TODO:2025.07.18
        // 执行播种动作的方法
        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            ItemData item = slot.GetItem(); // 获取插槽中的物品数据
            InventoryData inventory = slot.GetInventory(); // 获取插槽所在的背包数据
            if (item != null)
            {
                character.Crafting.BuildItemBuildMode(inventory, slot.index); // 调用角色的建造物品建造模式方法，用于播种
            }
        }

        // 判断是否可以执行播种动作的条件方法
        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            ItemData item = slot.GetItem(); // 获取插槽中的物品数据
            return item != null && item.plant_data != null; // 物品不为空且有种植数据时才能进行播种
        }
    }

}
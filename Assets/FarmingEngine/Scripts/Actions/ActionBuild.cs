using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 将物品建造到建筑物中（陷阱、诱饵等）
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Build", order = 50)]
    public class ActionBuild : SAction
    {
        // 执行建造操作
        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            ItemData item = slot.GetItem(); // 获取物品数据
            InventoryData inventory = slot.GetInventory(); // 获取物品栏数据
            if (item != null)
            {
                character.Crafting.BuildItemBuildMode(inventory, slot.index); // 调用角色的建造物品方法
            }
        }

        // 判断是否可以进行建造操作的条件方法
        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            ItemData item = slot.GetItem(); // 获取物品数据
            return item != null; // 只要物品存在，即可进行建造操作
        }
    }

}
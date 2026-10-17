using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 打开一个包裹，里面含有更多物品（例如礼物、盒子）
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Open", order = 50)]
    public class ActionOpen : SAction
    {
        public SData[] items; // 包裹中包含的物品数组

        // 执行打开包裹操作的方法
        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            InventoryData inventory = slot.GetInventory(); // 获取物品槽的库存数据
            inventory.RemoveItemAt(slot.index, 1); // 从库存中移除打开的包裹物品

            // 遍历包裹中的物品数组
            foreach (SData item in items)
            {
                if (item != null)
                {
                    // 如果是单个物品数据
                    if (item is ItemData)
                    {
                        ItemData iitem = (ItemData)item;
                        character.Inventory.GainItem(iitem, 1); // 将物品添加到角色的库存中
                    }

                    // 如果是掉落数据
                    if (item is LootData)
                    {
                        LootData loot = (LootData)item;
                        // 根据掉落概率随机确定是否获得该物品
                        if (Random.value <= loot.probability)
                        {
                            character.Inventory.GainItem(loot.item, loot.quantity); // 将掉落物品添加到角色的库存中
                        }
                    }
                }
            }
        }

        // 判断是否可以执行打开包裹操作的条件方法
        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            return true; // 可以随时执行打开包裹操作
        }
    }
}
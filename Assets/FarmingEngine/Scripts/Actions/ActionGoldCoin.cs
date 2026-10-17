using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 使用金币进行操作
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/GoldCoin", order = 50)]
    public class ActionGoldCoin : AAction
    {
        // 执行操作
        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            InventoryData inventory = slot.GetInventory(); // 获取物品槽对应的物品栏数据
            int amount = slot.GetQuantity(); // 获取物品数量
            inventory.RemoveItemAt(slot.index, amount); // 移除物品
            character.SaveData.gold += amount; // 增加角色金币数量
            ItemTakeFX.DoCoinTakeFX(character.transform.position, slot.GetItem(), character.player_id); // 播放金币收集特效
        }

        // 执行选择操作
        public override void DoSelectAction(PlayerCharacter character, ItemSlot slot)
        {
            InventoryData inventory = slot.GetInventory(); // 获取物品槽对应的物品栏数据
            int amount = slot.GetQuantity(); // 获取物品数量
            inventory.RemoveItemAt(slot.index, amount); // 移除物品
            character.SaveData.gold += amount; // 增加角色金币数量
            ItemTakeFX.DoCoinTakeFX(character.transform.position, slot.GetItem(), character.player_id); // 播放金币收集特效
        }

        // 判断是否可以执行操作的条件方法
        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            return true; // 永远返回 true，表示可以执行操作
        }
    }

}
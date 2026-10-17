using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 加速金币收集动作，避免它出现在物品栏中(Z 好像是原本本要手机到背包中，但又处理了下，使其在收集到背包后立刻被加到金币数中)
    /// </summary>
    [RequireComponent(typeof(Item))]
    public class GoldCoin : MonoBehaviour
    {
        private Item item; // 物品组件

        void Awake()
        {
            item = GetComponent<Item>(); // 获取物品组件
            item.onTake += OnTake; // 注册物品被收集事件
        }

        /// <summary>
        /// 当物品被收集时的处理方法
        /// </summary>
        void OnTake()
        {
            PlayerCharacter character = PlayerCharacter.GetNearest(transform.position); // 获取最近的玩家角色
            if (character != null)
            {
                // 从玩家角色的物品栏中移除金币并添加到金币总量中
                character.Inventory.RemoveItem(item.data, item.quantity);
                character.SaveData.gold += item.quantity;
                // 播放金币收集特效
                ItemTakeFX.DoCoinTakeFX(character.transform.position, item.data, character.player_id);
            }
        }
    }
}
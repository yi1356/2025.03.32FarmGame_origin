using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 学习一个制作配方
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Learn", order = 50)]
    public class ActionLearn : SAction
    {
        public AudioClip learn_audio; // 学习时播放的音频
        public bool destroy_on_learn = true; // 学习后是否销毁物品
        public CraftData[] learn_list; // 要学习的制作数据列表

        // 执行学习操作的方法
        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            foreach (CraftData data in learn_list)
            {
                character.Crafting.LearnCraft(data.id); // 调用角色的制作系统学习制作配方
            }

            TheAudio.Get().PlaySFX("learn", learn_audio); // 播放学习音效

            InventoryData inventory = slot.GetInventory(); // 获取物品槽的库存数据
            if (destroy_on_learn)
                inventory.RemoveItemAt(slot.index, 1); // 如果设定要销毁物品，则从库存中移除学习的物品

            CraftSubPanel.Get(character.player_id)?.RefreshCraftPanel(); // 刷新角色的制作面板
        }

        // 判断是否可以执行学习操作的条件方法
        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            foreach (CraftData data in learn_list)
            {
                if (!character.Crafting.HasLearnt(data.id)) // 遍历学习列表，如果有任何一个配方尚未学习，则返回true
                    return true;
            }
            return false; // 所有配方都已学习，返回false
        }
    }

}
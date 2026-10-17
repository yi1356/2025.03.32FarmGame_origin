using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 使用修理工具修理建筑物/物品的动作。
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Repair", order = 50)]
    public class ActionRepair : MAction
    {
        public float duration = 0.5f; // 修理持续时间，以游戏小时为单位

        // 修理物品
        public override void DoAction(PlayerCharacter character, ItemSlot slot, ItemSlot slot_other)
        {
            string anim = character.Animation ? character.Animation.use_anim : ""; // 获取角色的使用动画
            character.TriggerAnim(anim, character.transform.position); // 触发角色使用动画
            character.TriggerProgressBusy(duration, () =>
            {
                InventoryItemData repair = slot.GetInventoryItem(); // 获取修理工具的物品数据
                InventoryItemData titem = slot_other.GetInventoryItem(); // 获取目标物品的物品数据
                if (repair != null && titem != null)
                {
                    ItemData targetItem = ItemData.Get(titem.item_id); // 获取目标物品的数据
                    titem.durability = targetItem.durability; // 重置目标物品的耐久度为初始值
                    repair.durability -= 1f; // 修理工具的耐久度减少
                }
            });
        }

        // 修理建筑物
        public override void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            string anim = character.Animation ? character.Animation.use_anim : ""; // 获取角色的使用动画
            character.TriggerAnim(anim, select.transform.position); // 触发角色使用动画
            character.TriggerProgressBusy(duration, () =>
            {
                InventoryItemData repair = slot.GetInventoryItem(); // 获取修理工具的物品数据
                Destructible target = select.Destructible; // 获取可破坏物体组件
                if (repair != null && target != null)
                {
                    target.hp = target.GetMaxHP(); // 将目标建筑物的生命值恢复到最大值
                    repair.durability -= 1f; // 修理工具的耐久度减少
                }
            });
        }

        // 判断是否可以修理物品的条件方法
        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot, ItemSlot slot_other)
        {
            ItemData item = slot_other.GetItem(); // 获取目标物品的数据
            if (item == null) return false; // 如果目标物品为空，则返回false
            bool target_valid = merge_target == null || item.HasGroup(merge_target); // 判断目标物品是否符合合并目标的条件
            bool durability_valid = item.durability_type == DurabilityType.UsageCount || item.durability_type == DurabilityType.UsageTime; // 判断目标物品的耐久度类型是否符合修理条件
            return durability_valid && target_valid; // 返回是否可以修理的结果
        }

        // 判断是否可以修理建筑物的条件方法
        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            if (select == null) return false; // 如果选择的目标为空，则返回false
            bool target_valid = merge_target == null || select.HasGroup(merge_target); // 判断选择的目标是否符合合并目标的条件
            bool destruct_valid = select.Destructible != null && select.Destructible.target_team == AttackTeam.Ally; // 判断选择的目标是否可破坏，并且属于友方
            return target_valid && destruct_valid; // 返回是否可以修理的结果
        }
    }

}

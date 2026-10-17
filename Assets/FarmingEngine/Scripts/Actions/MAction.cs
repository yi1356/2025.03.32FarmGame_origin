using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{

    /// <summary>
    /// 合并动作的父类：任何在混合两个物品时发生的动作（例如：椰子和斧头），或者一个物品与可选择对象混合时发生的动作（例如：原始食物放在火上）
    /// </summary>
    public abstract class MAction : SAction
    {
        public GroupData merge_target; // 合并的目标物品组数据

        // 当对一个物品数据执行动作时作用于另一个物品数据时（例如：切割椰子），slot 是具有动作的槽位，slot_other 是没有动作的槽位
        public virtual void DoAction(PlayerCharacter character, ItemSlot slot, ItemSlot slot_other)
        {

        }

        // 当对一个物品数据执行动作时作用于可选择对象时（例如：烹饪肉类）[Z：这个感觉应该是 背包中物品 与 场景中物品交互 产生结果]
        public virtual void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {

        }

        // 在合并物品-物品时的条件，如果需要添加新条件，则重写此方法
        public virtual bool CanDoAction(PlayerCharacter character, ItemSlot slot, ItemSlot slot_target) // slot_target 是没有动作的槽位
        {
            ItemData item = slot_target.GetItem();
            if (item == null) return false;
            return merge_target == null || item.HasGroup(merge_target);
        }

        // 在合并物品-可选择对象时的条件，如果需要添加新条件，则重写此方法
        public virtual bool CanDoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            if (select == null) return false;
            return merge_target == null || select.HasGroup(merge_target);
        }


        //---- 重写基础动作，以便能够像常规动作一样使用合并动作

        // 执行动作使用最近的可选择对象
        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            Selectable select = Selectable.GetNearestGroup(merge_target, character.transform.position);
            if (select != null)
            {
                DoAction(character, slot, select);
            }
        }

        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            Selectable select = Selectable.GetNearestGroup(merge_target, character.transform.position);
            if (select != null && select.IsInUseRange(character))
            {
                return CanDoAction(character, slot, select);
            }
            return false;
        }

    }

}

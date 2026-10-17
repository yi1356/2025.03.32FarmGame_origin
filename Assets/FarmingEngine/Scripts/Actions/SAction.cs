using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{

    /// <summary>
    /// 可选择对象动作的父类：通过动作选择器手动选择的任何动作（物品或可选择对象）
    /// </summary>

    public abstract class SAction : ScriptableObject
    {
        public string title; // 动作的标题

        // 当在场景中对可选择对象使用动作时调用
        public virtual void DoAction(PlayerCharacter character, Selectable select)
        {

        }

        // 当在物品数据的库存中使用动作时调用（装备/食用等）
        public virtual void DoAction(PlayerCharacter character, ItemSlot slot)
        {

        }

        // 检查是否可以执行动作的条件，如果需要添加条件，请重写此方法
        public virtual bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            return true; // 没有特定条件
        }

        // 检查是否可以执行动作的条件，如果需要添加条件，请重写此方法
        public virtual bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            return true; // 没有特定条件
        }

        // 判断动作是否是自动执行的动作（继承自 AAction 类）
        public bool IsAuto()
        {
            return (this is AAction);
        }

        // 判断动作是否是合并动作（继承自 MAction 类）
        public bool IsMerge()
        {
            return (this is MAction);
        }

    }

}
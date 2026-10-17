using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 自动操作的父类：当点击对象时自动执行的任何操作
    /// 如果有多个操作可供选择，将选择列表中的第一个 AAction 来执行
    /// </summary>
    
    public abstract class AAction : SAction
    {
        // 当在场景中点击一个 Selectable 对象时
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            // 这个方法预期会被子类覆写
        }

        // 当在物品栏中右键点击（或按下使用键）一个 ItemData 时
        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            // 这个方法预期会被子类覆写
        }

        // 当在物品栏中左键点击（或选中）一个 ItemData 时(Z:没看懂这里是干什么的)
        public virtual void DoSelectAction(PlayerCharacter character, ItemSlot slot)
        {
            // 这个方法可选择被子类覆写
        }

        // 判断是否可以执行可选择对象的操作的条件方法
        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            return true; // 没有特定的条件
        }

        // 判断是否可以执行物品操作的条件方法
        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            return true; // 没有特定的条件
        }
    }
}
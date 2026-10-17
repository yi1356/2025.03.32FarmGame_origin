using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 重新激活之前触发过的陷阱
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/ActivateTrap", order = 50)]
    public class ActionSetTrap : SAction
    {
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            Trap trap = select.GetComponent<Trap>(); // 获取选择对象上的陷阱组件
            if (trap != null)
                trap.Activate(); // 激活陷阱
        }

        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            Trap trap = select.GetComponent<Trap>(); // 获取选择对象上的陷阱组件
            if (trap != null)
                return !trap.IsActive(); // 只有当陷阱未激活时才能执行该动作
            return false; // 如果没有陷阱组件或者陷阱已经激活，则不能执行该动作
        }
    }
}
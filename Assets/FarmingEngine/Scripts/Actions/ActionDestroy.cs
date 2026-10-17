using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 直接摧毁可破坏对象的操作
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Destroy", order = 50)]
    public class ActionDestroy : AAction
    {
        public string animation; // 操作时播放的动画名称

        // 执行摧毁操作
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            select.Destructible.KillIn(0.5f); // 在0.5秒内摧毁可破坏对象
            character.TriggerAnim(animation, select.transform.position); // 触发角色播放动画，并传入选择对象的位置
            character.TriggerBusy(0.5f); // 角色忙碌0.5秒
        }

        // 判断是否可以进行摧毁操作的条件方法
        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            return select.Destructible != null && !select.Destructible.IsDead(); // 只有当选择对象具有可破坏组件并且未被摧毁时才能执行操作
        }
    }

}
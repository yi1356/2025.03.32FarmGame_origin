using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 睡觉动作
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Sleep", order = 50)]
    public class ActionSleep : SAction
    {
        public float sleep_hp_hour; // 每小时睡觉恢复的生命值
        public float sleep_energy_hour; // 每小时睡觉恢复的能量值
        public float sleep_hunger_hour; // 每小时睡觉消耗的饥饿值
        public float sleep_happiness_hour; // 每小时睡觉增加的幸福值
        public float sleep_speed_mult = 8f; // 睡觉时的速度倍率

        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            //TODO：测试睡觉行为
            Debug.Log("测试睡觉行为");
            Construction construct = select.GetComponent<Construction>(); // 获取选择对象上的建筑组件
            if (construct != null)
                character.Sleep(this); // 让角色执行睡觉动作，并传递睡觉相关的参数
        }
    }
}
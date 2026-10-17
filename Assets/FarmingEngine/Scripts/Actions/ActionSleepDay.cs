using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 睡觉直到第二天的动作
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/SleepDay", order = 50)]
    public class ActionSleepDay : SAction
    {
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            TheGame.Get().TransitionToNextDay(); // 调用游戏管理器的过渡到下一天的方法
        }
    }
}
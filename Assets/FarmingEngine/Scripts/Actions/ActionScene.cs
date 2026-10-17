using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 当使用时改变场景
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/ChangeScene", order = 50)]
    public class ActionScene : AAction
    {
        public string scene; // 要切换到的场景名称
        public int entry_index; // 场景中的入口索引

        public override void DoAction(PlayerCharacter character, Selectable selectable)
        {
            TheGame.Get().TransitionToScene(scene, entry_index); // 调用游戏管理器的切换场景方法
        }

        public override bool CanDoAction(PlayerCharacter character, Selectable selectable)
        {
            return true; // 可以随时执行该动作
        }
    }
}
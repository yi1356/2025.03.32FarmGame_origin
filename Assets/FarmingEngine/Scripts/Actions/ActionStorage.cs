using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 用于将物品存储在建筑（如箱子）内的动作
    /// 注意！如果选择对象上没有设置唯一标识符（UniqueID），这个动作将无法使用
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Storage", order = 50)]
    public class ActionStorage : AAction
    {
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            Storage storage = select.GetComponent<Storage>(); // 获取选择对象上的存储（Storage）组件
            if (storage != null)
            {
                storage.OpenStorage(character); // 打开存储界面，让玩家与存储交互
            }
        }

        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            return select.GetComponent<Storage>() != null; // 如果选择对象有存储（Storage）组件，则可以执行该动作
        }
    }
}
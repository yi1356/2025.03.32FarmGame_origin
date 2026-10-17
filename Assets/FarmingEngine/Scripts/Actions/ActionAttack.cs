using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 攻击可破坏物体的操作
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Attack", order = 50)]
    public class ActionAttack : SAction
    {
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            if (select.Destructible)
            {
                character.Attack(select.Destructible);
            }
        }
    }

}
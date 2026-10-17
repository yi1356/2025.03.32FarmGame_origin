using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 骑乘动作，用于骑乘可骑乘的动物
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Ride", order = 50)]
    public class ActionRide : SAction
    {
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            AnimalRide ride = select.GetComponent<AnimalRide>(); // 获取可骑乘动物组件
            if (ride != null)
            {
                character.Riding.RideAnimal(ride); // 角色骑乘该动物
            }
        }

        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            AnimalRide ride = select.GetComponent<AnimalRide>(); // 获取可骑乘动物组件
            return ride != null && !ride.IsDead() && character.Riding != null; // 返回是否可以进行骑乘的条件判断
        }
    }

}
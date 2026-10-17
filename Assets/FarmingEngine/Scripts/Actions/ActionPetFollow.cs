using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 用于命令宠物跟随（跟随玩家并攻击）
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/PetFollow", order = 50)]
    public class ActionPetFollow : SAction
    {
        // 执行命令宠物跟随的方法
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            Pet pet = select.GetComponent<Pet>(); // 获取可选物体上的宠物组件
            pet.Follow(); // 命令宠物跟随主人
        }

        // 判断是否可以执行命令宠物跟随的条件方法
        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            Pet pet = select.GetComponent<Pet>(); // 获取可选物体上的宠物组件
            return pet != null && pet.GetMaster() == character && !pet.IsFollow(); // 宠物存在且未在跟随状态下才能执行
        }
    }
}
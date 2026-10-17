using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 用于命令宠物停留（停止移动）
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/PetStay", order = 50)]
    public class ActionPetStay : SAction
    {
        // 执行命令宠物停留的方法
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            Pet pet = select.GetComponent<Pet>(); // 获取可选物体上的宠物组件
            pet.StopFollow(); // 命令宠物停留
        }

        // 判断是否可以执行命令宠物停留的条件方法
        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            Pet pet = select.GetComponent<Pet>(); // 获取可选物体上的宠物组件
            return pet != null && pet.GetMaster() == character && pet.IsFollow(); // 宠物存在且正在跟随主人才能执行停留命令
        }
    }
}
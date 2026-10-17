using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{

    /// <summary>
    /// 用于驯服宠物，驯服后的宠物将跟随其驯养者
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/PetTame", order = 50)]
    public class ActionPetTame : SAction
    {
        // 执行驯服宠物的方法
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            Pet pet = select.GetComponent<Pet>(); // 获取可选物体上的宠物组件
            pet.TamePet(character); // 驯服宠物，使其成为驯养者的跟随者
        }

        // 判断是否可以执行驯服宠物的条件方法
        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            Pet pet = select.GetComponent<Pet>(); // 获取可选物体上的宠物组件
            return pet != null && !pet.HasMaster(); // 宠物存在且没有驯养者才能进行驯服
        }
    }

}
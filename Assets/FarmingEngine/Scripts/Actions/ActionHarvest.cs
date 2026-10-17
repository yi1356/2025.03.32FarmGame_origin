using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 收获植物的果实
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Harvest", order = 50)]
    public class ActionHarvest : AAction
    {
        public float energy = 1f; // 操作消耗的能量

        // 执行操作
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            Plant plant = select.GetComponent<Plant>(); // 获取选择对象的植物组件
            if (plant != null)
            {
                string animation = character.Animation ? character.Animation.take_anim : ""; // 获取角色的收获动画
                character.TriggerAnim(animation, plant.transform.position); // 触发角色的收获动画
                character.TriggerBusy(0.5f, () =>
                {
                    character.Attributes.AddAttribute(AttributeType.Energy, -energy); // 扣除角色能量
                    plant.Harvest(character); // 收获植物的果实
                });
            }
        }

        // 判断是否可以执行操作的条件方法
        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            Plant plant = select.GetComponent<Plant>(); // 获取选择对象的植物组件
            if (plant != null)
            {
                return plant.HasFruit() && character.Attributes.GetAttributeValue(AttributeType.Energy) >= energy; // 如果植物有果实并且角色能量足够，返回 true
            }
            return false; // 否则返回 false
        }
    }

}
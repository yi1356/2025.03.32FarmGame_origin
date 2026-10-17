using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 直接从水源处饮水的操作
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/DrinkPond", order = 50)]
    public class ActionDrinkPond : SAction
    {
        public float drink_hp; // 饮水增加的生命值
        public float drink_energy; // 饮水增加的能量
        public float drink_hunger; // 饮水增加的饥饿值
        public float drink_happy; // 饮水增加的幸福值

        // 执行从水源饮水操作
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            string animation = character.Animation ? character.Animation.take_anim : ""; // 获取角色的采摘动画名称
            character.TriggerAnim(animation, select.transform.position); // 触发角色的采摘动画，并传入选择对象的位置
            character.TriggerBusy(0.5f, () =>
            {
                character.Attributes.AddAttribute(AttributeType.Health, drink_hp); // 增加角色的生命值属性
                character.Attributes.AddAttribute(AttributeType.Energy, drink_energy); // 增加角色的能量属性
                character.Attributes.AddAttribute(AttributeType.Hunger, drink_hunger); // 增加角色的饥饿值属性
                character.Attributes.AddAttribute(AttributeType.Happiness, drink_happy); // 增加角色的幸福值属性
            });
        }
    }

}
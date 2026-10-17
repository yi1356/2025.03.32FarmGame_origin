using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 点击动物产品时收集动物产品的操作
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/CollectProduct", order = 50)]
    public class ActionCollectProduct : AAction
    {
        // 合并操作
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            AnimalLivestock animal = select.GetComponent<AnimalLivestock>(); // 获取动物生产类组件
            if (animal != null)
            {
                character.TriggerAnim("Take", animal.transform.position); // 触发角色的"Take"动画，并传入动物位置
                character.TriggerBusy(0.5f, () =>
                {
                    animal.CollectProduct(character); // 角色忙碌0.5秒后，收集动物产品
                });
            }
        }

        // 判断是否可以进行收集动物产品的操作的条件方法
        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            AnimalLivestock animal = select.GetComponent<AnimalLivestock>(); // 获取动物生产类组件
            return animal != null && animal.HasProduct(); // 只有当动物存在并且有产品时才能执行操作
        }
    }

}
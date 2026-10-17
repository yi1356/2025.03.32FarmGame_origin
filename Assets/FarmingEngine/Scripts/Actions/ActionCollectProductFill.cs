using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 使用容器收集动物产品的操作（例如挤牛奶）
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/CollectProductFill", order = 50)]
    public class ActionCollectProductFill : MAction
    {
        // 合并操作
        public override void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            AnimalLivestock animal = select.GetComponent<AnimalLivestock>(); // 获取动物生产类组件
            if (select.HasGroup(merge_target) && animal != null) // 检查是否具有合并目标组并且是动物生产类
            {
                character.TriggerAnim("Take", animal.transform.position); // 触发角色的"Take"动画，并传入动物位置
                character.TriggerBusy(0.5f, () =>
                {
                    InventoryData inventory = slot.GetInventory(); // 获取物品栏数据
                    inventory.RemoveItemAt(slot.index, 1); // 从物品栏移除一个物品
                    animal.CollectProduct(character); // 收集动物产品
                });
            }
        }

        // 判断是否可以进行收集动物产品的操作的条件方法
        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            AnimalLivestock animal = select.GetComponent<AnimalLivestock>(); // 获取动物生产类组件
            return select.HasGroup(merge_target) && animal != null && animal.HasProduct(); // 只有当具有合并目标组、存在动物并且动物有产品时才能执行操作
        }
    }

}
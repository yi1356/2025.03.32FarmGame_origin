using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 使用钓竿钓鱼！
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Fish", order = 50)]
    public class ActionFish : SAction
    {
        public GroupData fishing_rod; // 钓竿的组数据
        public float fish_time = 3f; // 钓鱼所需时间

        // 执行钓鱼操作
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            if (select != null)
            {
                character.FaceTorward(select.transform.position); // 角色面向选择对象的位置

                ItemProvider pond = select.GetComponent<ItemProvider>(); // 获取选择对象的物品提供者组件(Z：物品提供组件)
                if (pond != null)
                {
                    if (pond.HasItem()) // 如果池塘有物品（鱼）
                    {
                        character.FishItem(pond, 1, fish_time); // 角色钓鱼
                        character.Attributes.GainXP("fishing", 10); // 示例：增加钓鱼经验值
                    }
                }
            }
        }

        // 判断是否可以进行钓鱼操作的条件方法
        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            ItemProvider pond = select.GetComponent<ItemProvider>(); // 获取选择对象的物品提供者组件
            return pond != null && pond.HasItem() && character.EquipData.HasItemInGroup(fishing_rod) && !character.IsSwimming(); // 只有当池塘有物品、角色装备有钓竿、且角色不在游泳状态时才能进行钓鱼操作
        }
    }

}
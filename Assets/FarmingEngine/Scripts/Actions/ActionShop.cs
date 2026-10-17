using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 商店动作，用于与商店NPC交互
    /// </summary>
    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Shop", order = 50)]
    public class ActionShop : AAction
    {
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            ShopNPC shop = select.GetComponent<ShopNPC>(); // 获取选择对象上的商店NPC组件
            if (shop != null)
                shop.OpenShop(character); // 打开商店界面，让玩家与商店NPC交互
        }

        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            ShopNPC shop = select.GetComponent<ShopNPC>(); // 获取选择对象上的商店NPC组件
            return shop != null; // 如果选择对象有商店NPC组件，则可以执行该动作
        }
    }
}
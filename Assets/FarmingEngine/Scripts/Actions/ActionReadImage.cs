using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 阅读物品上的图像。
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/ReadImage", order = 50)]
    public class ActionReadImage : SAction
    {
        public Sprite image; // 图像属性

        // 阅读插槽中物品的图像动作
        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            ItemData item = slot.GetItem(); // 获取插槽中的物品数据
            if (item != null)
            {
                ReadPanel.Get(1).ShowPanel(item.title, image); // 显示阅读面板，显示物品的标题和图像
            }
        }

        // 判断是否可以执行阅读动作的条件方法
        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            return true; // 任何时候都可以执行阅读动作
        }
    }

}
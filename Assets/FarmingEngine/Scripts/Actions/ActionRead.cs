using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 阅读物品上的注释。
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Read", order = 50)]
    public class ActionRead : SAction
    {

        // 阅读插槽中物品的注释动作
        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            ItemData item = slot.GetItem(); // 获取插槽中的物品数据
            if (item != null)
            {
                ReadPanel.Get().ShowPanel(item.title, item.desc); // 显示阅读面板，显示物品的标题和描述
            }

        }

        // 阅读可选对象上的注释动作
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            ReadObject read = select.GetComponent<ReadObject>(); // 获取可选对象上的阅读组件
            if (read != null)
            {
                ReadPanel.Get().ShowPanel(read.title, read.text); // 显示阅读面板，显示对象的标题和文本内容
            }

        }

        // 判断是否可以执行阅读动作的条件方法
        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            return true; // 任何时候都可以执行阅读动作
        }
    }

}
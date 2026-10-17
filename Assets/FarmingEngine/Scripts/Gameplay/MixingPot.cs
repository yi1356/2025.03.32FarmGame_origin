using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine {

    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(UniqueID))]
    public class MixingPot : MonoBehaviour
    {
        public ItemData[] recipes; // 混合罐中可以使用的配方数组
        public int max_items = 6; // 最大物品数量
        public bool clear_on_mix = false; // 混合后是否清空混合罐

        private Selectable select; // 可选择组件

        void Start()
        {
            select = GetComponent<Selectable>(); // 获取可选择组件

            select.onUse += OnUse; // 添加使用事件监听
        }

        // 当使用混合罐时触发的方法
        private void OnUse(PlayerCharacter player)
        {
            if (!string.IsNullOrEmpty(select.GetUID())) // 检查是否已生成唯一标识符
            {
                MixingPanel.Get().ShowMixing(player, this, select.GetUID()); // 显示混合面板并传递相关信息
            }
            else
            {
                Debug.LogError("You must generate the UID to use the mixing pot feature."); // 如果唯一标识符为空，则输出错误信息
            }
        }

        // 获取可选择组件
        public Selectable GetSelectable()
        {
            return select;
        }
    }

}
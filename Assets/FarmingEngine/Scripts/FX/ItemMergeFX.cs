using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// 显示移动设备上可能的合并操作的特效
    /// </summary>
    public class ItemMergeFX : MonoBehaviour
    {
        public GameObject icon_group; // 物品图标组对象
        public SpriteRenderer icon; // 物品图标的SpriteRenderer组件
        public Text title; // 显示合并操作标题的Text组件

        [HideInInspector]
        public Selectable target; // 目标可选择对象

        private static ItemMergeFX _instance; // 单例实例

        void Awake()
        {
            _instance = this; // 设置单例实例
            icon_group.SetActive(false); // 初始时隐藏物品图标组
        }

        void Update()
        {
            if (target == null)
            {
                Destroy(gameObject); // 如果目标为空，则销毁当前对象
                return;
            }

            if (icon_group.activeSelf)
                icon_group.SetActive(false); // 如果物品图标组处于激活状态，则关闭它

            if (!target.IsActive()) // 如果目标不是活跃状态，则返回
            {
                return;
            }

            transform.position = target.transform.position; // 将特效位置设置为目标位置
            transform.rotation = Quaternion.LookRotation(TheCamera.Get().transform.forward, Vector3.up); // 保持朝向摄像机前方

            ItemSlot selected = ItemSlotPanel.GetSelectedSlotInAllPanels(); // 获取所有面板中选中的物品槽
            if (selected != null && selected.GetItem() != null)
            {
                MAction action = selected.GetItem().FindMergeAction(target); // 查找选中物品与目标之间的合并操作
                foreach (PlayerCharacter player in PlayerCharacter.GetAll()) // 遍历所有玩家角色
                {
                    if (player != null && action != null && action.CanDoAction(player, selected, target)) // 如果满足合并条件
                    {
                        icon.sprite = selected.GetItem().icon; // 设置物品图标
                        title.text = action.title; // 设置合并操作标题
                        icon_group.SetActive(true); // 激活物品图标组
                    }
                }
            }
        }
    }
}

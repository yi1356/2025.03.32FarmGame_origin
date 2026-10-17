using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 控制箱子打开和关闭的动画效果
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class ChestAnim : MonoBehaviour
    {
        public Transform chest_lid; // 箱子的盖子变换对象
        public Transform chest_lid_outline; // 箱子盖子的轮廓变换对象

        private Quaternion start_rot; // 盖子的初始旋转角度
        private Selectable select; // 可选组件的引用

        void Start()
        {
            select = GetComponent<Selectable>(); // 获取可选组件的引用
            start_rot = chest_lid.localRotation; // 记录盖子的初始本地旋转角度
        }

        void Update()
        {
            // 获取与选择组件关联的箱子物品槽面板
            ItemSlotPanel chest_panel = ItemSlotPanel.Get(select.GetUID());

            // 判断箱子是否打开
            bool open = chest_panel != null && chest_panel.IsVisible();

            // 计算目标旋转角度，打开时旋转至 -90 度，关闭时旋转至初始角度
            Quaternion target = open ? Quaternion.Euler(-90f, 0f, 0f) * start_rot : start_rot;

            // 使用球面插值平滑地旋转盖子
            chest_lid.localRotation = Quaternion.Slerp(chest_lid.localRotation, target, 10f * Time.deltaTime);

            // 如果有盖子轮廓对象，同样平滑地旋转它
            if (chest_lid_outline != null)
                chest_lid_outline.localRotation = Quaternion.Slerp(chest_lid_outline.localRotation, target, 10f * Time.deltaTime);
        }
    }
}
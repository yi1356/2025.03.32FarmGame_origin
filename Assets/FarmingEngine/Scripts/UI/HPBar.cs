using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// 处理显示目标生命值的血条
    /// </summary>
    public class HPBar : MonoBehaviour
    {
        public Image fill; // 代表血条填充部分的 Image 组件

        [HideInInspector]
        public Destructible target; // 需要显示生命值的目标（可破坏的对象）

        private CanvasGroup canvas_group; // 控制 Canvas 透明度的 CanvasGroup 组件

        void Start()
        {
            canvas_group = GetComponentInChildren<CanvasGroup>(); // 获取子物体中的 CanvasGroup 组件
            fill.fillAmount = 1f; // 初始化血条填充量为 1（满血）
            canvas_group.alpha = 0f; // 初始化 CanvasGroup 透明度为 0（完全透明）
        }

        void Update()
        {
            // 获取摄像机的前方方向
            Vector3 dir = TheCamera.Get().GetFacingFront();
            // 使血条始终面向摄像机
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            // 检查目标是否为空或是否已死亡
            if (target == null || target.IsDead())
            {
                // 如果目标为空或已死亡，则销毁血条对象
                Destroy(gameObject);
            }
            else
            {
                // 更新血条填充量
                fill.fillAmount = target.hp / (float) target.GetMaxHP();
                // 根据血条的填充量来决定 CanvasGroup 的透明度
                canvas_group.alpha = fill.fillAmount < 0.999f ? 1f : 0f;
            }
        }
    }
}
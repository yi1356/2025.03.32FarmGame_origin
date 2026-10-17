using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    public class ActionProgress : MonoBehaviour
    {
        public Image fill; // 进度条的图像组件
        public float duration; // 进度条的持续时间

        [HideInInspector]
        public bool manual = false; // 如果为真，则手动设置进度值
        [HideInInspector]
        public float manual_value = 0f; // 手动设置的进度值

        private float timer = 0f; // 计时器，用于跟踪进度

        void Start()
        {
            // 初始设置
        }

        void Update()
        {
            // 如果游戏处于暂停状态，则不执行更新
            if (TheGame.Get().IsPaused())
                return;
			
            // 获取相机的前向方向并设置对象的旋转
            Vector3 dir = TheCamera.Get().GetFacingFront();
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            if (manual)
            {
                // 如果处于手动模式，则直接设置进度值
                fill.fillAmount = manual_value;
            }
            else
            {
                // 否则根据计时器计算进度
                timer += Time.deltaTime;
                float value = timer / duration;
                fill.fillAmount = value;

                // 如果进度超过1，则销毁该游戏对象
                if (value > 1f)
                    Destroy(gameObject);
            }
        }
    }

}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{

    /// <summary>
    /// 显示一个填充条，用于显示数值（如属性值）
    /// </summary>
    public class ProgressBar : MonoBehaviour
    {
        public Image bar_fill; // 填充条的 Image 组件
        public Text bar_text;  // 显示数值的 Text 组件

        private int max_value = 100; // 最大值
        private int min_value = 0;   // 最小值

        private int target_value;    // 目标值
        private int current_value;   // 当前值
        private int start_value;     // 起始值
        private float current_value_float; // 当前值的浮点数表示
        private float timer = 0f;    // 定时器，用于动画效果

        void Start()
        {
            start_value = current_value; // 初始化起始值为当前值
        }

        void Update()
        {
            // 如果当前值不等于目标值，进行平滑过渡
            if (current_value != target_value)
            {
                timer += Time.deltaTime; // 更新定时器
                float val = Mathf.Clamp01(timer / 2f); // 计算过渡进度，限制在 0 到 1 之间
                current_value_float = start_value * (1f - val) + target_value * val; // 插值计算当前值
                current_value = Mathf.RoundToInt(current_value_float); // 将浮点值四舍五入为整数
            }
            else
            {
                start_value = current_value; // 如果达到目标值，更新起始值
            }

            // 更新填充条的填充量
            bar_fill.fillAmount = (current_value_float - min_value) / (float)(max_value - min_value);

            // 更新文本显示当前值
            if (bar_text != null)
            {
                bar_text.text = current_value.ToString();
            }
        }

        /// <summary>
        /// 设置最大值
        /// </summary>
        /// <param name="val">最大值</param>
        public void SetMax(int val)
        {
            max_value = val;
        }

        /// <summary>
        /// 设置最小值
        /// </summary>
        /// <param name="val">最小值</param>
        public void SetMin(int val)
        {
            min_value = val;
        }

        /// <summary>
        /// 设置当前值并立即显示
        /// </summary>
        /// <param name="val">当前值</param>
        public void SetValue(int val)
        {
            target_value = val;
            current_value = val;
            current_value_float = val;
            start_value = val;
            timer = 0f; // 重置定时器
        }

        /// <summary>
        /// 设置目标值并开始平滑过渡
        /// </summary>
        /// <param name="val">目标值</param>
        public void SetValueRoll(int val)
        {
            target_value = val;
            timer = 0f; // 重置定时器
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// 显示天数和时间的时钟
    /// </summary>
    public class TimeClockUI : MonoBehaviour
    {
        public Text day_txt; // 显示天数的文本
        public Text time_txt; // 显示时间的文本
        public Image clock_fill; // 时钟填充图像

        void Start()
        {
            // 可以在这里进行初始化工作
        }

        void Update()
        {
            // 获取玩家数据
            PlayerData pdata = PlayerData.Get();
            // 计算小时数和秒数
            int time_hours = Mathf.FloorToInt(pdata.day_time);
            int time_secs = Mathf.FloorToInt((pdata.day_time * 60f) % 60f);

            // 更新天数和时间的文本
            day_txt.text = "DAY " + pdata.day;
            time_txt.text = time_hours + ":" + time_secs.ToString("00");

            // 判断时钟方向（顺时针或逆时针）
            bool clockwise = pdata.day_time <= 12f;
            clock_fill.fillClockwise = clockwise;
            if (clockwise)
            {
                // 顺时针填充：从0到1
                float value = pdata.day_time / 12f;
                clock_fill.fillAmount = value;
            }
            else
            {
                // 逆时针填充：从1到0
                float value = (pdata.day_time - 12f) / 12f;
                clock_fill.fillAmount = 1f - value;
            }
        }
    }
}
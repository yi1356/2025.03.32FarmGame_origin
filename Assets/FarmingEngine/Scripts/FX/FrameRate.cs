using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 控制帧率显示的脚本
    /// </summary>
    public class FrameRate : MonoBehaviour
    {
        private float average_delta = 0f; // 平均帧间隔时间
        private GUIStyle style; // GUI 样式对象，用于设置文本显示样式

        private void Start()
        {
            style = new GUIStyle(); // 创建GUI样式对象
            style.alignment = TextAnchor.UpperLeft; // 设置文本对齐方式为左上角
            style.fontSize = Screen.height / 50; // 根据屏幕高度动态设置字体大小
            style.normal.textColor = new Color(0f, 0f, 0.4f, 1f); // 设置文本颜色为深蓝色
        }

        void Update()
        {
            float diff = Time.unscaledDeltaTime - average_delta; // 计算当前帧间隔时间与平均帧间隔时间的差值
            average_delta += diff * 0.2f; // 平滑计算平均帧间隔时间
        }

        void OnGUI()
        {
            float miliseconds = average_delta * 1000f; // 将平均帧间隔时间转换为毫秒
            float frame_rate = 1f / average_delta; // 计算帧率

            // 构造显示文本
            string text = miliseconds.ToString("0.0") + " ms (" + frame_rate.ToString("0") + " fps)";

            Rect rect = new Rect(0, 0, Screen.width, Screen.height / 50); // 创建文本显示区域
            GUI.Label(rect, text, style); // 在GUI中显示文本
        }
    }
}
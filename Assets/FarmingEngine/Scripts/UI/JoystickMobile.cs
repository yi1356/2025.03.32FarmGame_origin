using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 显示移动端虚拟摇杆
    /// </summary>
    public class JoystickMobile : MonoBehaviour
    {
        public int joystick_id; // 摇杆的唯一标识符

        public float sensitivity = 0.08f; // 灵敏度，决定摇杆达到全速前的百分比
        public float threshold = 0.02f; // 阈值，相对于屏幕高度的百分比
        public float range = 0.1f; // 当 fixed_position 为 true 时，摇杆可以使用的范围
        public bool fixed_position = false; // 是否固定位置

        public RectTransform pin; // 摇杆的指示器

        private CanvasGroup canvas; // 用于控制 Canvas 透明度的组件
        private RectTransform rect; // 摇杆的 RectTransform 组件

        private bool joystick_active = false; // 摇杆是否处于激活状态
        private bool joystick_down = false; // 摇杆是否被按下
        private Vector2 joystick_pos; // 摇杆的位置
        private Vector2 joystick_dir; // 摇杆的方向

        private static JoystickMobile instance; // 单例实例
        private static List<JoystickMobile> joysticks = new List<JoystickMobile>(); // 所有的摇杆实例

        void Awake()
        {
            instance = this; // 初始化单例
            joysticks.Add(this); // 添加到摇杆列表
            canvas = GetComponent<CanvasGroup>(); // 获取 CanvasGroup 组件
            rect = GetComponent<RectTransform>(); // 获取 RectTransform 组件
            canvas.alpha = 0f; // 初始化时设置透明度为 0

            if (!TheGame.IsMobile())
                enabled = false; // 如果不是移动设备，禁用脚本
        }

        private void Start()
        {
            joystick_pos = TheUI.Get().WorldToScreenPos(rect.transform.position); // 初始化摇杆位置
        }

        private void OnDestroy()
        {
            joysticks.Remove(this); // 从摇杆列表中移除
        }

        void Update()
        {
            PlayerControlsMouse controls = PlayerControlsMouse.Get(); // 获取玩家鼠标控制

            if (Input.GetMouseButtonDown(0) && controls.IsInGameplay())
            {
                if (!fixed_position)
                    joystick_pos = Input.mousePosition; // 非固定位置时更新摇杆位置
                joystick_dir = Vector2.zero; // 重置摇杆方向
                joystick_active = false; // 摇杆不激活
                float nrange = range * Screen.height; // 摇杆的范围
                float diff = Vector3.Distance(joystick_pos, Input.mousePosition); // 当前鼠标位置与摇杆位置的距离
                joystick_down = diff < nrange; // 判断是否在范围内
            }

            if (!Input.GetMouseButton(0))
            {
                joystick_active = false; // 鼠标未按下时，摇杆不激活
                joystick_down = false; // 摇杆未按下
                joystick_dir = Vector2.zero; // 重置摇杆方向
            }

            if (Input.GetMouseButton(0) && joystick_down)
            {
                Vector2 mpos = new Vector2(Input.mousePosition.x, Input.mousePosition.y); // 当前鼠标位置
                Vector2 distance = mpos - joystick_pos; // 计算鼠标位置与摇杆位置的距离
                distance = distance / (float)Screen.height; // 缩放距离
                if (distance.magnitude > threshold)
                    joystick_active = true; // 超过阈值时激活摇杆

                joystick_dir = distance / sensitivity; // 计算摇杆方向
                joystick_dir = joystick_dir.normalized * Mathf.Min(joystick_dir.magnitude, 1f); // 归一化方向
                if (distance.magnitude < threshold)
                    joystick_dir = Vector2.zero; // 如果距离小于阈值，方向为零
            }

            if (Input.touchCount >= 2)
                joystick_active = false; // 多点触摸时，摇杆不激活

            bool build_mode = PlayerUI.GetFirst() != null && PlayerUI.GetFirst().IsBuildMode(); // 判断是否处于建筑模式
            float target_alpha = IsVisible() && !build_mode ? 1f : 0f; // 计算目标透明度
            canvas.alpha = Mathf.MoveTowards(canvas.alpha, target_alpha, 4f * Time.deltaTime); // 平滑过渡透明度

            Vector2 screenPos = joystick_pos;
            rect.anchoredPosition = TheUI.Get().ScreenPointToCanvasPos(screenPos); // 更新摇杆的位置
            pin.anchoredPosition = joystick_dir * 50f; // 更新摇杆指示器的位置
        }

        /// <summary>
        /// 获取摇杆是否激活
        /// </summary>
        /// <returns>摇杆是否激活</returns>
        public bool IsActive()
        {
            return joystick_active;
        }

        /// <summary>
        /// 获取摇杆是否可见
        /// </summary>
        /// <returns>摇杆是否可见</returns>
        public bool IsVisible()
        {
            return joystick_active || fixed_position; // 如果摇杆激活或固定位置，则可见
        }

        /// <summary>
        /// 获取摇杆的位置
        /// </summary>
        /// <returns>摇杆的位置</returns>
        public Vector2 GetPosition()
        {
            return joystick_pos;
        }

        /// <summary>
        /// 获取摇杆的方向
        /// </summary>
        /// <returns>摇杆的方向</returns>
        public Vector2 GetDir()
        {
            return joystick_dir;
        }

        /// <summary>
        /// 获取单例实例
        /// </summary>
        /// <returns>单例实例</returns>
        public static JoystickMobile Get()
        {
            return instance;
        }

        /// <summary>
        /// 根据 ID 获取摇杆
        /// </summary>
        /// <param name="id">摇杆的 ID</param>
        /// <returns>对应的摇杆实例</returns>
        public static JoystickMobile Get(int id)
        {
            foreach (JoystickMobile stick in joysticks)
            {
                if (stick.joystick_id == id)
                    return stick;
            }
            return null;
        }
    }
}

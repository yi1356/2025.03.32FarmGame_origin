using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// 通用的 UI 面板脚本，可以被继承(Z：其实就是实现了一些显示、隐藏的方法、并在Update中判断进行了平滑显隐过度)
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPanel : MonoBehaviour
    {
        public float display_speed = 4f; // 面板显示和隐藏的速度

        public UnityAction onShow; // 显示面板时触发的事件
        public UnityAction onHide; // 隐藏面板时触发的事件

        private CanvasGroup canvas_group; // CanvasGroup 组件，用于控制面板的透明度
        private bool visible; // 面板是否可见的标志

        protected virtual void Awake()
        {
            canvas_group = GetComponent<CanvasGroup>(); // 获取 CanvasGroup 组件
            canvas_group.alpha = 0f; // 初始时透明度为 0
            visible = false; // 初始时面板不可见
        }

        protected virtual void Start()
        {
            // 在此可以进行其他初始化操作
        }

        protected virtual void Update()
        {
            // 根据面板是否可见来调整透明度
            float add = visible ? display_speed : -display_speed;
            float alpha = Mathf.Clamp01(canvas_group.alpha + add * Time.deltaTime);
            canvas_group.alpha = alpha;

            // 当面板隐藏且透明度接近 0 时，调用 AfterHide 方法
            if (!visible && alpha < 0.01f)
                AfterHide();
        }

        // 切换面板的显示状态
        public virtual void Toggle(bool instant = false)
        {
            if (IsVisible())
                Hide(instant); // 如果面板可见，则隐藏
            else
                Show(instant); // 如果面板不可见，则显示
        }

        // 显示面板
        public virtual void Show(bool instant = false)
        {
            visible = true; // 将面板标记为可见
            gameObject.SetActive(true); // 激活面板的 GameObject

            if (instant || display_speed < 0.01f)
                canvas_group.alpha = 1f; // 如果 instant 为 true 或 display_speed 很小，则立即设置透明度为 1

            if (onShow != null)
                onShow.Invoke(); // 调用显示面板时的事件
        }

        // 隐藏面板
        public virtual void Hide(bool instant = false)
        {
            visible = false; // 将面板标记为不可见
            if (instant || display_speed < 0.01f)
                canvas_group.alpha = 0f; // 如果 instant 为 true 或 display_speed 很小，则立即设置透明度为 0

            if (onHide != null)
                onHide.Invoke(); // 调用隐藏面板时的事件
        }

        // 设置面板的可见状态
        public void SetVisible(bool visi)
        {
            if (!visible && visi)
                Show(); // 如果当前不可见但需要可见，则显示面板
            else if (visible && !visi)
                Hide(); // 如果当前可见但需要隐藏，则隐藏面板
        }

        // 面板完全隐藏后的处理方法
        public virtual void AfterHide()
        {
            gameObject.SetActive(false); // 使面板的 GameObject 不可见
        }

        // 判断面板是否可见
        public bool IsVisible()
        {
            return visible;
        }

        // 判断面板是否完全可见（透明度接近 1）
        public bool IsFullyVisible()
        {
            return visible && canvas_group.alpha > 0.99f;
        }

        // 判断面板是否完全隐藏（不可见且 GameObject 不激活）
        public bool IsFullyHidden()
        {
            return !visible && !gameObject.activeSelf;
        }

        // 获取面板的透明度
        public float GetAlpha()
        {
            return canvas_group.alpha;
        }
    }
}

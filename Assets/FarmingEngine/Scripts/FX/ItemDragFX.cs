using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// 物品拖动效果，跟随鼠标显示物品图标和标题
    /// </summary>
    public class ItemDragFX : MonoBehaviour
    {
        public GameObject icon_group; // 物品图标组对象
        public SpriteRenderer icon; // 物品图标的SpriteRenderer组件
        public Text title; // 显示物品标题的Text组件
        public float refresh_rate = 0.1f; // 刷新频率

        private ItemSlot current_slot = null; // 当前物品槽
        private Selectable current_select = null; // 当前选择的可选择对象
        private float timer = 0f; // 计时器

        private static ItemDragFX _instance; // 单例实例

        void Awake()
        {
            _instance = this; // 设置单例实例
            icon_group.SetActive(false); // 初始时隐藏物品图标组
            title.enabled = false; // 初始时隐藏物品标题
        }

        void Update()
        {
            transform.position = PlayerControlsMouse.Get().GetMouseWorldPosition(); // 物品图标跟随鼠标位置
            transform.rotation = Quaternion.LookRotation(TheCamera.Get().transform.forward, Vector3.up); // 保持朝向摄像机前方

            PlayerCharacter player = PlayerCharacter.GetFirst(); // 获取第一个玩家角色
            PlayerControls controls = PlayerControls.GetFirst(); // 获取第一个玩家控制器

            // 获取当前物品槽中的物品合并操作
            MAction maction = current_slot != null && current_slot.GetItem() != null ? current_slot.GetItem().FindMergeAction(current_select) : null;

            // 根据条件判断是否显示物品标题
            title.enabled = maction != null && player != null && current_select.IsInUseRange(player)
                && maction.CanDoAction(player, current_slot, current_select);

            title.text = maction != null ? maction.title : ""; // 更新物品标题文本

            // 根据条件判断是否显示物品图标组
            bool active = current_slot != null && controls != null && !controls.IsGamePad();
            if (active != icon_group.activeSelf)
                icon_group.SetActive(active);

            icon.enabled = false; // 初始时隐藏物品图标
            if (current_slot != null && current_slot.GetItem())
            {
                icon.sprite = current_slot.GetItem().icon; // 更新物品图标
                icon.enabled = true; // 显示物品图标
            }

            timer += Time.deltaTime;
            if (timer > refresh_rate)
            {
                timer = 0f;
                SlowUpdate(); // 定期更新当前物品槽和选择的可选择对象
            }
        }

        // 慢速更新，用于定期更新当前物品槽和选择的可选择对象
        private void SlowUpdate()
        {
            current_slot = ItemSlotPanel.GetDragSlotInAllPanels(); // 获取所有面板中的拖动物品槽
            current_select = Selectable.GetNearestHover(transform.position); // 获取最近的悬停的可选择对象
        }

        // 获取单例实例的静态方法
        public static ItemDragFX Get()
        {
            return _instance;
        }
    }
}

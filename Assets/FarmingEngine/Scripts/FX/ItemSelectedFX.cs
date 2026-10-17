using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// 跟随鼠标显示选定物品的特效
    /// </summary>
    public class ItemSelectedFX : MonoBehaviour
    {
        public GameObject icon_group;  // 物品图标组
        public SpriteRenderer icon;    // 物品图标
        public Text title;             // 标题文本
        public float refresh_rate = 0.1f;  // 刷新频率

        private ItemSlot current_slot = null;  // 当前槽位
        private Selectable current_select = null;  // 当前选择对象
        private float timer = 0f;  // 计时器

        private static ItemSelectedFX _instance;  // 单例

        void Awake()
        {
            _instance = this;
            icon_group.SetActive(false);  // 初始状态隐藏图标组
            title.enabled = false;        // 初始状态标题文本不显示
        }

        void Update()
        {
            // 将特效位置设置为鼠标指向位置
            transform.position = PlayerControlsMouse.Get().GetPointingPos();
            // 将特效旋转为相机正方向朝向
            transform.rotation =  Quaternion.LookRotation(TheCamera.Get().transform.forward, Vector3.up);

            PlayerCharacter player = PlayerCharacter.GetFirst();
            PlayerControls controls = PlayerControls.GetFirst();

            // 获取当前槽位中的动作，检查是否可执行(Z 好像是决定是否显示Title)
            MAction maction = current_slot != null && current_slot.GetItem() != null ? current_slot.GetItem().FindMergeAction(current_select) : null;
            title.enabled = maction != null && player != null && maction.CanDoAction(player, current_slot, current_select);
            title.text = maction != null ? maction.title : "";

            // 根据是否使用游戏手柄决定是否显示图标组(Z 这里应该是检查选中格子是否为空，和是否使用游戏手柄 决定是否显示图标)
            bool active = current_slot != null && controls != null && !controls.IsGamePad();
            if (active != icon_group.activeSelf)
                icon_group.SetActive(active);

            icon.enabled = false;
            // 如果当前槽位有物品，显示其图标
            if (current_slot != null && current_slot.GetItem())
            {
                icon.sprite = current_slot.GetItem().icon;
                icon.enabled = true;
            }

            // 计时器更新
            timer += Time.deltaTime;
            if (timer > refresh_rate)
            {
                timer = 0f;
                SlowUpdate();  // 定时执行慢更新
            }
        }

        // 慢更新方法，用于较低频率更新当前槽位和选择对象
        private void SlowUpdate()
        {
            current_slot = ItemSlotPanel.GetSelectedSlotInAllPanels();  // 获取所有面板中选定的槽位
            current_select = Selectable.GetNearestHover(transform.position);  // 获取最接近的悬停选择对象
        }

        // 获取单例静态方法
        public static ItemSelectedFX Get()
        {
            return _instance;
        }
    }
}

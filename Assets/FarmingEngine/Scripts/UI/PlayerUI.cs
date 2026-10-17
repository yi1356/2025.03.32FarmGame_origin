using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// 玩家专用的游戏内 UI 面板（Z难道可以一个玩家一个数据吗？目前来看似乎只有一个玩家）
    /// </summary>
    public class PlayerUI : UIPanel
    {
        [Header("玩家信息")]
        public int player_id; // 玩家 ID

        [Header("游戏 UI")]
        public Text gold_value; // 显示金币数量的文本
        public UIPanel damage_fx; // 伤害特效面板
        public Text build_mode_text; // 建造模式文本
        public Image tps_cursor; // 第三人称视角光标
        public GameObject riding_button; // 骑乘按钮

        public UnityAction onCancelSelection; // 取消选择的回调

        private ItemSlotPanel[] item_slot_panels; // 物品槽面板数组
        private float damage_fx_timer = 0f; // 伤害特效计时器

        private static List<PlayerUI> ui_list = new List<PlayerUI>(); // 玩家 UI 面板列表

        protected override void Awake()
        {
            base.Awake();
            ui_list.Add(this); // 将当前 UI 面板添加到列表

            item_slot_panels = GetComponentsInChildren<ItemSlotPanel>(); // 获取所有子物品槽面板

            if (build_mode_text != null)
                build_mode_text.enabled = false; // 初始化时隐藏建造模式文本

            if (riding_button != null)
                riding_button.SetActive(false); // 初始化时隐藏骑乘按钮

            Show(true); // 显示面板
        }

        void OnDestroy()
        {
            ui_list.Remove(this); // 移除当前 UI 面板
        }

        protected override void Start()
        {
            base.Start();

            PlayerCharacter ui_player = GetPlayer(); // 获取玩家角色
            if (ui_player != null)
                ui_player.Combat.onDamaged += DoDamageFX; // 注册伤害特效回调

            PlayerControlsMouse mouse = PlayerControlsMouse.Get();
            mouse.onRightClick += (Vector3 pos) => { CancelSelection(); }; // 注册右键点击取消选择
        }

        protected override void Update()
        {
            base.Update();

            PlayerCharacter character = GetPlayer(); // 获取玩家角色
            int gold = (character != null) ? character.SaveData.gold : 0; // 获取金币数量
            if (gold_value != null)
                gold_value.text = gold.ToString(); // 更新金币显示

            // 初始化物品槽面板
            foreach (ItemSlotPanel panel in item_slot_panels)
                panel.InitPanel();

            // 处理伤害特效的显示
            damage_fx_timer += Time.deltaTime;

            if (build_mode_text != null)
                build_mode_text.enabled = IsBuildMode(); // 更新建造模式文本的显示状态

            if (tps_cursor != null)
                tps_cursor.enabled = TheCamera.Get().IsFreeRotation(); // 更新第三人称视角光标的显示状态

            if (character != null && !character.IsDead() && character.Attributes.IsDepletingHP())
                DoDamageFXInterval(); // 在角色生命值减少时显示伤害特效

            // 处理控制输入
            PlayerControls controls = PlayerControls.Get(player_id);

            if (controls.IsPressCraft())
            {
                CraftPanel.Get(player_id)?.Toggle(); // 切换工艺面板
                ActionSelectorUI.Get(player_id)?.Hide(); // 隐藏行动选择 UI
                ActionSelector.Get(player_id)?.Hide(); // 隐藏行动选择
            }

            // 处理背包面板
            BagPanel bag_panel = BagPanel.Get(player_id);
            if (character != null && bag_panel != null)
            {
                InventoryItemData item = character.Inventory.GetBestEquippedBag(); // 获取最佳装备的背包
                ItemData idata = ItemData.Get(item?.item_id);
                if (idata != null)
                    bag_panel.ShowBag(character, item.uid, idata.bag_size); // 显示背包
                else
                    bag_panel.HideBag(); // 隐藏背包
            }
			
			// 处理骑乘按钮
            if (riding_button != null)
            {
                bool active = character.IsRiding(); // 检查角色是否骑乘
                if (active != riding_button.activeSelf)
                    riding_button.SetActive(active); // 更新骑乘按钮的显示状态
            }
        }
		
		/// <summary>
        /// 点击停止骑乘按钮的处理方法
        /// </summary>
        public void OnClickStopRide()
        {
            PlayerCharacter character = GetPlayer();
            if(character != null)
                character.Riding.StopRide(); // 停止骑乘
        }

        /// <summary>
        /// 显示伤害特效
        /// </summary>
        public void DoDamageFX()
        {
            if (damage_fx != null)
                StartCoroutine(DamageFXRun()); // 启动伤害特效协程
        }

        /// <summary>
        /// 定时显示伤害特效
        /// </summary>
        public void DoDamageFXInterval()
        {
            if (damage_fx != null && damage_fx_timer > 0f)
                StartCoroutine(DamageFXRun()); // 启动伤害特效协程
        }

        /// <summary>
        /// 伤害特效协程
        /// </summary>
        /// <returns>协程的IEnumerator</returns>
        private IEnumerator DamageFXRun()
        {
            damage_fx_timer = -3f; // 重置伤害特效计时器
            damage_fx.Show(); // 显示伤害特效
            yield return new WaitForSeconds(1f); // 等待1秒
            damage_fx.Hide(); // 隐藏伤害特效
        }

        /// <summary>
        /// 取消所有选择
        /// </summary>
        public void CancelSelection()
        {
            ItemSlotPanel.CancelSelectionAll(); // 取消所有物品槽面板的选择
            CraftPanel.Get(player_id)?.CancelSelection(); // 取消工艺面板的选择
            CraftSubPanel.Get(player_id)?.CancelSelection(); // 取消子工艺面板的选择
            ActionSelectorUI.Get(player_id)?.Hide(); // 隐藏行动选择 UI
            ActionSelector.Get(player_id)?.Hide(); // 隐藏行动选择

            onCancelSelection?.Invoke(); // 调用取消选择的回调
        }

        /// <summary>
        /// 点击工艺按钮的处理方法
        /// </summary>
        public void OnClickCraft()
        {
            CancelSelection(); // 取消所有选择
            CraftPanel.Get(player_id)?.Toggle(); // 切换工艺面板
        }

        /// <summary>
        /// 获取选中的物品槽
        /// </summary>
        /// <returns>选中的物品槽</returns>
        public ItemSlot GetSelectedSlot()
        {
            foreach (ItemSlotPanel panel in ItemSlotPanel.GetAll())
            {
                if (panel.GetPlayerID() == player_id)
                {
                    ItemSlot slot = panel.GetSelectedSlot();
                    if (slot != null)
                        return slot;
                }
            }
            return null; // 如果没有选中的物品槽，则返回 null
        }

        /// <summary>
        /// 获取拖拽中的物品槽
        /// </summary>
        /// <returns>拖拽中的物品槽</returns>
        public ItemSlot GetDragSlot()
        {
            foreach (ItemSlotPanel panel in ItemSlotPanel.GetAll())
            {
                if (panel.GetPlayerID() == player_id)
                {
                    ItemSlot slot = panel.GetDragSlot();
                    if (slot != null)
                        return slot;
                }
            }
            return null; // 如果没有拖拽中的物品槽，则返回 null
        }

        /// <summary>
        /// 获取选中的物品槽索引
        /// </summary>
        /// <returns>选中的物品槽索引</returns>
        public int GetSelectedSlotIndex()
        {
            ItemSlot slot = ItemSlotPanel.GetSelectedSlotInAllPanels();
            return slot != null ? slot.index : -1; // 返回选中的物品槽索引，如果没有选中的槽则返回 -1
        }

        /// <summary>
        /// 获取选中的物品槽所属的库存数据
        /// </summary>
        /// <returns>选中的物品槽所属的库存数据</returns>
        public InventoryData GetSelectedSlotInventory()
        {
            ItemSlot slot = ItemSlotPanel.GetSelectedSlotInAllPanels();
            return slot != null ? slot.GetInventory() : null; // 返回选中的物品槽所属的库存数据，如果没有选中的槽则返回 null
        }

        /// <summary>
        /// 检查是否处于建造模式
        /// </summary>
        /// <returns>是否处于建造模式</returns>
        public bool IsBuildMode()
        {
            PlayerCharacter player = GetPlayer();
            if (player)
                return player.Crafting.IsBuildMode(); // 返回玩家是否处于建造模式
            return false;
        }

        /// <summary>
        /// 获取玩家角色
        /// </summary>
        /// <returns>玩家角色</returns>
        public PlayerCharacter GetPlayer()
        {
            return PlayerCharacter.Get(player_id); // 根据玩家 ID 获取玩家角色
        }

        /// <summary>
        /// 显示所有玩家 UI 面板
        /// </summary>
        public static void ShowUI()
        {
            foreach (PlayerUI ui in ui_list)
                ui.Show(); // 显示每个玩家 UI 面板
        }

        /// <summary>
        /// 隐藏所有玩家 UI 面板
        /// </summary>
        public static void HideUI()
        {
            foreach (PlayerUI ui in ui_list)
                ui.Hide(); // 隐藏每个玩家 UI 面板
        }

        /// <summary>
        /// 检查任何玩家 UI 面板是否可见
        /// </summary>
        /// <returns>玩家 UI 面板是否可见</returns>
        public static bool IsUIVisible()
        {
            if (ui_list.Count > 0)
                return ui_list[0].IsVisible(); // 返回第一个玩家 UI 面板的可见性
            return false;
        }

        /// <summary>
        /// 根据玩家 ID 获取对应的 PlayerUI 实例
        /// </summary>
        /// <param name="player_id">玩家 ID</param>
        /// <returns>对应的 PlayerUI 实例</returns>
        public static PlayerUI Get(int player_id = 0)
        {
            foreach (PlayerUI ui in ui_list)
            {
                if (ui.player_id == player_id)
                    return ui; // 返回对应玩家 ID 的 PlayerUI 实例
            }
            return null; // 如果未找到对应的 PlayerUI 实例，则返回 null
        }

        /// <summary>
        /// 获取第一个玩家的 PlayerUI 实例
        /// </summary>
        /// <returns>第一个玩家的 PlayerUI 实例</returns>
        public static PlayerUI GetFirst()
        {
            PlayerCharacter player = PlayerCharacter.GetFirst();
            if (player != null)
                return Get(player.player_id); // 根据第一个玩家的 ID 获取 PlayerUI 实例
            return null;
        }

        /// <summary>
        /// 获取所有的 PlayerUI 实例
        /// </summary>
        /// <returns>PlayerUI 实例列表</returns>
        public static List<PlayerUI> GetAll()
        {
            return ui_list; // 返回所有 PlayerUI 实例的列表
        }
    }
}

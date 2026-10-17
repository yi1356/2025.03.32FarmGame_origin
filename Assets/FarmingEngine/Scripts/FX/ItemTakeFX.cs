using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 物品拾取特效，用于展示物品被拾取后进入背包的效果（Z：好像和图片飞入背包有关）
    /// </summary>

    public class ItemTakeFX : MonoBehaviour
    {
        public SpriteRenderer icon; // 显示物品图标的SpriteRenderer组件（Z：就是图片）
        public float fx_speed = 10f; // 特效移动速度

        private Vector3 start_pos; // 初始位置
        private Vector3 start_scale; // 初始缩放
        private InventoryType inventory_target; // 目标背包类型
        private int slot_target = -1; // 目标物品槽索引，默认为-1，表示无效
        private int target_player = 0; // 目标玩家ID，默认为0
        private float timer = 0f; // 计时器
        private bool is_coin = false; // 是否为金币

        private void Awake()
        {
            start_pos = transform.position; // 记录初始位置
            start_scale = transform.localScale; // 记录初始缩放
        }

        void Start()
        {
            // 如果不是金币且目标槽索引小于0，销毁该游戏对象
            if (!is_coin && slot_target < 0)
                Destroy(gameObject);
        }

        void Update()
        {
            ItemSlotPanel panel = ItemSlotPanel.Get(inventory_target); // 获取物品槽面板
            PlayerUI player_ui = PlayerUI.Get(target_player); // 获取玩家UI

            // 处理物品的情况
            if (!is_coin && panel != null)
            {
                Vector3 wPos = panel.GetSlotWorldPosition(slot_target); // 获取目标物品槽的世界坐标
                DoMoveToward(wPos); // 执行朝向目标移动

                InventoryData inventory = panel.GetInventory(); // 获取背包数据
                InventoryItemData islot = inventory?.GetInventoryItem(slot_target); // 获取物品槽数据
                if (islot == null || islot.GetItem() == null)
                    Destroy(gameObject); // 如果物品数据为空，销毁该游戏对象
            }

            // 处理金币的情况
            if (is_coin && player_ui != null && player_ui.gold_value != null)
            {
                Vector3 wPos = player_ui.gold_value.transform.position; // 获取金币目标位置
                DoMoveToward(wPos); // 执行朝向目标移动
            }

            timer += Time.deltaTime; // 计时器累加
            if (timer > 2f)
                Destroy(gameObject); // 如果计时器超过2秒，销毁该游戏对象
        }

        // 执行朝向目标移动的方法
        private void DoMoveToward(Vector3 target_pos)
        {
            Vector3 dir = target_pos - transform.position; // 计算朝向目标的向量
            Vector3 tDir = target_pos - start_pos; // 计算初始位置到目标位置的向量
            float mdist = Mathf.Min(fx_speed * Time.deltaTime, dir.magnitude); // 计算移动距离
            float scale = dir.magnitude / tDir.magnitude; // 计算缩放比例
            transform.position += dir.normalized * mdist; // 移动位置
            transform.localScale = start_scale * scale; // 更新缩放
            transform.rotation = Quaternion.LookRotation(TheCamera.Get().transform.forward, Vector3.up); // 调整旋转

            if (dir.magnitude < 0.1f)
                Destroy(gameObject); // 如果接近目标位置，销毁该游戏对象
        }

        // 设置物品拾取特效参数的方法
        public void SetItem(ItemData item, InventoryType inventory, int slot)
        {
            inventory_target = inventory; // 设置背包类型
            slot_target = slot; // 设置物品槽索引
            icon.sprite = item.icon; // 设置图标
            is_coin = false; // 标记为非金币
        }

        // 设置金币拾取特效参数的方法
        public void SetCoin(ItemData item, int player_id)
        {
            icon.sprite = item.icon; // 设置金币图标
            target_player = player_id; // 设置目标玩家ID
            is_coin = true; // 标记为金币
        }

        // 执行物品拾取特效的静态方法
        public static void DoTakeFX(Vector3 pos, ItemData item, InventoryType inventory, int target_slot)
        {
            if (AssetData.Get().item_take_fx != null && item != null)
            {
                GameObject fx = Instantiate(AssetData.Get().item_take_fx, pos, Quaternion.identity); // 实例化特效对象
                fx.GetComponent<ItemTakeFX>().SetItem(item, inventory, target_slot); // 设置物品拾取特效参数
            }
        }

        // 执行金币拾取特效的静态方法
        public static void DoCoinTakeFX(Vector3 pos, ItemData item, int player_id)
        {
            if (AssetData.Get().item_take_fx != null && item != null)
            {
                GameObject fx = Instantiate(AssetData.Get().item_take_fx, pos, Quaternion.identity); // 实例化特效对象
                fx.GetComponent<ItemTakeFX>().SetCoin(item, player_id); // 设置金币拾取特效参数
            }
        }
    }
}

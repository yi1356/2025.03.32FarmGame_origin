using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{

    /// <summary>
    /// 定时生成物品，玩家可以拾取。例如鸟巢（生成鸟蛋）或钓鱼点（生成鱼类）等。
    /// </summary>

    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(UniqueID))]
    public class ItemProvider : MonoBehaviour
    {
        [Header("物品生成")]
        public float item_spawn_time = 2f; // 游戏时间（小时）
        public int item_max = 3; // 最大物品数量
        public ItemData[] items; // 可生成的物品数据数组

        [Header("物品获取")]
        public bool auto_take = true; // 是否允许角色通过点击自动获取物品，否则需要特殊操作

        [Header("特效")]
        public GameObject[] item_models; // 物品模型数组
        public AudioClip take_sound; // 获取物品时的音效

        private UniqueID unique_id; // 唯一ID组件

        private int nb_item = 1; // 当前物品数量
        private float item_progress = 0f; // 物品生成进度

        void Awake()
        {
            unique_id = GetComponent<UniqueID>();
        }

        private void Start()
        {
            if (PlayerData.Get().HasCustomInt(GetAmountUID()))
                nb_item = PlayerData.Get().GetCustomInt(GetAmountUID());

            if (auto_take)
                GetComponent<Selectable>().onUse += OnUse;
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            float game_speed = TheGame.Get().GetGameTimeSpeedPerSec();

            item_progress += game_speed * Time.deltaTime;
            if (item_progress > item_spawn_time)
            {
                item_progress = 0f;
                nb_item += 1;
                nb_item = Mathf.Min(nb_item, item_max);

                PlayerData.Get().SetCustomInt(GetAmountUID(), nb_item);
            }

            for (int i = 0; i < item_models.Length; i++)
            {
                bool visible = (i < nb_item);
                if (item_models[i].activeSelf != visible)
                    item_models[i].SetActive(visible);
            }
        }

        // 移除物品
        public void RemoveItem()
        {
            if (nb_item > 0)
                nb_item--;

            PlayerData.Get().SetCustomInt(GetAmountUID(), nb_item);
        }

        /// <summary>
        /// Z：玩家获取物品
        /// </summary>
        /// <param name="player"></param>
        /// <param name="quantity"></param>
        public void GainItem(PlayerCharacter player, int quantity=1)
        {
            if (items.Length > 0)
            {
                ItemData item = items[Random.Range(0, items.Length)];
                player.Inventory.GainItem(item, quantity); // 自动获取物品
            }
        }

        // 播放获取物品的音效
        public void PlayTakeSound()
        {
            TheAudio.Get().PlaySFX("item", take_sound);
        }

        private void OnUse(PlayerCharacter player)
        {
            if (HasItem())
            {
                string animation = player != null && player.Animation ? player.Animation.take_anim : "";
                player.TriggerAnim(animation, transform.position);
                player.TriggerBusy(0.5f, () =>
                {
                    RemoveItem();
                    GainItem(player);
                    PlayTakeSound();
                });
            }
        }

        // 是否有可获取的物品
        public bool HasItem()
        {
            return nb_item > 0;
        }

        // 获取当前物品数量
        public int GetNbItem()
        {
            return nb_item;
        }

        // 获取物品数量的唯一ID
        public string GetAmountUID()
        {
            if (!string.IsNullOrEmpty(unique_id.unique_id))
                return unique_id.unique_id + "_amount";
            return "";
        }
    }

}

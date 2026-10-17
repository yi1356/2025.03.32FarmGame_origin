using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 熔炉可以用来加工物品，需要放置在熔炉上。可以加工指定物品并在一定时间后完成。
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class Furnace : MonoBehaviour
    {
        public GameObject spawn_point; // 生成物品的位置点
        public int quantity_max = 1; // 最大处理数量

        [Header("FX")]
        public GameObject active_fx; // 活动特效
        public AudioClip put_audio; // 放置音效
        public AudioClip finish_audio; // 完成音效
        public GameObject progress_prefab; // 进度条预制体

        private Selectable select; // 可选择对象组件
        private UniqueID unique_id; // 唯一标识组件
        private Animator animator; // 动画控制器

        private ItemData prev_item = null; // 上一个物品数据
        private ItemData current_item = null; // 当前处理物品数据
        private int current_quantity = 0; // 当前处理数量
        private float timer = 0f; // 计时器
        private float duration = 0f; // 加工持续时间（游戏小时）
        private ActionProgress progress; // 加工进度条

        private static List<Furnace> furnace_list = new List<Furnace>(); // 所有熔炉对象的列表

        void Awake()
        {
            furnace_list.Add(this); // 添加到熔炉列表
            select = GetComponent<Selectable>(); // 获取可选择组件
            unique_id = GetComponent<UniqueID>(); // 获取唯一标识组件
            animator = GetComponentInChildren<Animator>(); // 获取动画控制器
        }

        private void OnDestroy()
        {
            furnace_list.Remove(this); // 从熔炉列表移除
        }

        private void Start()
        {
            // 根据唯一标识从玩家数据中获取保存的信息
            string item_id = PlayerData.Get().GetCustomString(GetItemUID());
            ItemData idata = ItemData.Get(item_id);
            if (HasUID() && idata != null)
            {
                timer = PlayerData.Get().GetCustomFloat(GetTimerUID());
                duration = PlayerData.Get().GetCustomFloat(GetDurationUID());
                current_quantity = PlayerData.Get().GetCustomInt(GetQuantityUID());
                current_item = idata;

                // 如果存在进度条预制体并且加工持续时间大于0.1秒，则实例化进度条
                if (progress_prefab != null && duration > 0.1f)
                {
                    GameObject obj = Instantiate(progress_prefab, transform);
                    progress = obj.GetComponent<ActionProgress>();
                    progress.manual = true;
                }
            }
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return; // 如果游戏暂停，则返回

            if (HasItem())
            {
                float game_speed = TheGame.Get().GetGameTimeSpeedPerSec(); // 获取游戏时间速度
                timer += game_speed * Time.deltaTime; // 计时

                PlayerData.Get().SetCustomFloat(GetTimerUID(), timer); // 更新玩家数据中的计时器值

                if (timer > duration)
                {
                    FinishItem(); // 完成加工
                }

                if (progress != null)
                    progress.manual_value = timer / duration; // 更新进度条的显示进度

                if (active_fx != null && active_fx.activeSelf != HasItem())
                    active_fx.SetActive(HasItem()); // 控制活动特效的显示状态

                if (animator != null)
                    animator.SetBool("Active", HasItem()); // 控制动画状态
            }
        }

        /// <summary>
        /// 将物品放置到熔炉中开始加工
        /// </summary>
        /// <param name="item">放置的物品数据</param>
        /// <param name="create">加工后生成的物品数据</param>
        /// <param name="duration">加工持续时间</param>
        /// <param name="quantity">放置的数量</param>
        /// <returns>实际放置到熔炉中的数量</returns>
        public int PutItem(ItemData item, ItemData create, float duration, int quantity)
        {
            if (current_item == null || create == current_item)
            {
                if (current_quantity < quantity_max && quantity > 0)
                {
                    int max = quantity_max - current_quantity; // 剩余的最大空间
                    int quant = Mathf.Min(max, quantity + current_quantity); // 不能放置超过最大限制的数量

                    prev_item = item;
                    current_item = create;
                    current_quantity += quant;
                    timer = 0f;
                    this.duration = duration;

                    PlayerData.Get().SetCustomFloat(GetTimerUID(), timer);
                    PlayerData.Get().SetCustomFloat(GetDurationUID(), duration);
                    PlayerData.Get().SetCustomInt(GetQuantityUID(), quant);
                    PlayerData.Get().SetCustomString(GetItemUID(), create.id);

                    // 如果存在进度条预制体并且加工持续时间大于0.1秒，则实例化进度条
                    if (progress_prefab != null && duration > 0.1f)
                    {
                        GameObject obj = Instantiate(progress_prefab, transform);
                        progress = obj.GetComponent<ActionProgress>();
                        progress.manual = true;
                    }

                    // 如果熔炉在摄像机附近，则播放放置音效
                    if (select.IsNearCamera(10f))
                        TheAudio.Get().PlaySFX("furnace", put_audio);

                    return quant; // 返回实际放置到熔炉中的数量
                }
            }
            return 0;
        }

        /// <summary>
        /// 完成当前加工的物品并生成物品到指定位置
        /// </summary>
        public void FinishItem()
        {
            if (current_item != null)
            {
                // 在生成点生成物品
                Item.Create(current_item, spawn_point.transform.position, current_quantity);

                prev_item = null;
                current_item = null;
                current_quantity = 0;
                timer = 0f;

                // 从玩家数据中移除相关的自定义数据
                PlayerData.Get().RemoveCustomFloat(GetTimerUID());
                PlayerData.Get().RemoveCustomFloat(GetDurationUID());
                PlayerData.Get().RemoveCustomInt(GetQuantityUID());
                PlayerData.Get().RemoveCustomString(GetItemUID());

                if (active_fx != null)
                    active_fx.SetActive(false); // 关闭活动特效

                if (animator != null)
                    animator.SetBool("Active", false); // 更新动画状态

                if (progress != null)
                    Destroy(progress.gameObject); // 销毁进度条

                // 如果熔炉在摄像机附近，则播放完成音效
                if (select.IsNearCamera(10f))
                    TheAudio.Get().PlaySFX("furnace", finish_audio);
            }
        }

        /// <summary>
        /// 检查熔炉中是否有正在加工的物品
        /// </summary>
        /// <returns>如果熔炉中有正在加工的物品则返回true，否则返回false</returns>
        public bool HasItem()
        {
            return current_item != null;
        }

        /// <summary>
        /// 计算当前熔炉中剩余的空间数量
        /// </summary>
        /// <returns>可以放置的物品数量</returns>
        public int CountItemSpace()
        {
            return quantity_max - current_quantity; // 返回剩余的物品空间数量
        }

        /// <summary>
        /// 检查熔炉是否有唯一标识
        /// </summary>
        /// <returns>如果熔炉有唯一标识则返回true，否则返回false</returns>
        public bool HasUID()
        {
            return unique_id != null && unique_id.HasUID();
        }

        /// <summary>
        /// 获取熔炉的唯一标识
        /// </summary>
        /// <returns>熔炉的唯一标识字符串</returns>
        public string GetUID()
        {
            return unique_id != null ? unique_id.unique_id : "";
        }

        /// <summary>
        /// 获取保存物品数据的唯一标识
        /// </summary>
        /// <returns>保存物品数据的唯一标识字符串</returns>
        public string GetItemUID()
        {
            if (HasUID())
                return GetUID() + "_item";
            return "";
        }

        /// <summary>
        /// 获取保存计时器数据的唯一标识
        /// </summary>
        /// <returns>保存计时器数据的唯一标识字符串</returns>
        public string GetTimerUID()
        {
            if (HasUID())
                return GetUID() + "_timer";
            return "";
        }

        /// <summary>
        /// 获取保存加工持续时间数据的唯一标识
        /// </summary>
        /// <returns>保存加工持续时间数据的唯一标识字符串</returns>
        public string GetDurationUID()
        {
            if (HasUID())
                return GetUID() + "_duration";
            return "";
        }

        /// <summary>
        /// 获取保存当前处理数量数据的唯一标识
        /// </summary>
        /// <returns>保存当前处理数量数据的唯一标识字符串</returns>
        public string GetQuantityUID()
        {
            if (HasUID())
                return GetUID() + "_quantity";
            return "";
        }

        /// <summary>
        /// 获取距离指定位置最近的熔炉对象
        /// </summary>
        /// <param name="pos">参考位置</param>
        /// <param name="range">搜索范围</param>
        /// <returns>最近的熔炉对象，如果没有找到则返回null</returns>
        public static Furnace GetNearestInRange(Vector3 pos, float range = 999f)
        {
            float min_dist = range;
            Furnace nearest = null;
            foreach (Furnace furnace in furnace_list)
            {
                float dist = (pos - furnace.transform.position).magnitude; // 计算距离
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = furnace; // 更新最近的熔炉对象
                }
            }
            return nearest; // 返回最近的熔炉对象
        }

        /// <summary>
        /// 获取所有熔炉对象的列表
        /// </summary>
        /// <returns>所有熔炉对象的列表</returns>
        public static List<Furnace> GetAll()
        {
            return furnace_list; // 返回所有熔炉对象的列表
        }
    }
}

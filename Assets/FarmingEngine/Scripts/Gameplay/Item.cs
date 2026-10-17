using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{

    /// <summary>
    /// 物品是可以被玩家拾取、丢弃并放入物品栏的对象。某些物品还可以用作制作材料或被用于制作。
    /// </summary>

    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(UniqueID))]
    public class Item : Craftable
    {
        [Header("Item")]
        public ItemData data; // 物品数据
        public int quantity = 1; // 数量

        [Header("FX")]
        public float auto_collect_range = 0f; // 当在范围内时将自动被收集(Z 不要设置为小于=0.1否则会出问题)
        public bool snap_to_ground = true; // 如果为真，物品将自动放置在地面上而不是浮空
        public AudioClip take_audio; // 收取时的音频
        public GameObject take_fx; // 收取时的特效

        [HideInInspector]
        public bool was_spawned = false; // 如果为真，表示物品是由玩家丢弃的或从保存文件中加载的

        public UnityAction onTake; // 收取时的事件
        public UnityAction onDestroy; // 销毁时的事件

        private Selectable selectable; // 可选中组件
        private UniqueID unique_id; // 唯一ID组件

        private static List<Item> item_list = new List<Item>(); // 物品列表

        protected override void Awake()
        {
            base.Awake();
            item_list.Add(this);
            selectable = GetComponent<Selectable>();
            unique_id = GetComponent<UniqueID>();
            selectable.onUse += OnUse; // 注册使用事件(Z：被拾取时会调用这里)
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            item_list.Remove(this);
        }

        private void Start()
        {
            if (!was_spawned && PlayerData.Get().IsObjectRemoved(GetUID()))
            {
                Destroy(gameObject);
                return;
            }

            if (snap_to_ground)
            {
                float dist;
                bool grounded = DetectGrounded(out dist);
                if (!grounded)
                {
                    transform.position += Vector3.down * dist;
                }
            }
        }

        //Z 会自动判断是否符合条件，监测物品耐久度小于临界就销毁，处理自动拾取的逻辑
        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (was_spawned && selectable.IsActive())//(Z  是被丢弃的物品，持续监测耐久度，低于临界值就销毁)
            {
                PlayerData pdata = PlayerData.Get();
                DroppedItemData dropped_item = pdata.GetDroppedItem(GetUID());
                if (dropped_item != null)
                {
                    if (data.HasDurability() && dropped_item.durability <= 0f)
                        DestroyItem(); // 根据耐久度销毁物品
                }
            }

            if (auto_collect_range > 0.1f)//(Z 感觉这个地方有点问题，自动拾取逻辑不应该先设置一个阈值，判断与玩家距离，小于阈值就会自动回收掉吗？)
            {
                PlayerCharacter player = PlayerCharacter.GetNearest(transform.position, auto_collect_range);
                if (player != null)
                {
                     player.Inventory.AutoTakeItem(this);
                     //Debug.Log("测试物品已经进入自动拾取范围"+this.gameObject.name);
                }
                   
            }
        }

        /// <summary>
        /// Z：委托方法（当其Slelectable中Use方法被调用后，触发这里）
        /// </summary>
        /// <param name="character"></param>
        private void OnUse(PlayerCharacter character)
        {
            // 收取物品
            character.Inventory.TakeItem(this);
        }

        public void TakeItem()//Z 这个一般在背包收集后使用，其实就是销毁这个模型及上面的各种东西
        {
            if (onTake != null)
                onTake.Invoke();

            DestroyItem();

            TheAudio.Get().PlaySFX("item", take_audio);
            if (take_fx != null)
                Instantiate(take_fx, transform.position, Quaternion.identity);
        }

        // 销毁物品但保留容器
        public void SpoilItem()
        {
            if (data.container_data)
                Item.Create(data.container_data, transform.position, quantity);
            DestroyItem();
        }

        // 吃掉物品
        public void EatItem()
        {
            quantity--;
            DroppedItemData invdata = PlayerData.Get().GetDroppedItem(GetUID());
            if (invdata != null)
                invdata.quantity = quantity;

            if (quantity <= 0)
                DestroyItem();
        }

        // 彻底销毁物品
        public void DestroyItem()
        {
            PlayerData pdata = PlayerData.Get();
            if (was_spawned)
                pdata.RemoveDroppedItem(GetUID()); // 从丢弃物品中移除
            else
                pdata.RemoveObject(GetUID()); // 从地图中移除

            item_list.Remove(this);

            if (onDestroy != null)
                onDestroy.Invoke();

            Destroy(gameObject);
        }

        // 检测物品是否接触地面
        private bool DetectGrounded(out float dist)
        {
            float radius = 20f;
            float radius_up = 5f;
            float offset = 0.5f;
            Vector3 center = transform.position + Vector3.up * offset;
            Vector3 centerup = transform.position + Vector3.up * radius_up;

            RaycastHit hd1, hu1, hf1;
            LayerMask everything = ~0;
            bool f1 = Physics.Raycast(center, Vector3.down, out hf1, offset + 0.1f, everything.value, QueryTriggerInteraction.Ignore);
            bool d1 = Physics.Raycast(center, Vector3.down, out hd1, radius + offset, everything.value, QueryTriggerInteraction.Ignore);
            bool u1 = Physics.Raycast(centerup, Vector3.down, out hu1, radius_up + 0.1f, everything.value, QueryTriggerInteraction.Ignore);
            dist = d1 ? hd1.distance - offset : (u1 ? hu1.distance - radius_up : 0f);
            return f1;
        }

        public bool HasUID()
        {
            return !string.IsNullOrEmpty(unique_id.unique_id);
        }

        public string GetUID()
        {
            return unique_id.unique_id;
        }

        // 是否具有指定组
        public bool HasGroup(GroupData group)
        {
            return data.HasGroup(group) || selectable.HasGroup(group);
        }

        public Selectable GetSelectable()
        {
            return selectable;
        }

        // 获取物品的保存数据
        public DroppedItemData SaveData
        {
            get { return PlayerData.Get().GetDroppedItem(GetUID()); }  // 可能为null，如果物品没有被丢弃或生成
        }

        // 获取最近的物品
        public static new Item GetNearest(Vector3 pos, float range = 999f)
        {
            Item nearest = null;
            float min_dist = range;
            foreach (Item item in item_list)
            {
                float dist = (item.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = item;
                }
            }
            return nearest;
        }

        // 统计范围内的物品数量
        public static int CountInRange(Vector3 pos, float range)
        {
            int count = 0;
            foreach (Item item in GetAll())
            {
                float dist = (item.transform.position - pos).magnitude;
                if (dist < range)
                    count++;
            }
            return count;
        }

        // 统计指定物品在范围内的数量
        public static int CountInRange(ItemData data, Vector3 pos, float range)
        {
            int count = 0;
            foreach (Item item in GetAll())
            {
                if (item.data == data)
                {
                    float dist = (item.transform.position - pos).magnitude;
                    if (dist < range)
                        count++;
                }
            }
            return count;
        }

        // 根据唯一ID获取物品
        public static Item GetByUID(string uid)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                foreach (Item item in item_list)
                {
                    if (item.GetUID() == uid)
                        return item;
                }
            }
            return null;
        }

        // 获取所有指定物品数据的物品列表
        public static List<Item> GetAllOf(ItemData data)
        {
            List<Item> valid_list = new List<Item>();
            foreach (Item item in item_list)
            {
                if (item.data == data)
                    valid_list.Add(item);
            }
            return valid_list;
        }

        // 获取所有物品列表
        public static new List<Item> GetAll()
        {
            return item_list;
        }

        // 生成一个已存在的物品（如加载后）
        public static Item Spawn(string uid, Transform parent = null)
        {
            DroppedItemData ddata = PlayerData.Get().GetDroppedItem(uid);
            if (ddata != null && ddata.scene == SceneNav.GetCurrentScene())
            {
                ItemData idata = ItemData.Get(ddata.item_id);
                if (idata != null)
                {
                    GameObject build = Instantiate(idata.item_prefab, ddata.pos, idata.item_prefab.transform.rotation);
                    build.transform.parent = parent;

                    Item item = build.GetComponent<Item>();
                    item.data = idata;
                    item.was_spawned = true;
                    item.unique_id.unique_id = uid;
                    item.quantity = ddata.quantity;
                    return item;
                }
            }
            return null;
        }

        // 创建一个全新的物品并添加到保存文件中
        public static Item Create(ItemData data, Vector3 pos, int quantity)
        {
            DroppedItemData ditem = PlayerData.Get().AddDroppedItem(data.id, SceneNav.GetCurrentScene(), pos, quantity, data.durability);
            GameObject obj = Instantiate(data.item_prefab, pos, data.item_prefab.transform.rotation);
            Item item = obj.GetComponent<Item>();
            item.data = data;
            item.was_spawned = true;
            item.unique_id.unique_id = ditem.uid;
            item.quantity = quantity;
            return item;
        }

        // 创建一个已存在于物品栏中的物品（如丢弃物品时）
        public static Item Create(ItemData data, Vector3 pos, int quantity, float durability, string uid)
        {
            DroppedItemData ditem = PlayerData.Get().AddDroppedItem(data.id, SceneNav.GetCurrentScene(), pos, quantity, durability, uid);
            GameObject obj = Instantiate(data.item_prefab, pos, data.item_prefab.transform.rotation);
            Item item = obj.GetComponent<Item>();
            item.data = data;
            item.was_spawned = true;
            item.unique_id.unique_id = ditem.uid;
            item.quantity = quantity;
            return item;
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 可堆叠多个同一类型物品的堆叠体（不适用于像箱子这样的存储容器）
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(UniqueID))]
    public class ItemStack : MonoBehaviour
    {
        public ItemData item; // 物品数据
        public int item_start = 0; // 初始物品数量
        public int item_max = 20; // 最大物品堆叠数量

        public GameObject item_mesh; // 物品模型

        private Selectable selectable;
        private UniqueID unique_id;

        private static List<ItemStack> stack_list = new List<ItemStack>(); // 所有堆叠体的静态列表

        void Awake()
        {
            stack_list.Add(this);
            selectable = GetComponent<Selectable>();
            unique_id = GetComponent<UniqueID>();
        }

        private void OnDestroy()
        {
            stack_list.Remove(this);
        }

        private void Start()
        {
            if (!PlayerData.Get().HasCustomInt(GetCountUID()))
                 PlayerData.Get().SetCustomInt(GetCountUID(), item_start);
        }

        void Update()
        {
            // 控制物品模型的显示状态
            if (item_mesh != null)
            {
                bool active = GetItemCount() > 0;
                if (active != item_mesh.activeSelf)
                    item_mesh.SetActive(active);
            }
        }

        // 添加物品到堆叠体中
        public void AddItem(int value)
        {
            int val = GetItemCount();
            PlayerData.Get().SetCustomInt(GetCountUID(), val + value);
        }

        // 从堆叠体中移除物品
        public void RemoveItem(int value)
        {
            int val = GetItemCount();
            val -= value;
            val = Mathf.Max(val, 0);
            PlayerData.Get().SetCustomInt(GetCountUID(), val);
        }

        // 获取堆叠体中物品的数量
        public int GetItemCount()
        {
            return PlayerData.Get().GetCustomInt(GetCountUID());
        }

        // 获取堆叠体的唯一ID
        public string GetUID()
        {
            return unique_id.unique_id;
        }

        // 获取堆叠体中物品数量的唯一ID
        public string GetCountUID()
        {
            return unique_id.unique_id + "_count";
        }

        // 获取距离指定位置最近的堆叠体
        public static ItemStack GetNearest(Vector3 pos, float range = 999f)
        {
            float min_dist = range;
            ItemStack nearest = null;
            foreach (ItemStack item in stack_list)
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

        // 获取所有堆叠体的列表
        public static List<ItemStack> GetAll()
        {
            return stack_list;
        }
    }
}

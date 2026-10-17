using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{

    public class AnimalFood : MonoBehaviour
    {
        public GroupData food_group; // 食物所属的组数据

        private Item item; // 物品组件
        private ItemStack stack; // 物品堆叠组件
        private Plant plant; // 植物组件

        private static List<AnimalFood> food_list = new List<AnimalFood>(); // 静态列表，存储所有AnimalFood实例

        void Awake()
        {
            food_list.Add(this); // 将当前实例添加到静态列表中

            // 下面的组件获取可能为空，通常只有一个会存在
            item = GetComponent<Item>(); // 获取Item组件
            stack = GetComponent<ItemStack>(); // 获取ItemStack组件
            plant = GetComponent<Plant>(); // 获取Plant组件
        }

        private void OnDestroy()
        {
            food_list.Remove(this); // 当销毁对象时，从静态列表中移除
        }

        public void EatFood()
        {
            if (item != null)
                item.EatItem(); // 如果存在Item组件，则吃掉物品
            if (stack != null)
                stack.RemoveItem(1); // 如果存在ItemStack组件，则移除一个物品
            if (plant != null)
                plant.KillNoLoot(); // 如果存在Plant组件，则摧毁植物但不掉落物品
        }

        public bool CanBeEaten()
        {
            if (stack != null)
                return stack.GetItemCount() > 0; // 如果存在ItemStack组件，则返回堆叠中物品数量是否大于0
            return true; // 否则默认可以被吃
        }

        public static AnimalFood GetNearest(GroupData group, Vector3 pos, float range = 999f)
        {
            float min_dist = range; // 最小距离初始化为range
            AnimalFood nearest = null; // 最近的食物初始化为null
            foreach (AnimalFood item in food_list)
            {
                // 如果食物的组数据与指定组相同，或者两者都为null
                if (item.food_group == group || group == null || item.food_group == null)
                {
                    float dist = (item.transform.position - pos).magnitude; // 计算食物与目标位置的距离
                    if (dist < min_dist && item.CanBeEaten()) // 如果距离小于最小距离且可以被吃
                    {
                        min_dist = dist; // 更新最小距离
                        nearest = item; // 更新最近的食物
                    }
                }
            }
            return nearest; // 返回最近的食物
        }

        public static AnimalFood GetNearest(Vector3 pos, float range = 999f)
        {
            float min_dist = range; // 最小距离初始化为range
            AnimalFood nearest = null; // 最近的食物初始化为null
            foreach (AnimalFood item in food_list)
            {
                float dist = (item.transform.position - pos).magnitude; // 计算食物与目标位置的距离
                if (dist < min_dist && item.CanBeEaten()) // 如果距离小于最小距离且可以被吃
                {
                    min_dist = dist; // 更新最小距离
                    nearest = item; // 更新最近的食物
                }
            }
            return nearest; // 返回最近的食物
        }

        public static List<AnimalFood> GetAll()
        {
            return food_list; // 返回所有食物的静态列表
        }
    }

}

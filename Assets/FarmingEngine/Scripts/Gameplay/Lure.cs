using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 用来吸引附近动物的诱饵物品
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class Lure : MonoBehaviour
    {
        public float range = 10f; // 作用范围

        private Selectable selectable; // 可选择组件

        private static List<Lure> lure_list = new List<Lure>(); // 所有诱饵物品的列表

        void Awake()
        {
            lure_list.Add(this); // 添加自身到诱饵物品列表
            selectable = GetComponent<Selectable>(); // 获取可选择组件
        }

        private void OnDestroy()
        {
            lure_list.Remove(this); // 从诱饵物品列表中移除自身
        }

        // 销毁诱饵物品
        public void Kill()
        {
            selectable.Destroy();
        }

        // 获取范围内最近的诱饵物品
        public static Lure GetNearestInRange(Vector3 pos)
        {
            Lure nearest = null;
            float min_dist = 999f;
            foreach (Lure lure in lure_list)
            {
                float dist = (lure.transform.position - pos).magnitude;
                if (dist < min_dist && dist < lure.range)
                {
                    min_dist = dist;
                    nearest = lure;
                }
            }
            return nearest;
        }

        // 获取范围内最近的诱饵物品
        public static Lure GetNearest(Vector3 pos, float range = 999f)
        {
            Lure nearest = null;
            float min_dist = range;
            foreach (Lure lure in lure_list)
            {
                float dist = (lure.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = lure;
                }
            }
            return nearest;
        }

        // 获取所有诱饵物品列表
        public static List<Lure> GetAll()
        {
            return lure_list;
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 自动给范围内的植物浇水
    /// </summary>

    public class Sprinkler : MonoBehaviour
    {
        public float range = 1f; // 浇水范围

        private static List<Sprinkler> sprinkler_list = new List<Sprinkler>(); // 所有洒水器的列表

        private void Awake()
        {
            sprinkler_list.Add(this); // 加入洒水器列表
        }

        private void OnDestroy()
        {
            sprinkler_list.Remove(this); // 移除洒水器列表
        }

        // 获取最近范围内的洒水器
        public static Sprinkler GetNearestInRange(Vector3 pos)
        {
            float min_dist = 999f; // 初始化最小距离为一个较大值
            Sprinkler nearest = null; // 最近的洒水器
            foreach (Sprinkler sprinkler in sprinkler_list)
            {
                float dist = (sprinkler.transform.position - pos).magnitude; // 计算位置之间的距离
                if (dist < min_dist && dist < sprinkler.range) // 如果距离小于当前最小距离且在洒水范围内
                {
                    nearest = sprinkler; // 更新最近的洒水器
                    min_dist = dist; // 更新最小距离
                }
            }
            return nearest; // 返回最近的洒水器
        }

        // 获取所有洒水器列表
        public static List<Sprinkler> GetAll()
        {
            return sprinkler_list;
        }
    }
}
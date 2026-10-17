using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 场景切换区域，当角色进入该区域时切换场景。确保该区域有一个触发器碰撞体。
    /// </summary>
    public class ExitZone : MonoBehaviour
    {
        [Header("离开")]
        public string scene; // 要切换到的场景名称
        public int go_to_index = 0; // 如果设置为0，将回到默认角色位置；否则将到达相同索引的出口区域

        [Header("进入")]
        public int entry_index = 1; // 确保这个值大于0
        public Vector3 entry_offset; // 进入区域时的偏移量

        private float timer = 0f; // 计时器，防止立刻触发

        private static List<ExitZone> exit_list = new List<ExitZone>(); // 所有出口区域的列表

        void Awake()
        {
            exit_list.Add(this); // 加入列表
        }

        private void OnDestroy()
        {
            exit_list.Remove(this); // 移除列表
        }

        void Update()
        {
            timer += Time.deltaTime; // 计时器增加
        }

        /// <summary>
        /// 进入出口区域
        /// </summary>
        public void EnterZone()
        {
            if (!string.IsNullOrWhiteSpace(scene))
            {
                TheGame.Get().TransitionToScene(scene, go_to_index); // 切换到指定场景
            }
        }

        private void OnTriggerEnter(Collider collision)
        {
            if (timer > 0.1f && collision.GetComponent<PlayerCharacter>()) // 碰撞体是玩家角色且计时器超过0.1秒
            {
                EnterZone(); // 触发进入出口区域事件
            }
        }

        /// <summary>
        /// 获取指定索引的出口区域
        /// </summary>
        /// <param name="index">出口区域的索引</param>
        /// <returns>找到的出口区域，如果没有找到则返回null</returns>
        public static ExitZone GetIndex(int index)
        {
            foreach (ExitZone exit in exit_list)
            {
                if (index == exit.entry_index)
                    return exit; // 返回指定索引的出口区域
            }
            return null;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// 游戏结束面板类，处理游戏结束后的操作，如加载游戏或开始新游戏
    /// </summary>
    public class GameOverPanel : UISlotPanel
    {
        private static GameOverPanel _instance; // 游戏结束面板的单例实例

        protected override void Awake()
        {
            base.Awake();
            _instance = this; // 设置单例实例
        }

        protected override void Start()
        {
            base.Start();
            // 初始化代码（如有需要可以在这里添加）
        }

        protected override void Update()
        {
            base.Update();
            // 更新代码（如有需要可以在这里添加）
        }

        /// <summary>
        /// 处理点击加载游戏按钮的事件
        /// </summary>
        public void OnClickLoad()
        {
            if (PlayerData.HasLastSave()) // 检查是否有最后的保存
                StartCoroutine(LoadRoutine()); // 如果有，启动加载游戏的协程
            else
                StartCoroutine(NewRoutine()); // 如果没有，启动新游戏的协程
        }

        /// <summary>
        /// 处理点击新游戏按钮的事件
        /// </summary>
        public void OnClickNew()
        {
            StartCoroutine(NewRoutine()); // 启动新游戏的协程
        }

        /// <summary>
        /// 加载游戏的协程
        /// </summary>
        /// <returns>协程的执行状态</returns>
        private IEnumerator LoadRoutine()
        {
            BlackPanel.Get().Show(); // 显示黑色面板作为过渡效果

            yield return new WaitForSeconds(1f); // 等待1秒

            TheGame.Load(); // 调用游戏的加载方法
        }

        /// <summary>
        /// 开始新游戏的协程
        /// </summary>
        /// <returns>协程的执行状态</returns>
        private IEnumerator NewRoutine()
        {
            BlackPanel.Get().Show(); // 显示黑色面板作为过渡效果

            yield return new WaitForSeconds(1f); // 等待1秒

            TheGame.NewGame(); // 调用游戏的新游戏方法
        }

        /// <summary>
        /// 获取游戏结束面板的单例实例
        /// </summary>
        /// <returns>游戏结束面板的单例实例</returns>
        public static GameOverPanel Get()
        {
            return _instance; // 返回单例实例
        }
    }
}

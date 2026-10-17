using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// 暂停面板类
    /// </summary>
    public class PausePanel : UISlotPanel
    {
        [Header("暂停面板")]
        public Image speaker_btn; // 音量按钮的图像
        public Sprite speaker_on; // 音量打开时的图标
        public Sprite speaker_off; // 音量关闭时的图标

        private static PausePanel _instance; // PausePanel 的单例实例

        protected override void Awake()
        {
            base.Awake();
            _instance = this; // 设置单例实例
        }

        protected override void Start()
        {
            base.Start();
            // 这里可以添加额外的初始化代码，如果有需要的话
        }

        protected override void Update()
        {
            base.Update();

            // 更新音量按钮的图标
            if(speaker_btn != null)
                speaker_btn.sprite = PlayerData.Get().master_volume > 0.1f ? speaker_on : speaker_off;
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            // 这里可以添加隐藏面板时的额外操作
        }

        /// <summary>
        /// 点击保存按钮的处理方法
        /// </summary>
        public void OnClickSave()
        {
            TheGame.Get().Save(); // 调用游戏管理器的保存方法
        }

        /// <summary>
        /// 点击加载按钮的处理方法
        /// </summary>
        public void OnClickLoad()
        {
            if (PlayerData.HasLastSave())
                StartCoroutine(LoadRoutine()); // 如果有最后保存，则启动加载协程
            else
                StartCoroutine(NewRoutine()); // 否则启动新游戏协程
        }

        /// <summary>
        /// 点击新游戏按钮的处理方法
        /// </summary>
        public void OnClickNew()
        {
            StartCoroutine(NewRoutine()); // 启动新游戏协程
        }

        /// <summary>
        /// 加载游戏的协程
        /// </summary>
        /// <returns>协程的IEnumerator</returns>
        private IEnumerator LoadRoutine()
        {
            BlackPanel.Get().Show(); // 显示黑色背景面板

            yield return new WaitForSeconds(1f); // 等待1秒

            TheGame.Load(); // 调用游戏管理器的加载方法
        }

        /// <summary>
        /// 新游戏的协程
        /// </summary>
        /// <returns>协程的IEnumerator</returns>
        private IEnumerator NewRoutine()
        {
            BlackPanel.Get().Show(); // 显示黑色背景面板

            yield return new WaitForSeconds(1f); // 等待1秒

            TheGame.NewGame(); // 调用游戏管理器的新游戏方法
        }

        /// <summary>
        /// 点击音量切换按钮的处理方法
        /// </summary>
        public void OnClickMusicToggle()
        {
            // 切换音量状态
            PlayerData.Get().master_volume = PlayerData.Get().master_volume > 0.1f ? 0f : 1f;
            TheAudio.Get().RefreshVolume(); // 刷新音量设置
        }

        /// <summary>
        /// 获取 PausePanel 的单例实例
        /// </summary>
        /// <returns>PausePanel 的实例</returns>
        public static PausePanel Get()
        {
            return _instance; // 返回单例实例
        }
    }

}

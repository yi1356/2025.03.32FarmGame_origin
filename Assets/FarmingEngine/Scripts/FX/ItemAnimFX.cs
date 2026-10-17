using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 当角色执行特定动画时，在已装备物品上播放动画效果
    /// </summary>
    public class ItemAnimFX : MonoBehaviour
    {
        public string anim; // 触发播放效果的动画名称
        public GameObject fx; // 要播放的特效对象

        private EquipItem item; // 已装备物品组件

        void Start()
        {
            fx.SetActive(false); // 在开始时将特效对象设为不活跃状态

            item = GetComponent<EquipItem>(); // 获取已装备物品组件

            // 获取角色并监听其触发动画事件
            PlayerCharacter character = item.GetCharacter();
            if (character != null)
                character.onTriggerAnim += OnAnim;
        }

        private void OnDestroy()
        {
            // 在销毁时取消监听角色的触发动画事件
            PlayerCharacter character = item.GetCharacter();
            if (character != null)
                character.onTriggerAnim -= OnAnim;
        }

        // 当触发特定动画时调用的方法
        private void OnAnim(string anim, float duration)
        {
            if (this.anim == anim) // 检查是否为要触发的动画
                StartCoroutine(RunFX(duration)); // 启动播放特效的协程
        }

        // 播放特效的协程方法
        private IEnumerator RunFX(float duration)
        {
            fx.SetActive(true); // 激活特效对象
            yield return new WaitForSeconds(duration); // 等待一段时间
            fx.SetActive(false); // 关闭特效对象
        }
    }
}
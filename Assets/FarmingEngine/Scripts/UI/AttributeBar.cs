using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{

    /// <summary>
    /// 显示某个属性的进度条
    /// </summary>
    [RequireComponent(typeof(ProgressBar))] // 确保此组件上附加了 ProgressBar 组件
    public class AttributeBar : MonoBehaviour
    {
        public AttributeType attribute; // 要显示的属性类型

        private PlayerUI parent_ui; // 父级 UI（玩家 UI）
        private ProgressBar bar; // 进度条组件

        void Awake()
        {
            parent_ui = GetComponentInParent<PlayerUI>(); // 获取父级 UI
            bar = GetComponent<ProgressBar>(); // 获取进度条组件
        }

        void Update()
        {
            PlayerCharacter character = GetPlayer(); // 获取玩家角色
            if (character != null)
            {
                // 设置进度条的最大值和当前值
                bar.SetMax(Mathf.RoundToInt(character.Attributes.GetAttributeMax(attribute)));
                bar.SetValue(Mathf.RoundToInt(character.Attributes.GetAttributeValue(attribute)));
            }
        }
		
        /// <summary>
        /// 获取玩家角色
        /// </summary>
        /// <returns>玩家角色对象</returns>
        public PlayerCharacter GetPlayer()
        {
            return parent_ui ? parent_ui.GetPlayer() : PlayerCharacter.GetFirst(); // 如果有父级 UI，则从父级 UI 获取角色；否则获取第一个玩家角色
        }
    }

}
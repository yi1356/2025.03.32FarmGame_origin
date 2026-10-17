using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// 显示角色等级和经验值的进度条
    /// </summary>
    [RequireComponent(typeof(ProgressBar))]
    public class XPBar : MonoBehaviour
    {
        public string level_id; // 等级ID，用于识别不同的等级系统
        public Text level_txt; // 显示等级的文本组件

        private PlayerUI parent_ui; // 当前面板的玩家UI
        private ProgressBar bar; // 进度条组件

        void Awake()
        {
            // 获取玩家UI和进度条组件
            parent_ui = GetComponentInParent<PlayerUI>();
            bar = GetComponent<ProgressBar>();
        }

        void Update()
        {
            // 获取玩家角色
            PlayerCharacter character = GetPlayer();
            if (character != null)
            {
                // 获取当前等级和经验值
                int level = character.Attributes.GetLevel(level_id);
                int xp = character.Attributes.GetXP(level_id);
                int xp_max = xp; // 经验值最大值，初始为当前经验值
                int xp_min = 0; // 经验值最小值，初始为0

                // 获取当前等级的等级数据
                LevelData current = LevelData.GetLevel(level_id, level);
                if(current != null)
                    xp_min = Mathf.Min(xp, current.xp_required); // 设置最小经验值

                // 获取下一级等级的等级数据
                LevelData next = LevelData.GetLevel(level_id, level + 1);
                if (next != null)
                    xp_max = Mathf.Max(xp, next.xp_required); // 设置最大经验值

                // 更新进度条的最小值、最大值和当前值
                bar.SetMin(xp_min);
                bar.SetMax(xp_max);
                bar.SetValue(xp);

                // 更新等级显示文本
                if (level_txt != null)
                    level_txt.text = "等级 " + level.ToString();
            }
        }

        // 获取玩家角色
        public PlayerCharacter GetPlayer()
        {
            return parent_ui ? parent_ui.GetPlayer() : PlayerCharacter.GetFirst();
        }
    }
}
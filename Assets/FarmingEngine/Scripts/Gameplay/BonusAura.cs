using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{

    /// <summary>
    /// When the character is near this, will gain bonus
    /// 当角色靠近此物体时，会获得奖励效果
    /// </summary>

    public class BonusAura : MonoBehaviour
    {
        public BonusEffectData effect;   // 奖励效果数据
        public float range = 5f;         // 范围


        private static List<BonusAura> aura_list = new List<BonusAura>(); // 静态奖励光环列表

        void Awake()
        {
            aura_list.Add(this); // 在Awake时将自身添加到奖励光环列表中
        }

        private void OnDestroy()
        {
            aura_list.Remove(this); // 在销毁时从奖励光环列表中移除自身
        }

        // 获取所有奖励光环的静态方法
        public static List<BonusAura> GetAll()
        {
            return aura_list;
        }
    }

}
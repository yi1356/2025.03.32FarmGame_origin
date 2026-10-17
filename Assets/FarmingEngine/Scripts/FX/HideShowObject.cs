using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 根据具有唯一标识符（UID）的对象隐藏或显示对象
    /// </summary>
    [RequireComponent(typeof(UniqueID))]
    public class HideShowObject : MonoBehaviour
    {
        public bool visible_at_start = true; // 是否在开始时可见

        private UniqueID unique_id; // 唯一标识符组件

        private void Awake()
        {
            unique_id = GetComponent<UniqueID>(); // 获取唯一标识符组件
        }

        void Start()
        {
            // 如果对象具有UID并且玩家数据中存在该对象的隐藏状态，则根据隐藏状态设置对象的活跃状态
            if (HasUID() && PlayerData.Get().HasHiddenState(unique_id.unique_id))
                gameObject.SetActive(!PlayerData.Get().IsObjectHidden(unique_id.unique_id));
            else
                gameObject.SetActive(visible_at_start); // 否则根据visible_at_start设置对象的活跃状态

            // 如果对象没有UID并且在游戏开始时，输出错误日志
            if (!HasUID() && Time.time < 0.1f)
                Debug.LogError("UID is empty on " + gameObject.name + ". It is required for HideShowObject");
        }

        /// <summary>
        /// 显示对象
        /// </summary>
        public void Show()
        {
            unique_id.Show(); // 调用唯一标识符组件的显示方法
        }

        /// <summary>
        /// 隐藏对象
        /// </summary>
        public void Hide()
        {
            unique_id.Hide(); // 调用唯一标识符组件的隐藏方法
        }

        /// <summary>
        /// 检查对象是否具有UID
        /// </summary>
        /// <returns>对象是否具有UID</returns>
        public bool HasUID()
        {
            return unique_id.HasUID(); // 返回唯一标识符组件的HasUID方法结果
        }
    }
}
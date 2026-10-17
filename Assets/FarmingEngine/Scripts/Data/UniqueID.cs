using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 帮助生成每个场景对象实例的唯一标识符（UID）的管理器。
    /// 唯一标识符主要在保存文件中使用，用于跟踪对象的状态。
    /// </summary>
    public class UniqueID : MonoBehaviour
    {
        public string uid_prefix; // 在此类型对象的每个ID前面添加的前缀，在预制件中设置

        [TextArea(1, 2)]
        public string unique_id; // 唯一标识符，应在预制件中为空。只应添加到场景中的实例。可以自动生成

        private Dictionary<string, string> sub_dict = new Dictionary<string, string>(); // 子标识符字典，存储子对象的唯一标识符

        private static Dictionary<string, UniqueID> dict_id = new Dictionary<string, UniqueID>(); // 静态字典，存储所有唯一标识符的映射关系

        void Awake()
        {
            if (!string.IsNullOrEmpty(unique_id))
            {
                dict_id[unique_id] = this; // 在唯一标识符不为空时，将其添加到静态字典中
            }
        }

        private void OnDestroy()
        {
            dict_id.Remove(unique_id); // 当对象销毁时，从静态字典中移除唯一标识符
        }

        private void Start()
        {
            if (HasUID() && PlayerData.Get().IsObjectHidden(unique_id))
                gameObject.SetActive(false); // 如果对象有唯一标识符并且已被隐藏，则禁用游戏对象

            if (!HasUID() && Time.time < 0.1f)
                Debug.LogWarning("UID is empty on " + gameObject.name + ". Make sure to generate UIDs with FarmingEngine->Generate UID"); // 如果唯一标识符为空并且时间小于0.1秒，则发出警告
        }

        /// <summary>
        /// 隐藏游戏对象，并在玩家数据中标记为隐藏
        /// </summary>
        public void Hide()
        {
            PlayerData.Get().HideObject(unique_id); // 在玩家数据中隐藏唯一标识符对应的对象
            gameObject.SetActive(false); // 禁用游戏对象
        }

        /// <summary>
        /// 显示游戏对象，并在玩家数据中标记为显示
        /// </summary>
        public void Show()
        {
            PlayerData.Get().ShowObject(unique_id); // 在玩家数据中显示唯一标识符对应的对象
            gameObject.SetActive(true); // 启用游戏对象
        }

        /// <summary>
        /// 设置唯一标识符
        /// </summary>
        public void SetUID(string uid)
        {
            if (dict_id.ContainsKey(unique_id))
                dict_id.Remove(unique_id); // 如果静态字典中存在当前唯一标识符，则移除
            unique_id = uid; // 设置新的唯一标识符
            if (!string.IsNullOrEmpty(unique_id))
                dict_id[unique_id] = this; // 将新的唯一标识符添加到静态字典中
            sub_dict.Clear(); // 清空子标识符字典
        }

        /// <summary>
        /// 检查是否有唯一标识符
        /// </summary>
        public bool HasUID()
        {
            return !string.IsNullOrEmpty(unique_id); // 返回唯一标识符是否不为空
        }

        /// <summary>
        /// 生成唯一标识符
        /// </summary>
        public void GenerateUID()
        {
            SetUID(uid_prefix + GenerateUniqueID()); // 生成唯一标识符并设置
        }

        /// <summary>
        /// 在编辑器模式下生成唯一标识符
        /// </summary>
        public void GenerateUIDEditor()
        {
            unique_id = uid_prefix + GenerateUniqueID(); // 在编辑器模式下生成唯一标识符，不保存到静态字典中
        }

        /// <summary>
        /// 获取子标识符
        /// </summary>
        public string GetSubUID(string sub_tag)
        {
            if (sub_dict.ContainsKey(sub_tag))
                return sub_dict[sub_tag]; // 如果子标识符字典中包含指定的子标识符，则返回对应的子标识符
            if (string.IsNullOrEmpty(unique_id))
                return ""; // 如果唯一标识符为空，则返回空字符串

            string sub_uid = unique_id + "_" + sub_tag; // 否则生成新的子标识符
            sub_dict[sub_tag] = sub_uid; // 将子标识符添加到字典中
            return sub_uid;
        }
		
        /// <summary>
        /// 移除所有子标识符
        /// </summary>
        public void RemoveAllSubUIDs()
        {
            PlayerData pdata = PlayerData.Get(); // 获取玩家数据实例
            foreach (KeyValuePair<string, string> pair in sub_dict)
            {
                string subuid = pair.Value;
                pdata.RemoveAllCustom(subuid); // 在玩家数据中移除所有自定义数据
            }
            sub_dict.Clear(); // 清空子标识符字典
        }

        // 下面是对玩家数据的快捷操作方法，使用子标识符作为参数

        public void SetCustomInt(string sub_id, int val) { PlayerData.Get().SetCustomInt(GetSubUID(sub_id), val); }
        public void SetCustomFloat(string sub_id, float val) { PlayerData.Get().SetCustomFloat(GetSubUID(sub_id), val); }
        public void SetCustomString(string sub_id, string val) { PlayerData.Get().SetCustomString(GetSubUID(sub_id), val); }

        public int GetCustomInt(string sub_id) { return PlayerData.Get().GetCustomInt(GetSubUID(sub_id)); }
        public float GetCustomFloat(string sub_id) { return PlayerData.Get().GetCustomFloat(GetSubUID(sub_id)); }
        public string GetCustomString(string sub_id) { return PlayerData.Get().GetCustomString(GetSubUID(sub_id)); }

        public bool HasCustomInt(string sub_id) { return PlayerData.Get().HasCustomInt(GetSubUID(sub_id)); }
        public bool HasCustomFloat(string sub_id) { return PlayerData.Get().HasCustomFloat(GetSubUID(sub_id)); }
        public bool HasCustomString(string sub_id) { return PlayerData.Get().HasCustomString(GetSubUID(sub_id)); }

        /// <summary>
        /// 生成指定长度范围内的随机唯一标识符
        /// </summary>
        public static string GenerateUniqueID(int min = 11, int max = 17)
        {
            int length = Random.Range(min, max); // 随机生成长度
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"; // 可用字符集
            string unique_id = "";
            for (int i = 0; i < length; i++)
            {
                unique_id += chars[Random.Range(0, chars.Length - 1)]; // 随机选择字符组成唯一标识符
            }
            return unique_id;
        }

        /// <summary>
        /// 为所有对象生成唯一标识符
        /// </summary>
        public static void GenerateAll(UniqueID[] objs)
        {
            HashSet<string> existing_ids = new HashSet<string>(); // 存储现有的唯一标识符集合

            foreach (UniqueID uid_obj in objs)
            {
                if (uid_obj.unique_id != "")
                {
                    if (existing_ids.Contains(uid_obj.unique_id))
                        uid_obj.unique_id = ""; // 如果已存在该唯一标识符，则清空
                    else
                        existing_ids.Add(uid_obj.unique_id); // 否则将其添加到集合中
                }
            }

            foreach (UniqueID uid_obj in objs)
            {
                if (uid_obj.unique_id == "")
                {
                    // 生成新的唯一标识符
                    string new_id = "";
                    while (new_id == "" || existing_ids.Contains(new_id))
                    {
                        new_id = UniqueID.GenerateUniqueID();
                    }

                    // 添加新的唯一标识符
                    uid_obj.unique_id = uid_obj.uid_prefix + new_id;
                    existing_ids.Add(new_id);

#if UNITY_EDITOR
                    if (Application.isEditor && !Application.isPlaying)
                        UnityEditor.EditorUtility.SetDirty(uid_obj); // 在编辑器模式下，标记对象为已修改，确保修改被保存
#endif
                }
            }
        }

        /// <summary>
        /// 清除所有对象的唯一标识符
        /// </summary>
        public static void ClearAll(UniqueID[] objs)
        {
            foreach (UniqueID uid_obj in objs)
            {
                uid_obj.unique_id = ""; // 清空唯一标识符

#if UNITY_EDITOR
                if (Application.isEditor && !Application.isPlaying)
                    UnityEditor.EditorUtility.SetDirty(uid_obj); // 在编辑器模式下，标记对象为已修改，确保修改被保存
#endif
            }
        }

        /// <summary>
        /// 检查是否存在指定的唯一标识符
        /// </summary>
        public static bool HasID(string id)
        {
            return dict_id.ContainsKey(id); // 返回静态字典中是否包含指定的唯一标识符
        }

        /// <summary>
        /// 根据唯一标识符获取游戏对象
        /// </summary>
        public static GameObject GetByID(string id)
        {
            if (dict_id.ContainsKey(id))
            {
                return dict_id[id].gameObject; // 根据唯一标识符从静态字典中获取游戏对象
            }
            return null;
        }
    }
}

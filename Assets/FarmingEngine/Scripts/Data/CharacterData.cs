using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 角色数据文件[Z：比如松鼠、蝴蝶之类的，好像character脚本中需要引用]
    /// </summary>

    [CreateAssetMenu(fileName = "CharacterData", menuName = "FarmingEngine/CharacterData", order = 5)]
    public class CharacterData : CraftData
    {
        [Header("--- CharacterData ------------------")]

        public GameObject character_prefab; // 构建角色时生成的预制体(Z 这个应该就是角色模型（蝴蝶等之类的）了)

        [Header("引用数据")]
        public ItemData take_item_data; // 可获取的物品数据（这里暂时还看不懂好像和动物产小动物有关系）

        private static List<CharacterData> character_data = new List<CharacterData>(); // 角色数据列表

        public static new void Load(string folder = "")
        {
            character_data.Clear();
            character_data.AddRange(Resources.LoadAll<CharacterData>(folder));
        }

        public new static CharacterData Get(string character_id)
        {
            foreach (CharacterData item in character_data)
            {
                if (item.id == character_id)
                    return item;
            }
            return null;
        }

        public new static List<CharacterData> GetAll()
        {
            return character_data;
        }
    }

}
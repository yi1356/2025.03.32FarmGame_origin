using System.Collections;
using UnityEngine;

namespace FarmingEngine.EditorTool
{
    /// <summary>
    /// 创建对象编辑器脚本的默认设置文件
    /// </summary>
    
    [CreateAssetMenu(fileName = "CreateObjectSettings", menuName = "FarmingEngine/CreateObjectSettings", order = 100)]
    public class CreateObjectSettings : ScriptableObject
    {

        [Header("保存文件夹")]
        public string prefab_folder = "FarmingEngine/Prefabs";  // 预制件文件夹路径
        public string prefab_equip_folder = "FarmingEngine/Prefabs/Equip";  // 装备预制件文件夹路径
        public string items_folder = "FarmingEngine/Resources/Items";  // 物品资源文件夹路径
        public string constructions_folder = "FarmingEngine/Resources/Constructions";  // 建筑资源文件夹路径
        public string plants_folder = "FarmingEngine/Resources/Plants";  // 植物资源文件夹路径
        public string characters_folder = "FarmingEngine/Resources/Characters";  // 角色资源文件夹路径

        [Header("默认数值")]
        public Material outline;  // 轮廓材质
        public GameObject death_fx;  // 死亡特效
        public AudioClip craft_audio;  // 制作音效
        public GameObject take_fx;  // 拾取特效
        public AudioClip take_audio;  // 拾取音效
        public AudioClip attack_audio;  // 攻击音效
        public AudioClip build_audio;  // 建造音效
        public GameObject build_fx;  // 建造特效
        public SAction[] item_actions;  // 物品行为列表
        public SAction equip_action;  // 装备行为
        public SAction eat_action;  // 食用行为

    }

}
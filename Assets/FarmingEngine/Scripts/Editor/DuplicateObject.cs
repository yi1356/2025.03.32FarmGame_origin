using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace FarmingEngine.EditorTool
{
    /// <summary>
    /// 使用此工具轻松复制CraftData对象及其所有链接
    /// </summary>

    public class DuplicateObject : ScriptableWizard
    {
        [Header("New Object")]
        public CraftData source; // 源对象
        public string object_title; // 新对象的标题

        private Dictionary<int, string> copied_prefabs = new Dictionary<int, string>(); // 已复制的预制体字典

        [MenuItem("Farming Engine/Duplicate Object", priority = 2)]
        static void ScriptableWizardMenu()
        {
            ScriptableWizard.DisplayWizard<DuplicateObject>("Duplicate Object", "Duplicate");
        }

        void DoDuplicateObject()
        {
            if (source == null)
            {
                Debug.LogError("A source must be assigned!"); // 源对象不能为空
                return;
            }

            if (string.IsNullOrEmpty(object_title.Trim()))
            {
                Debug.LogError("Title can't be blank"); // 标题不能为空
                return;
            }
;
            copied_prefabs.Clear(); // 清空已复制的预制体字典

            if (source is ItemData)
            {
                ItemData nitem = CopyAsset<ItemData>((ItemData)source, object_title); // 复制ItemData对象

                if (nitem != null && nitem.item_prefab != null)
                {
                    GameObject nprefab = CopyPrefab(nitem.item_prefab, object_title); // 复制对应的预制体
                    nitem.item_prefab = nprefab;
                    Item item = nprefab.GetComponent<Item>();
                    if (item != null)
                        item.data = nitem;
                }

                if (nitem != null && nitem.equipped_prefab != null)
                {
                    GameObject nprefab = CopyPrefab(nitem.equipped_prefab, object_title + "Equip"); // 复制对应的装备预制体
                    nitem.equipped_prefab = nprefab;
                    EquipItem item = nprefab.GetComponent<EquipItem>();
                    if (item != null)
                        item.data = nitem;
                }

                Selection.activeObject = nitem;
            }

            if (source is CharacterData)
            {
                CharacterData nitem = CopyAsset<CharacterData>((CharacterData)source, object_title); // 复制CharacterData对象

                if (nitem != null && nitem.character_prefab != null)
                {
                    GameObject nprefab = CopyPrefab(nitem.character_prefab, object_title); // 复制对应的预制体
                    nitem.character_prefab = nprefab;
                    Character character = nprefab.GetComponent<Character>();
                    if (character != null)
                        character.data = nitem;
                }

                Selection.activeObject = nitem;
            }

            if (source is ConstructionData)
            {
                ConstructionData nitem = CopyAsset<ConstructionData>((ConstructionData)source, object_title); // 复制ConstructionData对象

                if (nitem != null && nitem.construction_prefab != null)
                {
                    GameObject nprefab = CopyPrefab(nitem.construction_prefab, object_title); // 复制对应的预制体
                    nitem.construction_prefab = nprefab;
                    Construction construct = nprefab.GetComponent<Construction>();
                    if (construct != null)
                        construct.data = nitem;
                }

                Selection.activeObject = nitem;
            }

            if (source is PlantData)
            {
                PlantData nitem = CopyAsset<PlantData>((PlantData)source, object_title); // 复制PlantData对象

                if (nitem != null)
                {
                    if (nitem.growth_stage_prefabs.Length == 0 && nitem.plant_prefab != null)
                    {
                        GameObject nprefab = CopyPrefab(nitem.plant_prefab, object_title); // 复制对应的预制体
                        nitem.plant_prefab = nprefab;
                        Plant plant = nprefab.GetComponent<Plant>();
                        if (plant != null)
                            plant.data = nitem;
                    }
                    else
                    {
                        for (int i = 0; i < nitem.growth_stage_prefabs.Length; i++)
                        {
                            GameObject sprefab = CopyPrefab(nitem.growth_stage_prefabs[i], object_title + "S" + (i + 1)); // 复制对应的生长阶段预制体
                            nitem.growth_stage_prefabs[i] = sprefab;
                            nitem.plant_prefab = sprefab; 
                            Plant plant_stage = sprefab.GetComponent<Plant>();
                            if (plant_stage != null)
                                plant_stage.data = nitem;
                        }
                    }
                }

                Selection.activeObject = nitem;
            }

            AssetDatabase.SaveAssets(); // 保存所有改动
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); // 标记场景为已修改状态

        }

        private GameObject CopyPrefab(GameObject prefab, string title)
        {
            if (prefab != null)
            {
                // 快速访问已复制的预制体
                if (copied_prefabs.ContainsKey(prefab.GetInstanceID()))
                {
                    string cpath = copied_prefabs[prefab.GetInstanceID()];
                    GameObject cprefab = AssetDatabase.LoadAssetAtPath<GameObject>(cpath);
                    if(cprefab != null)
                        return cprefab;
                }

                // 否则进行复制
                string path = AssetDatabase.GetAssetPath(prefab);
                string folder = Path.GetDirectoryName(path);
                string ext = Path.GetExtension(path);
                string filename = title.Replace(" ", "").Replace("/", "");
                string npath = folder + "/" + filename + ext;

                if (!Directory.Exists(folder))
                {
                    Debug.LogError("Folder does not exist: " + folder); // 文件夹不存在
                    return null;
                }

                if (File.Exists(npath))
                {
                    Debug.LogError("File already exists: " + npath); // 文件已存在
                    return null;
                }

                AssetDatabase.CopyAsset(path, npath); // 复制预制体文件
                GameObject nprefab = AssetDatabase.LoadAssetAtPath<GameObject>(npath);
                if (nprefab != null)
                {
                    nprefab.name = filename;
                    copied_prefabs[prefab.GetInstanceID()] = npath;
                    return nprefab;
                }
            }
            return null;
        }

        private T CopyAsset<T>(T asset, string title) where T : CraftData
        {
            if (asset != null)
            {
                string path = AssetDatabase.GetAssetPath(asset);
                string folder = Path.GetDirectoryName(path);
                string ext = Path.GetExtension(path);
                string filename = title.Replace(" ", "").Replace("/", "");
                string fileid = title.Trim().Replace(" ", "_").ToLower();
                string npath = folder + "/" + filename + ext;

                if (!Directory.Exists(folder))
                {
                    Debug.LogError("Folder does not exist: " + folder); // 文件夹不存在
                    return null;
                }

                if (File.Exists(npath))
                {
                    Debug.LogError("File already exists: " + npath); // 文件已存在
                    return null;
                }

                AssetDatabase.CopyAsset(path, npath); // 复制资产文件
                T nasset = AssetDatabase.LoadAssetAtPath<T>(npath);
                if (nasset != null)
                {
                    nasset.name = filename;
                    nasset.title = title;
                    nasset.id = fileid;
                    return nasset;
                }
            }
            return null;
        }

        void OnWizardCreate()
        {
            DoDuplicateObject();
        }

        void OnWizardUpdate()
        {
            helpString = "Use this tool to duplicate a prefab and its data file."; // 使用此工具复制预制体及其数据文件
        }
    }

}

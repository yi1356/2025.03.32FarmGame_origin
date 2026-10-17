using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace FarmingEngine
{
   
    /// <summary>
    /// PlayerData 是主要的存档文件数据脚本。该脚本中包含的所有内容都将被保存。
    /// 脚本还包含许多函数，用于轻松访问保存的数据。确保调用 TheGame.Get().Save() 将数据写入磁盘上的文件中。
    /// 最新的保存文件将在启动游戏时自动加载。
    /// </summary>

    [System.Serializable]
    public class PlayerData
    {
        public string filename; // 存档文件名
        public string version; // 游戏版本号
        public DateTime last_save; // 上次保存时间

        //-------------------

        public int world_seed = 0; // 世界随机种子
        public string current_scene = ""; // 当前加载的场景
        public int current_entry_index = 0; // 当前条目索引，-1 表示当前位置，0 表示默认场景位置，>0 表示匹配的条目索引
        
        public int day = 0; // 游戏天数
        public float day_time = 0f; // 当天时间，0 = 午夜，24 = 一天结束
        public float play_time = 0f; // 游戏总玩耍时间，以秒为单位（Z：就是记录我们一共玩了这个游戏多长时间）
        public bool new_day = false; // 新的一天标志（Z：？？？？？？）

        public float master_volume = 1f; // 主音量
        public float music_volume = 1f; // 音乐音量
        public float sfx_volume = 1f; // 音效音量

        public Dictionary<int, PlayerCharacterData> player_characters = new Dictionary<int, PlayerCharacterData>();// 玩家角色数据
        public Dictionary<string, InventoryData> inventories = new Dictionary<string, InventoryData>();// 物品库存数据[Z：这里应该是不同玩家id存不同inventoryData数据，注意：包括了背包、装备、箱子几种类型的，也就是一个玩家也有好几个InventoryData]

        public Dictionary<string, int> unique_ids = new Dictionary<string, int>(); // 唯一整数
        public Dictionary<string, float> unique_floats = new Dictionary<string, float>();// 唯一浮点数
        public Dictionary<string, string> unique_strings = new Dictionary<string, string>();// 唯一字符串
        public Dictionary<string, int> removed_objects = new Dictionary<string, int>(); // 已移除对象
        public Dictionary<string, int> hidden_objects = new Dictionary<string, int>(); // 隐藏对象

        public Dictionary<string, DroppedItemData> dropped_items = new Dictionary<string, DroppedItemData>();// 掉落物品数据
        public Dictionary<string, BuiltConstructionData> built_constructions = new Dictionary<string, BuiltConstructionData>();// 建造物数据
        public Dictionary<string, SowedPlantData> sowed_plants = new Dictionary<string, SowedPlantData>();// 种植物数据
        public Dictionary<string, TrainedCharacterData> trained_characters = new Dictionary<string, TrainedCharacterData>();// 训练角色数据
        public Dictionary<string, SpawnedData> spawned_objects = new Dictionary<string, SpawnedData>(); // 生成的对象数据
        public Dictionary<string, SceneObjectData> scene_objects = new Dictionary<string, SceneObjectData>();  // 场景中的对象数据
        public Dictionary<string, RegrowthData> world_regrowth = new Dictionary<string, RegrowthData>();// 世界再生数据
        
        //-------------------

        private static string file_loaded = ""; // 已加载的文件名
        private static PlayerData player_data = null; // 当前玩家数据

        public const string last_save_id = "last_save_farming"; // 最后保存的标识
        public const string extension = ".farming"; // 文件扩展名

        public PlayerData(string name)
        {
            filename = name; // 设置文件名
            version = Application.version; // 设置版本号
            last_save = DateTime.Now; // 设置当前时间为上次保存时间

            day = 1; // 从第一天开始
            day_time = 6f; // 游戏从早上6点开始
            new_day = true; // 新的一天

            master_volume = 1f; // 主音量默认值
            music_volume = 1f; // 音乐音量默认值
            sfx_volume = 1f; // 音效音量默认值
        }

        public void FixData()
        {
            //Fix data to make sure old save files compatible with new game version
            if (unique_ids == null)
                unique_ids = new Dictionary<string, int>();
            if (unique_floats == null)
                unique_floats = new Dictionary<string, float>();
            if (unique_strings == null)
                unique_strings = new Dictionary<string, string>();

            if (player_characters == null)
                player_characters = new Dictionary<int, PlayerCharacterData>();
            if (inventories == null)
                inventories = new Dictionary<string, InventoryData>();

            if (dropped_items == null)
                dropped_items = new Dictionary<string, DroppedItemData>();
            if (removed_objects == null)
                removed_objects = new Dictionary<string, int>();
            if (hidden_objects == null)
                hidden_objects = new Dictionary<string, int>();
            if (built_constructions == null)
                built_constructions = new Dictionary<string, BuiltConstructionData>();
            if (sowed_plants == null)
                sowed_plants = new Dictionary<string, SowedPlantData>();
            if (trained_characters == null)
                trained_characters = new Dictionary<string, TrainedCharacterData>();

            if (spawned_objects == null)
                spawned_objects = new Dictionary<string, SpawnedData>();
            if (scene_objects == null)
                scene_objects = new Dictionary<string, SceneObjectData>();
            if (world_regrowth == null)
                world_regrowth = new Dictionary<string, RegrowthData>();

            foreach (KeyValuePair<int, PlayerCharacterData> character in player_characters)
                character.Value.FixData();

            foreach (KeyValuePair<string, InventoryData> inventory in inventories)
                inventory.Value.FixData();

        }

        //-------- 掉落物品 --------

        public DroppedItemData AddDroppedItem(string item_id, string scene, Vector3 pos, int quantity, float durability)
        {
            string uid = UniqueID.GenerateUniqueID();
            return AddDroppedItem(item_id, scene, pos, quantity, durability, uid);
        }

        public DroppedItemData AddDroppedItem(string item_id, string scene, Vector3 pos, int quantity, float durability, string uid)
        {
            RemoveDroppedItem(uid);// 添加之前先移除旧的同一 UID 的掉落物品

            DroppedItemData ditem = new DroppedItemData();
            ditem.uid = uid;
            ditem.item_id = item_id;
            ditem.scene = scene;
            ditem.pos = pos;
            ditem.quantity = quantity;
            ditem.durability = durability;
            dropped_items[ditem.uid] = ditem;// 添加新的掉落物品
            return ditem;
        }

        public void RemoveDroppedItem(string uid)
        {
            if (dropped_items.ContainsKey(uid))
                dropped_items.Remove(uid);// 移除指定 UID 的掉落物品
        }

        public DroppedItemData GetDroppedItem(string uid)
        {
            if (dropped_items.ContainsKey(uid))
                return dropped_items[uid]; // 获取指定 UID 的掉落物品
            return null;
        }

        //---- 建造物和种植物和角色 ----

        public BuiltConstructionData AddConstruction(string construct_id, string scene, Vector3 pos, Quaternion rot, float durability)
        {
            BuiltConstructionData citem = new BuiltConstructionData();
            citem.uid = UniqueID.GenerateUniqueID();
            citem.construction_id = construct_id;
            citem.scene = scene;
            citem.pos = pos;
            citem.rot = rot;
            citem.durability = durability;
            built_constructions[citem.uid] = citem;// 添加新建造物
            return citem;
        }

        public void RemoveConstruction(string uid)
        {
            if (built_constructions.ContainsKey(uid))
                built_constructions.Remove(uid);
        }

        public BuiltConstructionData GetConstructed(string uid)
        {
            if (built_constructions.ContainsKey(uid))
                return built_constructions[uid];
            return null;
        }

        public SowedPlantData AddPlant(string plant_id, string scene, Vector3 pos, Quaternion rot, int stage)
        {
            SowedPlantData citem = new SowedPlantData();
            citem.uid = UniqueID.GenerateUniqueID();
            citem.plant_id = plant_id;
            citem.scene = scene;
            citem.pos = pos;
            citem.rot = rot;
            citem.growth_stage = stage;
            sowed_plants[citem.uid] = citem;
            return citem;
        }

        public void GrowPlant(string plant_uid, int stage)
        {
            if (sowed_plants.ContainsKey(plant_uid))
                sowed_plants[plant_uid].growth_stage = stage;
        }

        public void RemovePlant(string uid)
        {
            if (sowed_plants.ContainsKey(uid))
                sowed_plants.Remove(uid);
        }

        public SowedPlantData GetSowedPlant(string uid)
        {
            if (sowed_plants.ContainsKey(uid))
                return sowed_plants[uid];
            return null;
        }

        public TrainedCharacterData AddCharacter(string character_id, string scene, Vector3 pos, Quaternion rot)
        {
            TrainedCharacterData citem = new TrainedCharacterData();
            citem.uid = UniqueID.GenerateUniqueID();
            citem.character_id = character_id;
            citem.scene = scene;
            citem.pos = pos;
            citem.rot = rot;
            trained_characters[citem.uid] = citem;
            return citem;
        }

        public void RemoveCharacter(string uid)
        {
            if (trained_characters.ContainsKey(uid))
                trained_characters.Remove(uid);
        }

        public TrainedCharacterData GetCharacter(string uid)
        {
            if (trained_characters.ContainsKey(uid))
                return trained_characters[uid];
            return null;
        }

        public void SetCharacterPosition(string uid, string scene, Vector3 pos, Quaternion rot)
        {
            TrainedCharacterData cdata = GetCharacter(uid);
            if (cdata != null)
            {
                cdata.scene = scene;
                cdata.pos = pos;
                cdata.rot = rot;
            }
            else
            {
                //未生成的角色被保存为场景对象
                SceneObjectData sobj = GetOrCreateSceneObject(uid, scene);
                if (sobj != null)
                {
                    sobj.pos = pos;
                    sobj.rot = rot;
                }
            }
        }

        public SceneObjectData GetOrCreateSceneObject(string uid, string scene)
        {
            SceneObjectData sobj = GetSceneObject(uid);
            if (sobj != null && sobj.scene == scene)
                return sobj;
            
            if (!string.IsNullOrEmpty(uid))
            {
                SceneObjectData nobj = new SceneObjectData();
                nobj.uid = uid;
                nobj.scene = scene;
                scene_objects[uid] = nobj;
                return nobj;
            }
            return null;
        }

        public SceneObjectData GetSceneObject(string uid)
        {
            if (scene_objects.ContainsKey(uid))
                return scene_objects[uid];
            return null;
        }

        public SpawnedData AddSpawnedObject(string id, string scene, Vector3 pos, Quaternion rot, float scale)
        {
            SpawnedData sdata = new SpawnedData();
            sdata.id = id;
            sdata.uid = UniqueID.GenerateUniqueID();
            sdata.scene = scene;
            sdata.pos = pos;
            sdata.rot = rot;
            sdata.scale = scale;
            spawned_objects[sdata.uid] = sdata;
            return sdata;
        }

        public void RemoveSpawnedObject(string uid)
        {
            if (spawned_objects.ContainsKey(uid))
                spawned_objects.Remove(uid);
        }

        public SpawnedData GetSpawnedObject(string uid)
        {
            if (spawned_objects.ContainsKey(uid))
                return spawned_objects[uid];
            return null;
        }

        //---- 世界再创造 -----

        public void AddWorldRegrowth(string uid, RegrowthData data)
        {
            world_regrowth[uid] = data;
        }

        public void RemoveWorldRegrowth(string uid)
        {
            if (world_regrowth.ContainsKey(uid))
                world_regrowth.Remove(uid);
        }

        public RegrowthData GetWorldRegrowth(string uid)
        {
            if (world_regrowth.ContainsKey(uid))
                return world_regrowth[uid];
            return null;
        }

        public bool HasWorldRegrowth(string uid)
        {
            return world_regrowth.ContainsKey(uid);
        }

        //---- 可破坏物体 -----

        public void RemoveObject(string uid)
        {
            if (!string.IsNullOrEmpty(uid))
                removed_objects[uid] = 1;
        }

        public void ClearRemovedObject(string uid) {
            if (removed_objects.ContainsKey(uid))
                removed_objects.Remove(uid);
        }

        public bool IsObjectRemoved(string uid)
        {
            if (removed_objects.ContainsKey(uid))
                return removed_objects[uid] > 0;
            return false;
        }

        //----- 显示或者隐藏一般物体 ------

        public void HideObject(string uid)
        {
            if (!string.IsNullOrEmpty(uid))
                hidden_objects[uid] = 1;
        }

        public void ShowObject(string uid)
        {
            if (!string.IsNullOrEmpty(uid))
                hidden_objects[uid] = 0;
        }

        public bool IsObjectHidden(string uid)
        {
            if (hidden_objects.ContainsKey(uid))
                return hidden_objects[uid] > 0;
            return false;
        }

        public bool HasHiddenState(string uid)
        {
            return hidden_objects.ContainsKey(uid);
        }

        // ---- 唯一标识符（自定义数据） ----
        public void SetCustomInt(string unique_id, int val)
        {
            if (!string.IsNullOrEmpty(unique_id))
                unique_ids[unique_id] = val;
        }

        public void RemoveCustomInt(string unique_id)
        {
            if (unique_ids.ContainsKey(unique_id))
                unique_ids.Remove(unique_id);
        }

        public int GetCustomInt(string unique_id)
        {
            if (unique_ids.ContainsKey(unique_id))
                return unique_ids[unique_id];
            return 0;
        }

        public bool HasCustomInt(string unique_id)
        {
            return unique_ids.ContainsKey(unique_id);
        }

        public void SetCustomFloat(string unique_id, float val)
        {
            if (!string.IsNullOrEmpty(unique_id))
                unique_floats[unique_id] = val;
        }

        public void RemoveCustomFloat(string unique_id)
        {
            if (unique_floats.ContainsKey(unique_id))
                unique_floats.Remove(unique_id);
        }

        public float GetCustomFloat(string unique_id)
        {
            if (unique_floats.ContainsKey(unique_id))
                return unique_floats[unique_id];
            return 0;
        }

        public bool HasCustomFloat(string unique_id)
        {
            return unique_floats.ContainsKey(unique_id);
        }

        public void SetCustomString(string unique_id, string val)
        {
            if (!string.IsNullOrEmpty(unique_id))
                unique_strings[unique_id] = val;
        }

        public void RemoveCustomString(string unique_id)
        {
            if (unique_strings.ContainsKey(unique_id))
                unique_strings.Remove(unique_id);
        }

        public string GetCustomString(string unique_id)
        {
            if (unique_strings.ContainsKey(unique_id))
                return unique_strings[unique_id];
            return "";
        }

        public bool HasCustomString(string unique_id)
        {
            return unique_strings.ContainsKey(unique_id);
        }
		
		public void RemoveAllCustom(string unique_id)
        {
            RemoveCustomString(unique_id);
            RemoveCustomFloat(unique_id);
            RemoveCustomInt(unique_id);
        }

        // ---- 多库存物品操作 -----

        public void SwapInventoryItems(InventoryData inventory1, int slot1, InventoryData inventory2, int slot2) 
        {
            InventoryItemData invt_slot1 = inventory1.GetInventoryItem(slot1);
            InventoryItemData invt_slot2 = inventory2.GetInventoryItem(slot2);
            ItemData idata1 = ItemData.Get(invt_slot1?.item_id);
            ItemData idata2 = ItemData.Get(invt_slot2?.item_id);

            if (idata1 && idata1.IsBag() && inventory2.type == InventoryType.Bag)
                return; //Cant put bag into bag
            if (idata2 && idata2.IsBag() && inventory1.type == InventoryType.Bag)
                return; //Cant put bag into bag

            inventory1.items[slot1] = invt_slot2;
            inventory2.items[slot2] = invt_slot1;

            if (invt_slot2 == null)
                inventory1.items.Remove(slot1);
            if (invt_slot1 == null)
                inventory2.items.Remove(slot2);
        }

        public void CombineInventoryItems(InventoryData inventory1, int slot1, InventoryData inventory2, int slot2)
        {
            InventoryItemData invt_slot1 = inventory1.GetInventoryItem(slot1);
            InventoryItemData invt_slot2 = inventory2.GetInventoryItem(slot2);

            if (invt_slot1.item_id == invt_slot2.item_id) {
                inventory1.RemoveItemAt(slot1, invt_slot1.quantity);
                inventory2.AddItemAt(invt_slot1.item_id, slot2, invt_slot1.quantity, invt_slot1.durability, invt_slot1.uid);
            }
        }

        // ---- 通用方法 ------

        public InventoryData GetInventory(InventoryType type, string inventory_uid)
        {
            InventoryData sdata = null;
            if (!string.IsNullOrEmpty(inventory_uid))
            {
                if (inventories.ContainsKey(inventory_uid))
                {
                    sdata = inventories[inventory_uid];
                }
                else
                {
                    //Create new if dont exist
                    sdata = new InventoryData(type, inventory_uid);
                    inventories[inventory_uid] = sdata;
                }
            }
            return sdata;
        }
        /// <summary>
        /// Z：其实这两个方法，是根据传入的type和player_id  得到指定玩家的指定类型的背包数据的ID，再从字典中获取************************************************************************
        /// </summary>
        /// <param name="type">要获取那种 库存数据</param>
        /// <param name="player_id">玩家Id，区分是哪一个玩家的</param>
        /// <returns></returns>
        public InventoryData GetInventory(InventoryType type, int player_id)
        {
            string uid = GetPlayerUID(player_id);
            return GetInventory(type, uid);
        }

        public InventoryData GetEquipInventory(InventoryType type, int player_id)
        {
            string uid = GetPlayerEquipUID(player_id);
            return GetInventory(type, uid);
        }

        public bool HasInventory(int player_id)
        {
            return HasInventory(GetPlayerUID(player_id));
        }

        public bool HasEquipInventory(int player_id)
        {
            return HasInventory(GetPlayerEquipUID(player_id));
        }

        public bool HasInventory(string inventory_uid)
        {
            if (!string.IsNullOrEmpty(inventory_uid))
            {
                if (inventories.ContainsKey(inventory_uid))
                    return true;
            }
            return false;
        }

        public PlayerCharacterData GetPlayerCharacter(int player_id)
        {
            PlayerCharacterData cdata;
            if (player_characters.ContainsKey(player_id))
            {
                cdata = player_characters[player_id];
            }
            else
            {
                //Create new if dont exist
                cdata = new PlayerCharacterData(player_id);
                player_characters[player_id] = cdata;
            }
            return cdata;
        }

        public string GetPlayerUID(int player_id)
        {
            return "player_" + player_id;
        }

        public string GetPlayerEquipUID(int player_id)
        {
            return "player_equip_" + player_id;
        }

        public bool IsWorldGenerated()
        {
            return world_seed != 0;
        }

        public bool IsNewGame()
        {
            return play_time < 0.0001f;
        }

        public float GetTotalTime()
        {
            return (day-1) * 24f + day_time;
        }

        //--- Save / load -----

        public bool IsVersionValid()
        {
            return version == Application.version;
        }

        public void Save()
        {
            Save(file_loaded, this);
        }

        public static void Save(string filename, PlayerData data)
        {
            if (!string.IsNullOrEmpty(filename) && data != null)
            {
                data.filename = filename;
                data.last_save = DateTime.Now;
                data.version = Application.version;
                player_data = data;
                file_loaded = filename;

                SaveTool.SaveFile<PlayerData>(filename + extension, data);
                SetLastSave(filename);
            }
        }

        public static void NewGame()
        {
            NewGame(GetLastSave()); //default name
        }

        //在调用 NewGame 后应该重新加载场景
        public static PlayerData NewGame(string filename)
        {
            file_loaded = filename;
            player_data = new PlayerData(filename);
            player_data.FixData();
            return player_data;
        }

        public static PlayerData Load(string filename)
        {
            if (player_data == null || file_loaded != filename)
            {
                player_data = SaveTool.LoadFile<PlayerData>(filename + extension);
                if (player_data != null)
                {
                    file_loaded = filename;
                    player_data.FixData();
                }
            }
            return player_data;
        }

        public static PlayerData LoadLast()//Z: 现在的playerData数据是从本地的一个二进制文件加载的
        {
            return AutoLoad(GetLastSave());
        }

        //如果找到存档则加载，否则开始新游戏
        public static PlayerData AutoLoad(string filename)
        {
            if (player_data == null)
                player_data = Load(filename);
            if (player_data == null)
                player_data = NewGame(filename);
            return player_data;
        }

        public static void SetLastSave(string filename)
        {
            if (SaveTool.IsValidFilename(filename))
            {
                PlayerPrefs.SetString(last_save_id, filename);
            }
        }

        public static string GetLastSave()
        {
            string name = PlayerPrefs.GetString(last_save_id, "");
            if (string.IsNullOrEmpty(name))
                name = "player"; //Default name
            return name; 
        }

        public static bool HasLastSave()
        {
            return HasSave(GetLastSave());
        }

        public static bool HasSave(string filename)
        {
            return SaveTool.DoesFileExist(filename + extension);
        }

        public static void Unload()
        {
            player_data = null;
            file_loaded = "";
        }

        public static void Delete(string filename)
        {
            if (file_loaded == filename)
            {
                player_data = new PlayerData(filename);
                player_data.FixData();
            }

            SaveTool.DeleteFile(filename + extension);
        }

        public static bool IsLoaded()
        {
            return player_data != null && !string.IsNullOrEmpty(file_loaded);
        }

        public static PlayerData Get()
        {
            return player_data;
        }
    }

    [System.Serializable]
    public class DroppedItemData
    {
        public string uid; // 唯一标识符
        public string item_id; // 物品ID
        public string scene; // 场景名称
        public Vector3Data pos; // 位置信息
        public int quantity; // 数量
        public float durability; // 耐久度
    }

    [System.Serializable]
    public class BuiltConstructionData
    {
        public string uid; // 唯一标识符
        public string construction_id; // 建造物ID
        public string scene; // 场景名称
        public Vector3Data pos; // 位置信息
        public QuaternionData rot; // 旋转信息
        public float durability; // 耐久度
    }

    [System.Serializable]
    public class SowedPlantData
    {
        public string uid; // 唯一标识符
        public string plant_id; // 植物ID
        public string scene; // 场景名称
        public Vector3Data pos; // 位置信息
        public QuaternionData rot; // 旋转信息
        public int growth_stage; // 生长阶段
    }

    [System.Serializable]
    public class TrainedCharacterData
    {
        public string uid; // 唯一标识符
        public string character_id; // 角色ID
        public string scene; // 场景名称
        public Vector3Data pos; // 位置信息
        public QuaternionData rot; // 旋转信息
    }

    [System.Serializable]
    public class SpawnedData
    {
        public string id; // ID
        public string uid; // 唯一标识符
        public string scene; // 场景名称
        public Vector3Data pos; // 位置信息
        public QuaternionData rot; // 旋转信息
        public float scale; // 缩放比例
    }

    [System.Serializable]
    public class SceneObjectData
    {
        public string uid; // 唯一标识符
        public string scene; // 场景名称
        public Vector3Data pos; // 位置信息
        public QuaternionData rot; // 旋转信息
    }

    [System.Serializable]
    public class RegrowthData
    {
        public string data_id; // 数据ID
        public string uid; // 原始对象的唯一标识符
        public string scene; // 场景名称
        public Vector3Data pos; // 位置信息
        public QuaternionData rot; // 旋转信息
        public int layer; // 层级
        public float scale; // 缩放比例
        public float time; // 在再生之前剩余的时间
        public float probability; // 在时间到期后生成的概率
    }

    public enum TimeType
    {
        GameHours = 0, // 游戏小时
        GameDays = 10, // 游戏天数
    }

}
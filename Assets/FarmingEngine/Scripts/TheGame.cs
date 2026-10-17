using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// 农业引擎的游戏管理器脚本(Z：游戏开始时加载玩家数据PlayerData、)
    /// Z：整个时间控制逻辑形成了一个完整的 “从流逝→触发→过渡→同步” 的闭环：
    /// </summary>
    public class TheGame : MonoBehaviour
    {
        // 非静态的 UnityAction 仅在使用 TheGame.cs 的游戏场景中有效
        public UnityAction<string> beforeSave; // 在调用 Save() 后、写入磁盘之前
        public UnityAction<bool> onPause; // 游戏暂停/恢复时
        public UnityAction onStartNewGame; // 创建新游戏后，游戏场景加载完成后的第一次调用
        public UnityAction onNewDay; // 更改天数后（如果使用睡眠功能）
        public UnityAction<float> onSkipTime; // 当跳过时间（睡眠）时，场景更改前。<float> 是跳过的游戏小时数

        // 静态 UnityAction 在任何场景中（包括不包含 TheGame.cs 的菜单场景）有效
        public static UnityAction afterLoad; // 在调用 Load() 后，加载 PlayerData 之后但在更改场景之前
        public static UnityAction afterNewGame; // 在调用 NewGame() 后，创建 PlayerData 之后但在更改场景之前
        public static UnityAction<string> beforeChangeScene; // 更改场景前（无论出于何种原因）
        
        private bool paused_by_player = false; // 是否由玩家暂停
        private bool paused_by_script = false; // 是否由脚本暂停
        private float death_timer = 0f; // 死亡计时器
        private float speed_multiplier = 1f; // 游戏速度乘数（Z：玩家可调）
        [Tooltip("标记正在切换中，在切换完成前，任何新的切换请求（比如再次点击按钮）都会因为 scene_transition 是 true 而被拒绝")]
        private bool scene_transition = false; // 场景过渡状态（Z：场景过渡锁（避免时间计算与场景切换冲突，比如新天过渡））
        private float game_speed = 1f; // 游戏速度（Z：最终游戏时间速度也就是现实和游戏时间流逝倍速差（现实时间→游戏时间的换算核心））
        private float game_speed_per_sec = 0.002f; // 游戏每秒钟的速度（Z：每秒流逝的 “游戏小时数”（时间流逝的最小单位），也就是现实过一秒游戏中过多少小时）

        private static TheGame _instance; // TheGame 单例实例

        void Awake()
        {
            _instance = this; // 初始化单例
            PlayerData.LoadLast(); // 加载上一个存档
        }

        private void Start()
        {
            PlayerData pdata = PlayerData.Get();
            GameObject spawn_parent = new GameObject("SaveFileSpawns"); // 用于存放生成对象的父物体
            string scene = SceneNav.GetCurrentScene(); // 获取当前场景

            // 生成建筑物（首先执行，因为它们可能很大，具有碰撞体，进入区域会影响玩家）
            foreach (KeyValuePair<string, BuiltConstructionData> elem in pdata.built_constructions)
            {
                Construction.Spawn(elem.Key, spawn_parent.transform); // 生成建筑物
            }

            // 设置玩家和相机位置
            if (!string.IsNullOrEmpty(pdata.current_scene) && pdata.current_scene == scene)
            {
                foreach (PlayerCharacter player in PlayerCharacter.GetAll())
                {
                    // 进入索引：-1 = 转到保存位置，0 = 不更改角色位置，1+ = 转到入口索引

                    // 保存位置
                    if (pdata.current_entry_index < 0)
                    {
                        player.transform.position = player.SaveData.position;
                        TheCamera.Get().MoveToTarget(player.SaveData.position);
                    }

                    // 入口索引
                    if (pdata.current_entry_index > 0)
                    {
                        ExitZone zone = ExitZone.GetIndex(pdata.current_entry_index);
                        if (zone != null)
                        {
                            Vector3 pos = zone.transform.position + zone.entry_offset;
                            Vector3 dir = new Vector3(zone.entry_offset.x, 0f, zone.entry_offset.z);
                            player.transform.position = pos;
                            if (dir.magnitude > 0.1f)
                            {
                                player.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                                player.FaceTorward(transform.position + dir.normalized);
                            }
                            TheCamera.Get().MoveToTarget(pos);
                        }
                    }

                    // 更新保存位置
                    player.SaveData.position = player.transform.position;
                }
            }

            // 更新宠物位置（在生成角色之前执行）
            foreach (PlayerCharacter player in PlayerCharacter.GetAll())
            {
                foreach (KeyValuePair<string, PlayerPetData> pet_pair in player.SaveData.pets)
                {
                    float radius = 1f;
                    float angle = Random.Range(0f, 360f);
                    Vector3 pos = player.transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    PlayerData.Get().SetCharacterPosition(pet_pair.Key, scene, pos, player.transform.rotation);
                }
            }

            // 生成角色
            foreach (KeyValuePair<string, TrainedCharacterData> elem in pdata.trained_characters)
            {
                Character.Spawn(elem.Key, spawn_parent.transform); // 生成角色
            }

            // 生成植物
            foreach (KeyValuePair<string, SowedPlantData> elem in pdata.sowed_plants)
            {
                Plant.Spawn(elem.Key, spawn_parent.transform); // 生成植物
            }

            // 生成其他物品
            foreach (KeyValuePair<string, SpawnedData> elem in pdata.spawned_objects)
            {
                Spawnable.Spawn(elem.Key, spawn_parent.transform); // 生成其他物品
            }

            // 生成掉落物品
            foreach (KeyValuePair<string, DroppedItemData> elem in pdata.dropped_items)
            {
                Item.Spawn(elem.Key, spawn_parent.transform); // 生成掉落物品
            }

            // 设置当前场景
            pdata.current_scene = scene;

            // 黑色面板过渡
            if (!BlackPanel.Get().IsVisible())
            {
                BlackPanel.Get().Show(true);
                BlackPanel.Get().Hide();
            }

            // 新游戏
            if (pdata.IsNewGame())
            {
                pdata.play_time = 0.01f; // 将游戏时间初始化为 0.01f 确保 onStartNewGame 不会再次被调用
                pdata.new_day = true; // 新游戏也是新的一天！
                onStartNewGame?.Invoke(); // 新游戏开始！
            }
            
            // 新的一天
            if(pdata.new_day)
            {
                pdata.new_day = false;
                pdata.day_time = GameData.Get().start_day_time; // 设置一天的开始时间
                onNewDay?.Invoke();
            }
        }
        //Z：核心逻辑 1：游戏时间的 “计算与流逝”（Update() 方法）

        void Update()
        {
            if (IsPaused())
                return; // 如果游戏暂停，直接返回

            // 检查是否死亡
            PlayerCharacter character = PlayerCharacter.GetFirst();
            if (character && character.IsDead())
            {
                death_timer += Time.deltaTime;
                if (death_timer > 2f)
                {
                    enabled = false; // 停止执行此循环
                    TheUI.Get().ShowGameOver(); // 显示游戏结束界面
                }
            }

            // 游戏速度
            game_speed = speed_multiplier * GameData.Get().game_time_mult;
            game_speed_per_sec = game_speed / 3600f;

            // 游戏时间
            PlayerData pdata = PlayerData.Get();
            pdata.day_time += game_speed_per_sec * Time.deltaTime;//Z：时间流逝，以小时论
            if (pdata.day_time >= 24f)
            {
                pdata.day_time = 0f;
                pdata.day++; // 新的一天
            }

            // 游戏时间
            pdata.play_time += Time.deltaTime;

            // 设置音乐
            AudioClip[] music_playlist = AssetData.Get().music_playlist;
            if (music_playlist != null && music_playlist.Length > 0 && !TheAudio.Get().IsMusicPlaying("music"))
            {
                AudioClip clip = music_playlist[Random.Range(0, music_playlist.Length)];
                TheAudio.Get().PlayMusic("music", clip, 0.4f, false); // 播放音乐
            }

            // 更新耐久度
            UpdateDurability(game_speed_per_sec * Time.deltaTime);

            // 新的一天(Z：从游戏配置中获取“一天开始时间”（比如6点）和“一天结束时间”（比如22点））
            GameData gdata = GameData.Get();
            //Z：两种时间区间判断（避免跨0点的逻辑bug，比如开始时间6点、结束时间2点）
            if (gdata.start_day_time + 0.5f < gdata.end_day_time)//Z：不跨0点
            {
                if (pdata.day_time > gdata.end_day_time)
                    TransitionToNextDay(); // 过渡到下一天
            }
            else if (gdata.start_day_time > gdata.end_day_time + 0.5f)//Z：跨0点
            {
                //Z：比如小于6点大于2点时
                if (pdata.day_time < gdata.start_day_time && pdata.day_time > gdata.end_day_time)
                    TransitionToNextDay(); // 过渡到下一天
            }
        }

        private void UpdateDurability(float game_hours)
        {
            PlayerData pdata = PlayerData.Get();
            List<string> remove_items_uid = new List<string>();

            // 掉落物品
            foreach (KeyValuePair<string, DroppedItemData> pair in pdata.dropped_items)
            {
                DroppedItemData ddata = pair.Value;
                ItemData idata = ItemData.Get(ddata?.item_id);

                if (idata != null && ddata != null && idata.durability_type == DurabilityType.Spoilage)
                {
                    ddata.durability -= game_hours; // 减少耐久度
                }

                if (idata != null && ddata != null && idata.HasDurability() && ddata.durability <= 0f)
                    remove_items_uid.Add(pair.Key); // 收集需要删除的物品 UID
            }

            foreach (string uid in remove_items_uid)
            {
                Item item = Item.GetByUID(uid);
                if (item != null)
                    item.SpoilItem(); // 处理掉落物品的腐坏
            }
            remove_items_uid.Clear();

            // 胶囊
            foreach (KeyValuePair<string, InventoryData> spair in pdata.inventories)
            {
                if (spair.Value != null)
                {
                    spair.Value.UpdateAllDurability(game_hours); // 更新所有耐久度
                }
            }

            // 建筑物
            foreach (KeyValuePair<string, BuiltConstructionData> pair in pdata.built_constructions)
            {
                BuiltConstructionData bdata = pair.Value;
                ConstructionData cdata = ConstructionData.Get(bdata?.construction_id);

                if (cdata != null && bdata != null && (cdata.durability_type == DurabilityType.Spoilage || cdata.durability_type == DurabilityType.UsageTime))
                {
                    bdata.durability -= game_hours; // 减少建筑物耐久度
                }

                if (cdata != null && bdata != null && cdata.HasDurability() && bdata.durability <= 0f)
                    remove_items_uid.Add(pair.Key); // 收集需要删除的建筑物 UID
            }

            foreach (string uid in remove_items_uid)
            {
                Construction item = Construction.GetByUID(uid);
                if (item != null)
                    item.Kill(); // 处理建筑物的破坏
            }
            remove_items_uid.Clear();

            // 定时奖励
            foreach (KeyValuePair <int, PlayerCharacterData> pcdata in PlayerData.Get().player_characters)
            {
                List<BonusType> remove_bonus_list = new List<BonusType>();
                foreach (KeyValuePair<BonusType, TimedBonusData> pair in pcdata.Value.timed_bonus_effects)
                {
                    TimedBonusData bdata = pair.Value;
                    bdata.time -= game_hours; // 减少奖励时间

                    if (bdata.time <= 0f)
                        remove_bonus_list.Add(pair.Key); // 收集需要移除的奖励类型
                }
                foreach (BonusType bonus in remove_bonus_list)
                    pcdata.Value.RemoveTimedBonus(bonus); // 移除奖励
                remove_bonus_list.Clear();
            }

            // 世界再生
            List<RegrowthData> spawn_growth_list = new List<RegrowthData>();
            foreach (KeyValuePair<string, RegrowthData> pair in PlayerData.Get().world_regrowth)
            {
                RegrowthData bdata = pair.Value;
                bdata.time -= game_hours; // 减少再生时间

                if (bdata.time <= 0f && bdata.scene == SceneNav.GetCurrentScene())
                    spawn_growth_list.Add(pair.Value); // 收集需要再生的物体
            }

            foreach (RegrowthData regrowth in spawn_growth_list)
            {
                Regrowth.SpawnRegrowth(regrowth); // 处理世界再生
                PlayerData.Get().RemoveWorldRegrowth(regrowth.uid); // 移除再生数据
            }
            spawn_growth_list.Clear();
        }


        //其它逻辑5，辅助功能：时间相关的 “查询接口”
        // 获取时间戳（Z：游戏中总共经过了多长时间）
        public float GetTimestamp()
        {
            PlayerData sdata = PlayerData.Get();
            return sdata.day * 24f + sdata.day_time; // 计算时间戳
        }

        // 判断是否为夜晚
        public bool IsNight()
        {
            PlayerData pdata = PlayerData.Get();
            return pdata.day_time >= 18f || pdata.day_time < 6f; // 夜晚时间段
        }

        // 判断是否有特定天气效果
        public bool IsWeather(WeatherEffect effect)
        {
            if (WeatherSystem.Get() != null)
                return WeatherSystem.Get().HasWeatherEffect(effect); // 检查是否存在特定天气效果
            return false;
        }

        // 设置游戏速度乘数，1f 为默认速度
        public void SetGameSpeedMultiplier(float mult)
        {
            speed_multiplier = mult; // 设置速度乘数
        }

        // 游戏小时数每真实时间小时（Z：游戏与现实时间倍速）
        public float GetGameTimeSpeed()
        {
            return game_speed; // 获取游戏时间速度
        }

        // 游戏小时数每真实时间秒（Z：每秒流逝的小时数）
        public float GetGameTimeSpeedPerSec()
        {
            return game_speed_per_sec; // 获取游戏时间速度（每秒）
        }

        // ---- 暂停 / 恢复 -----

        // 暂停游戏
        public void Pause()
        {
            paused_by_player = true;
            onPause?.Invoke(IsPaused()); // 调用暂停回调
        }

        // 恢复游戏
        public void Unpause()
        {
            paused_by_player = false;
            onPause?.Invoke(IsPaused()); // 调用恢复回调
        }

        // 暂停脚本
        public void PauseScripts()
        {
            paused_by_script = true;
            onPause?.Invoke(IsPaused()); // 调用暂停回调
        }

        // 恢复脚本
        public void UnpauseScripts()
        {
            paused_by_script = false;
            onPause?.Invoke(IsPaused()); // 调用恢复回调
        }

        // 判断游戏是否处于暂停状态
        public bool IsPaused()
        {
            return paused_by_player || paused_by_script; // 返回是否暂停
        }

        // 判断是否由玩家暂停
        public bool IsPausedByPlayer()
        {
            return paused_by_player;
        }

        // 判断是否由脚本暂停
        public bool IsPausedByScript()
        {
            return paused_by_script;
        }

        // -- 场景过渡 -----

        // 切换场景
        public void TransitionToScene(string scene, int entry_index)
        {
            if (!scene_transition)
            {
                if (SceneNav.DoSceneExist(scene))
                {
                    scene_transition = true;
                    StartCoroutine(GoToSceneRoutine(scene, entry_index)); // 启动场景切换协程
                }
                else
                {
                    Debug.Log("场景不存在: " + scene);
                }
            }
        }

        // 场景切换协程
        private IEnumerator GoToSceneRoutine(string scene, int entry_index)
        {
            BlackPanel.Get().Show(); // 显示黑色面板
            yield return new WaitForSeconds(1f); // 等待 1 秒
            TheGame.GoToScene(scene, entry_index); // 切换场景
        }

        // 静态方法：切换场景
        public static void GoToScene(string scene, int entry_index = 0)
        {
            if (!string.IsNullOrEmpty(scene)) {
                PlayerData pdata = PlayerData.Get();
                if (pdata != null)
                {
                    pdata.current_scene = scene;
                    pdata.current_entry_index = entry_index;
                }

                if (beforeChangeScene != null)
                    beforeChangeScene.Invoke(scene); // 调用场景更改前回调

                SceneNav.GoTo(scene); // 执行场景切换
            }
        }

        // ----- 过渡到下一天 -----
        //核心逻辑 2：“新天过渡”—— 从当天到第二天的完整流程


        /// <summary>
        /// Z：过渡到下一天
        /// </summary>
        public void TransitionToNextDay()
        {
            if (!scene_transition)//Z：确保没在切换场景（避免重复触发）
            {
                scene_transition = true;// Z：锁上过渡状态
                StartCoroutine(GoToDayRoutine()); // 启动过渡到下一天的协程
            }
        }

        /// <summary>
        /// 过渡到下一天的协程
        /// </summary>
        /// <returns></returns>
        private IEnumerator GoToDayRoutine()
        {
            BlackPanel.Get().Show(); // 显示黑色面板
            yield return new WaitForSeconds(1f); // 等待 1 秒
            GoToNextDay(); // 执行切换到下一天
        }

        /// <summary>
        /// Z:切换到下一天的具体执行方法（算清楚需要跳过多久-> 跳过时间-> 场景刷新，完成过渡）
        /// </summary>
        public void GoToNextDay()
        {
            PlayerData pdata = PlayerData.Get();
            GameData gdata = GameData.Get();

            float skipped_time;
            if (pdata.day_time > gdata.start_day_time)
            {
                //Z：情况1：当前时刻>第二天开始时间（比如当前22点，第二天6点开始）→ 跳过24-22+6=8小时
                skipped_time = 24f - pdata.day_time + gdata.start_day_time;
            }
            else
            {
                //Z： 情况2：当前时刻≤第二天开始时间（比如当前2点，第二天6点开始）→ 跳过6-2=4小时
                skipped_time = gdata.start_day_time - pdata.day_time;
            }

            SkipTime(skipped_time); // 跳过时间

            Save(); // 保存游戏
            SceneNav.RestartLevel(); // 重新加载场景
        }

        //核心逻辑 3：时间快进（SkipTime ()）—— 跳过 N 小时后的状态同步
        // 跳过 X 小时的游戏时间
        public void SkipTime(float skipped_time)
        {
            // 第一步：更新当前时刻和天数（快进时间）
            PlayerData sdata = PlayerData.Get();

            sdata.day_time += skipped_time;

            while (sdata.day_time >= 24)//Z：处理跨天
            {
                sdata.day++;
                sdata.day_time -= 24f;
                sdata.new_day = true; // 新的一天
            }

            UpdateDurability(skipped_time); // 更新耐久度

            if (onSkipTime != null)
                onSkipTime.Invoke(skipped_time); // 调用跳过时间回调
        }

        // ---- 加载 / 保存 -----

        // 保存（不是静态的，因为需要加载场景和保存文件）
        public void Save()
        {
            Save(PlayerData.Get().filename); // 保存当前文件
        }

        // 保存到指定文件
        public bool Save(string filename)
        {
            if (!SaveTool.IsValidFilename(filename))
                return false; // 失败

            foreach (PlayerCharacter player in PlayerCharacter.GetAll())
                player.SaveData.position = player.transform.position;

            PlayerData.Get().current_scene = SceneNav.GetCurrentScene();
            PlayerData.Get().current_entry_index = -1; // 根据当前位置保存数据

            if (beforeSave != null)
                beforeSave.Invoke(filename); // 调用保存前回调

            PlayerData.Save(filename, PlayerData.Get());
            return true;
        }

        // 静态方法：加载游戏
        public static void Load()
        {
            Load(PlayerData.GetLastSave()); // 从上次保存的文件加载
        }

        // 静态方法：从指定文件加载游戏
        public static bool Load(string filename)
        {
            if (!SaveTool.IsValidFilename(filename))
                return false; // 失败

            PlayerData.Unload(); // 确保先卸载
            PlayerData.AutoLoad(filename);

            if (afterLoad != null)
                afterLoad.Invoke(); // 调用加载后回调

            SceneNav.GoTo(PlayerData.Get().current_scene); // 切换到保存的场景
            return true;
        }

        // 静态方法：开始新游戏
        public static void NewGame()
        {
            NewGame(PlayerData.GetLastSave(), SceneNav.GetCurrentScene()); // 从上次保存的文件和当前场景开始新游戏
        }

        // 静态方法：从指定文件和场景开始新游戏
        public static bool NewGame(string filename, string scene)
        {
            if (!SaveTool.IsValidFilename(filename))
                return false; // 失败

            PlayerData.NewGame(filename); // 创建新游戏

            if (afterNewGame != null)
                afterNewGame.Invoke(); // 调用新游戏后回调

            SceneNav.GoTo(scene); // 切换到指定场景
            return true;
        }

        // 静态方法：删除游戏
        public static void DeleteGame(string filename)
        {
            PlayerData.Delete(filename); // 删除指定文件的游戏数据
        }

        // ---------

        // 判断是否为移动设备
        public static bool IsMobile()
        {
#if UNITY_ANDROID || UNITY_IOS || UNITY_TIZEN
            return true;
#elif UNITY_WEBGL
            return WebGLTool.isMobile();
#else
            return false;
#endif
        }

        // 在 Awake 函数中使用此方法替代 Get()
        public static TheGame Find()
        {
            if (_instance == null)
                _instance = FindObjectOfType<TheGame>();
            return _instance;
        }

        // 获取 TheGame 实例
        public static TheGame Get()
        {
            return _instance;
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if DIALOGUE_QUESTS
using DialogueQuests;
#endif

namespace FarmingEngine
{
    /// <summary>
    /// 对接 DialogueQuests 的包装类
    /// </summary>
    public class DialogueQuestsWrap : MonoBehaviour
    {
#if DIALOGUE_QUESTS

        private HashSet<Actor> inited_actors = new HashSet<Actor>(); // 已初始化的角色集合
        private float timer = 1f; // 计时器，用于慢速更新

        // 静态构造函数，注册事件处理程序
        static DialogueQuestsWrap()
        {
            TheGame.afterLoad += ReloadDQ; // 在加载后重新加载对话任务
            TheGame.afterNewGame += NewDQ; // 在新游戏开始后初始化对话任务
        }

        void Awake()
        {
            PlayerData.LoadLast(); // 确保游戏已加载

            TheGame the_game = FindObjectOfType<TheGame>();
            NarrativeManager narrative = FindObjectOfType<NarrativeManager>();

            if (narrative != null)
            {
                narrative.onPauseGameplay += OnPauseGameplay; // 游戏暂停时的事件处理
                narrative.onUnpauseGameplay += OnUnpauseGameplay; // 游戏继续时的事件处理
                narrative.onPlaySFX += OnPlaySFX; // 播放音效的事件处理
                narrative.onPlayMusic += OnPlayMusic; // 播放音乐的事件处理
                narrative.onStopMusic += OnStopMusic; // 停止音乐的事件处理
                narrative.getTimestamp += GetTimestamp; // 获取时间戳的委托
                narrative.use_custom_audio = true; // 使用自定义音频
            }
            else
            {
                Debug.LogError("Dialogue Quests: 集成失败 - 确保在场景中添加了 DQManager");
            }

            if (the_game != null)
            {
                the_game.beforeSave += SaveDQ; // 保存游戏前的事件处理
                LoadDQ(); // 加载对话任务数据
            }
        }

        private void Start()
        {
            Actor player = Actor.GetPlayerActor();
            if (player == null)
            {
                Debug.LogError("Dialogue Quests: 集成失败 - 确保在 PlayerCharacter 上添加了 Actor 脚本，并且 ActorData 的 is_player 设置为 true");
            }
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer > 1f)
            {
                timer = 0f;
                SlowUpdate(); // 慢速更新，处理角色初始化等
            }
        }

        private void SlowUpdate()
        {
            foreach (Actor actor in Actor.GetAll())
            {
                if (!inited_actors.Contains(actor))
                {
                    inited_actors.Add(actor);
                    InitActor(actor); // 初始化角色
                }
            }
        }

        private void InitActor(Actor actor)
        {
            if (actor != null)
            {
                Selectable select = actor.GetComponent<Selectable>();
                if (select != null)
                {
                    actor.auto_interact_enabled = false; // 禁用角色的自动交互
                    select.onUse += (PlayerCharacter character) =>
                    {
                        character.StopMove(); // 停止角色移动
                        character.FaceTorward(actor.transform.position); // 面向角色位置
                        actor.Interact(character.GetComponent<Actor>()); // 角色与角色交互
                    };
                }
            }
        }

        // 在 Awake 中不要调用此方法（因为在获取 NarrativeManager 之前无法工作）
        private static void ReloadDQ()
        {
            NarrativeData.Unload(); // 卸载对话数据
            LoadDQ(); // 重新加载对话任务数据
        }

        private static void NewDQ()
        {
            PlayerData pdata = PlayerData.Get();
            if (pdata != null)
            {
                NarrativeData.Unload(); // 卸载对话数据
                NarrativeData.NewGame(pdata.filename); // 新建游戏，根据指定的文件名
            }
        }

        private static void LoadDQ()
        {
            PlayerData pdata = PlayerData.Get();
            if (pdata != null)
            {
                NarrativeData.AutoLoad(pdata.filename); // 自动加载对话数据
            }
        }

        private void SaveDQ(string filename)
        {
            if (NarrativeData.Get() != null && !string.IsNullOrEmpty(filename))
            {
                NarrativeData.Save(filename, NarrativeData.Get()); // 保存对话数据
            }
        }

        private void OnPauseGameplay()
        {
            TheGame.Get().PauseScripts(); // 暂停脚本执行
        }

        private void OnUnpauseGameplay()
        {
            TheGame.Get().UnpauseScripts(); // 恢复脚本执行
        }

        private void OnPlaySFX(string channel, AudioClip clip, float vol = 0.8f)
        {
            TheAudio.Get().PlaySFX(channel, clip, vol); // 播放音效
        }

        private void OnPlayMusic(string channel, AudioClip clip, float vol = 0.4f)
        {
            TheAudio.Get().PlayMusic(channel, clip, vol); // 播放音乐
        }

        private void OnStopMusic(string channel)
        {
            TheAudio.Get().StopMusic(channel); // 停止音乐
        }

        private float GetTimestamp()
        {
            return TheGame.Get().GetTimestamp(); // 获取时间戳
        }

#endif
    }
}

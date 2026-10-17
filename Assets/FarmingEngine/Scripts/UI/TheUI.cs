using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// 顶层UI脚本，管理UI面板[Z：游戏中的 "UI 总管"，负责管理所有 UI 面板（比如商店、暂停菜单、游戏结束界面等）的显示状态，以及处理 UI 相关的通用功能]
    /// </summary>
    public class TheUI : MonoBehaviour
    {
        [Header("Panels")]
        public CanvasGroup gameplay_ui; // 游戏中UI的CanvasGroup
        public PausePanel pause_panel; // 暂停面板
        public GameOverPanel game_over_panel; // 游戏结束面板

        [Header("Material")]
        public Material ui_material; // UI材质
        public Material text_material; // 文本材质

        public AudioClip ui_sound; // UI音效

        public Color filter_red; // 红色滤镜
        public Color filter_yellow; // 黄色滤镜

        private Canvas canvas; // Canvas组件
        private RectTransform rect; // RectTransform组件

        private static TheUI _instance; // TheUI类的单例实例

        void Awake()
        {
            _instance = this; // 设置单例实例
            canvas = GetComponent<Canvas>(); // 获取Canvas组件
            rect = GetComponent<RectTransform>(); // 获取RectTransform组件

            // 设置UI和文本的材质
            if (ui_material != null)
            {
                foreach (Image image in GetComponentsInChildren<Image>())
                    image.material = ui_material;
            }
            if(text_material != null)
            {
                foreach (Text txt in GetComponentsInChildren<Text>())
                    txt.material = text_material;
            }
        }

        private void Start()
        {
            canvas.worldCamera = TheCamera.GetCamera(); // 设置Canvas的渲染相机

            // 如果未设置ItemSelectedFX或ItemDragFX，则实例化默认的特效
            if (!TheGame.IsMobile() && ItemSelectedFX.Get() == null && AssetData.Get().item_select_fx != null)
            {
                Instantiate(AssetData.Get().item_select_fx, transform.position, Quaternion.identity);
            }

            if (ItemDragFX.Get() == null && AssetData.Get().item_drag_fx != null)
            {
                Instantiate(AssetData.Get().item_drag_fx, transform.position, Quaternion.identity);
            }

            // 检查游戏中是否有PlayerUI脚本
            PlayerUI gameplay_ui = GetComponentInChildren<PlayerUI>();
            if (gameplay_ui == null)
                Debug.LogError("警告：在UI预制件的Gameplay选项卡中缺少PlayerUI脚本");
        }

        void Update()
        {
            // 根据游戏是否暂停来显示或隐藏暂停面板
            pause_panel.SetVisible(TheGame.Get().IsPausedByPlayer());

            foreach (PlayerControls controls in PlayerControls.GetAll())
            {
                if (controls.IsPressPause() && !TheGame.Get().IsPausedByPlayer())
                    TheGame.Get().Pause(); // 暂停游戏
                else if (controls.IsPressPause() && TheGame.Get().IsPausedByPlayer())
                    TheGame.Get().Unpause(); // 继续游戏
            }

            // 游戏手柄自动聚焦
            UISlotPanel focus_panel = UISlotPanel.GetFocusedPanel();
            if (focus_panel != pause_panel && TheGame.Get().IsPausedByPlayer() && PlayerControls.IsAnyGamePad())
            {
                pause_panel.Focus(); // 聚焦暂停面板
            }
            if (focus_panel == pause_panel && !TheGame.Get().IsPausedByPlayer())
            {
               UISlotPanel.UnfocusAll(); // 取消所有面板的焦点
            }
        }

        // 显示游戏结束面板
        public void ShowGameOver()
        {
            foreach(PlayerUI ui in PlayerUI.GetAll())
                ui.CancelSelection(); // 取消玩家UI的选择
            game_over_panel.Show(); // 显示游戏结束面板
        }

        // 点击暂停按钮时的处理
        public void OnClickPause()
        {
            if (TheGame.Get().IsPaused())
                TheGame.Get().Unpause(); // 如果游戏已经暂停，则继续游戏
            else
                TheGame.Get().Pause(); // 如果游戏未暂停，则暂停游戏

            TheAudio.Get().PlaySFX("UI", ui_sound); // 播放UI音效
        }

        // 检查是否有阻挡的面板被打开
        public bool IsBlockingPanelOpened()
        {
            return StoragePanel.IsAnyVisible() || ReadPanel.IsAnyVisible() || ShopPanel.Get().IsVisible() 
                || pause_panel.IsVisible() || game_over_panel.IsVisible();
        }

        /// <summary>
        /// Z：检查是否有完全阻挡的面板被打开（其实就是检测暂停面板、游戏结束面板、商店面板）
        /// </summary>
        /// <returns></returns>
        public bool IsFullPanelOpened()
        {
            return pause_panel.IsVisible() || game_over_panel.IsVisible() || ShopPanel.Get().IsVisible();
        }

        // 菜单面板会阻挡游戏手柄的控制
        public bool IsMenuOpened()
        {
            return pause_panel.IsVisible() || game_over_panel.IsVisible();
        }

        // 将屏幕坐标（如鼠标位置）转换为Canvas上的锚点坐标
        public Vector2 ScreenPointToCanvasPos(Vector2 pos)
        {
            Vector2 localpoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, pos, canvas.worldCamera, out localpoint);
            return localpoint;
        }

        public Vector2 ScreenPointToCanvasPos(Vector2 pos, RectTransform localRect)
        {
            Vector2 localpoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(localRect, pos, canvas.worldCamera, out localpoint);
            return localpoint;
        }

        /// <summary>
        /// 将世界坐标转换为屏幕坐标 [Z：坐标转换：把鼠标位置、游戏世界中的位置（比如角色脚下）转换成 UI 面板上的位置（用于显示头顶血条、弹出提示等）。]
        /// </summary>
        /// <param name="world"></param>
        /// <returns></returns>
        public Vector2 WorldToScreenPos(Vector3 world)
        {
            return TheCamera.GetCamera().WorldToScreenPoint(world);
        }

        // 将世界坐标转换为Canvas坐标
        public Vector2 WorldToCanvasPos(Vector3 world)
        {
            Vector2 screen_pos = WorldToScreenPos(world);
            return ScreenPointToCanvasPos(screen_pos);
        }

        // 获取Canvas的大小
        public Vector2 GetCanvasSize()
        {
            return rect.sizeDelta;
        }

        // 射线检测UI元素，返回最靠近的UI元素
        public static GameObject RaycastUI(Vector2 mouse_pos)
        {
            List<RaycastResult> results = RaycastAllUI(mouse_pos);
            float mdist = 999f;
            GameObject ui = null;
            foreach (RaycastResult result in results)
            {
                if (result.distance < mdist)
                {
                    ui = result.gameObject;
                    mdist = result.distance;
                }
            }
            return ui;
        }

        // 射线检测所有UI元素
        public static List<RaycastResult> RaycastAllUI(Vector2 mouse_pos)
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = mouse_pos;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            return results;
        }

        /// <summary>
        /// Z：检查鼠标是否悬停在UI元素上[Z：判断鼠标是否点击 / 悬停在 UI 元素上（比如按钮、面板）。]
        /// </summary>
        /// <param name="mouse_pos"></param>
        /// <returns></returns>
        public static bool IsMouseOverUI(Vector2 mouse_pos)
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = mouse_pos;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            return results.Count > 0;
        }


        // 获取TheUI的单例实例
        public static TheUI Get()
        {
            return _instance;
        }
    }
}

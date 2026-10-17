using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{

    /// <summary>
    /// 键盘控制管理器
    /// </summary>

    public class PlayerControls : MonoBehaviour
    {
        public int player_id = 0; // 玩家ID

        [Header("动作")]
        public KeyCode action_key = KeyCode.Space; // 动作键
        public KeyCode attack_key = KeyCode.LeftShift; // 攻击键
        public KeyCode jump_key = KeyCode.LeftControl; // 跳跃键

        [Header("摄像机")]
        public KeyCode cam_rotate_left = KeyCode.Q; // 左旋转键
        public KeyCode cam_rotate_right = KeyCode.E; // 右旋转键

        [Header("UI")]
        public KeyCode craft_key = KeyCode.C; // 制作键
        public KeyCode ui_select = KeyCode.Return; // UI选择键
        
        public KeyCode ui_use = KeyCode.RightShift; // UI使用键
        public KeyCode ui_cancel = KeyCode.Backspace; // UI取消键

        [Header("菜单")]
        public KeyCode menu_accept = KeyCode.Return; // 菜单确认键
        public KeyCode menu_cancel = KeyCode.Backspace; // 菜单取消键
        public KeyCode menu_pause = KeyCode.Escape; // 游戏暂停键

        [Header(" ---- 游戏手柄模式 ---- ")]
        public bool gamepad_controls = false; // 是否启用游戏手柄控制模式，启用后，一切通常由鼠标完成的操作将由键盘/游戏手柄控制替代，
                                              // 例如建造系统将以不同方式放置建筑物

        public delegate Vector2 MoveAction();
        public delegate bool PressAction();

        [HideInInspector]
        public bool gamepad_linked = false; // 是否连接游戏手柄
        public MoveAction gamepad_move; // 游戏手柄移动
        public MoveAction gamepad_freelook; // 游戏手柄自由看
        public MoveAction gamepad_menu; // 游戏手柄菜单
        public MoveAction gamepad_dpad; // 游戏手柄方向键
        public MoveAction gamepad_camera; // 触发摄像机
        public PressAction gamepad_pause; // 开始键
        public PressAction gamepad_action; // A键
        public PressAction gamepad_attack; // X或R1键
        public PressAction gamepad_jump; // Y键
        public PressAction gamepad_craft; // L1键
        public PressAction gamepad_use; // X键
        public PressAction gamepad_accept; // A键
        public PressAction gamepad_cancel; // B键
        public System.Action gamepad_update; // 游戏手柄更新

        private Vector2 move; // 移动向量
        private Vector2 freelook; // 自由看向量
        private Vector2 menu_move; // 菜单移动向量
        private Vector2 ui_move; // UI移动向量
        private bool menu_moved; // 菜单是否移动
        private bool ui_moved; // UI是否移动
        private float rotate_cam; // 摄像机旋转值

        private bool press_action; // 是否按下动作键
        private bool press_attack; // 是否按下攻击键
        private bool press_jump; // 是否按下跳跃键
        private bool press_craft; // 是否按下制作键

        private bool press_accept; // 是否按下确认键
        private bool press_cancel; // 是否按下取消键
        private bool press_pause; // 是否按下暂停键
        private bool press_ui_select; // 是否按下UI选择键
        private bool press_ui_use; // 是否按下UI使用键
        private bool press_ui_cancel; // 是否按下UI取消键

        private static PlayerControls control_first = null; // 第一个控制器
        private static List<PlayerControls> controls = new List<PlayerControls>(); // 所有控制器列表

        void Awake()
        {
            controls.Add(this);

            // 确定第一个控制器
            if (control_first == null || player_id < control_first.player_id)
                control_first = this;

            if (TheGame.IsMobile())
                gamepad_controls = false; // 移动设备上不使用游戏手柄


           
        }

        private void OnDestroy()
        {
            controls.Remove(this);
        }

        void Update()
        {
            // 重置所有输入状态
            move = Vector3.zero;
            freelook = Vector2.zero;
            menu_move = Vector2.zero;
            ui_move = Vector2.zero;
            rotate_cam = 0f;
            press_action = false;
            press_attack = false;
            press_jump = false;
            press_craft = false;

            press_accept = false;
            press_cancel = false;
            press_pause = false;
            press_ui_select = false;
            press_ui_use = false;
            press_ui_cancel = false;

            // 检测键盘输入
            Vector2 wasd = Vector2.zero;
            if (Input.GetKey(KeyCode.A))
                wasd += Vector2.left;
            if (Input.GetKey(KeyCode.D))
                wasd += Vector2.right;
            if (Input.GetKey(KeyCode.W))
                wasd += Vector2.up;
            if (Input.GetKey(KeyCode.S))
                wasd += Vector2.down;

            Vector2 arrows = Vector2.zero;
            if (Input.GetKey(KeyCode.LeftArrow))
                arrows += Vector2.left;
            if (Input.GetKey(KeyCode.RightArrow))
                arrows += Vector2.right;
            if (Input.GetKey(KeyCode.UpArrow))
                arrows += Vector2.up;
            if (Input.GetKey(KeyCode.DownArrow))
                arrows += Vector2.down;

            if (Input.GetKey(cam_rotate_left))
                rotate_cam += -1f;
            if (Input.GetKey(cam_rotate_right))
                rotate_cam += 1f;

            if (Input.GetKeyDown(action_key))
                press_action = true;
            if (Input.GetKeyDown(attack_key))
                press_attack = true;
            if (Input.GetKeyDown(jump_key))
                press_jump = true;
            if (Input.GetKeyDown(craft_key))
                press_craft = true;

            if (Input.GetKeyDown(menu_accept))
                press_accept = true;
            if (Input.GetKeyDown(menu_cancel))
                press_cancel = true;
            if (Input.GetKeyDown(menu_pause))
                press_pause = true;

            if (Input.GetKeyDown(ui_select))
                press_ui_select = true;
            if (Input.GetKeyDown(ui_use))
                press_ui_use = true;
            if (Input.GetKeyDown(ui_cancel))
                press_ui_cancel = true;

            Vector2 both = (arrows + wasd);
            move = wasd;
            if (gamepad_controls)
                freelook = arrows;

            // 菜单 / UI
            if (!menu_moved && both.magnitude > 0.5f)
            {
                menu_move = both;
                menu_moved = true;
            }

            if (both.magnitude < 0.5f)
                menu_moved = false;

            if (!ui_moved && arrows.magnitude > 0.5f)
            {
                ui_move = arrows;
                ui_moved = true;
            }

            if (arrows.magnitude < 0.5f)
                ui_moved = false;

            // 游戏手柄
            if (gamepad_linked && gamepad_controls)
            {
                move += gamepad_move.Invoke();
                freelook += gamepad_freelook.Invoke();
                rotate_cam += gamepad_camera.Invoke().x;
                ui_move += gamepad_dpad.Invoke();
                menu_move += gamepad_menu.Invoke();
                menu_move += gamepad_dpad.Invoke();

                press_action = press_action || gamepad_action.Invoke();
                press_attack = press_attack || gamepad_attack.Invoke();
                press_jump = press_jump || gamepad_jump.Invoke();
                press_craft = press_craft || gamepad_craft.Invoke();
                press_accept = press_accept || gamepad_accept.Invoke();
                press_cancel = press_cancel || gamepad_cancel.Invoke();
                press_pause = press_pause || gamepad_pause.Invoke();
                press_ui_select = press_ui_select || gamepad_accept.Invoke();
                press_ui_use = press_ui_use || gamepad_use.Invoke();
                press_ui_cancel = press_ui_cancel || gamepad_cancel.Invoke();

                gamepad_update?.Invoke();
            }

            move = move.normalized * Mathf.Min(move.magnitude, 1f);
            freelook = freelook.normalized * Mathf.Min(freelook.magnitude, 1f);
        }

        public Vector2 GetMove() { return move; } // 获取移动向量
        public Vector2 GetFreelook() { return freelook; } // 获取自由看向量
        public bool IsMoving() { return move.magnitude > 0.1f; } // 检测是否在移动
        public float GetRotateCam() { return rotate_cam; } // 获取摄像机旋转值

        public bool IsPressAttack() { return press_attack; } // 检测是否按下攻击键
        public bool IsPressAction() { return press_action; } // 检测是否按下动作键
        public bool IsPressJump() { return press_jump; } // 检测是否按下跳跃键
        public bool IsPressCraft() { return press_craft; } // 检测是否按下制作键

        public Vector2 GetUIMove() { return ui_move; } // 获取UI移动向量
        public Vector2 GetMenuMove() { return menu_move; } // 获取菜单移动向量

        public bool IsPressMenuAccept() { return press_accept; } // 检测是否按下菜单确认键
        public bool IsPressMenuCancel() { return press_cancel; } // 检测是否按下菜单取消键
        public bool IsPressPause() { return press_pause; } // 检测是否按下暂停键
        public bool IsPressUISelect() { return press_ui_select; } // 检测是否按下UI选择键
        public bool IsPressUIUse() { return press_ui_use; } // 检测是否按下UI使用键
        public bool IsPressUICancel() { return press_ui_cancel; } // 检测是否按下UI取消键

        public bool IsUIPressAny() { return ui_move.magnitude > 0.5f; } // 检测是否有任何UI按键按下
        public bool IsUIPressLeft() { return ui_move.x < -0.5f; } // 检测是否按下UI左键
        public bool IsUIPressRight() { return ui_move.x > 0.5f; } // 检测是否按下UI右键
        public bool IsUIPressUp() { return ui_move.y > 0.5f; } // 检测是否按下UI上键
        public bool IsUIPressDown() { return ui_move.y < -0.5f; } // 检测是否按下UI下键

        public bool IsMenuPressLeft() { return menu_move.x < -0.5f; } // 检测是否按下菜单左键
        public bool IsMenuPressRight() { return menu_move.x > 0.5f; } // 检测是否按下菜单右键
        public bool IsMenuPressUp() { return menu_move.y > 0.5f; } // 检测是否按下菜单上键
        public bool IsMenuPressDown() { return menu_move.y < -0.5f; } // 检测是否按下菜单下键

        public bool IsPressedByName(string name)
        {
            return Input.GetKeyDown(name); // 按名称检测是否按下某键
        }

        /// <summary>
        /// Z：是否处于游戏手柄模式
        /// </summary>
        /// <returns></returns>
        public bool IsGamePad()
        {
            return gamepad_controls; // 是否处于游戏手柄模式
        }

        public static bool IsAnyGamePad()
        {
            foreach (PlayerControls control in controls)
            {
                if (control.IsGamePad())
                    return true;
            }
            return false;
        }

        public static PlayerControls Get(int player_id = 0)
        {
            foreach (PlayerControls control in controls)
            {
                if (control.player_id == player_id)
                    return control;
            }
            return null;
        }

        public static PlayerControls GetFirst()
        {
            return control_first; // 获取第一个控制器
        }

        public static List<PlayerControls> GetAll()
        {
            return controls; // 获取所有控制器列表
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if IN_CONTROL
using InControl;
#endif

namespace FarmingEngine
{
    /// <summary>
    /// 对接 InControl 的包装类
    /// 若要使控制器正常工作，请确保在 PlayerControls 脚本中启用 gamepad_controls
    /// </summary>
    public class InControlWrap : MonoBehaviour
    {
#if IN_CONTROL

        public int player_id = 0; // 玩家ID

        // 输入控制类型定义
        public InputControlType action = InputControlType.Action1;
        public InputControlType attack = InputControlType.Action3;
        public InputControlType attack2 = InputControlType.RightBumper;
        public InputControlType jump = InputControlType.Action4;
        public InputControlType use = InputControlType.Action3;
        public InputControlType craft = InputControlType.LeftBumper;
        public InputControlType menu_accept = InputControlType.Action1;
        public InputControlType menu_cancel = InputControlType.Action2;
        public InputControlType menu_pause = InputControlType.Command;
        public InputControlType camera_left = InputControlType.LeftTrigger;
        public InputControlType camera_right = InputControlType.RightTrigger;

        [Header("Load InControl Manager Prefab")]
        public GameObject in_control_manager; // InControl 管理器预制体

        private InputDevice active_device; // 当前激活的输入设备

        private void Awake()
        {
            // 添加 InControl 管理器到场景中
            if (!FindObjectOfType<InControlManager>())
            {
                if (in_control_manager != null)
                {
                    Instantiate(in_control_manager);
                }
                else
                {
                    GameObject incontrol = new GameObject("InControl");
                    incontrol.AddComponent<InControlManager>();
                }
            }
        }

        void Start()
        {
            active_device = InputManager.ActiveDevice; // 获取当前激活的输入设备

            PlayerControls controls = PlayerControls.Get(player_id); // 获取玩家控制器
            controls.gamepad_linked = true; // 将游戏手柄连接状态设置为 true

            // 设置游戏手柄输入的处理方法
            controls.gamepad_action = () => { return WasPressed(active_device, action); };
            controls.gamepad_attack = () => { return WasPressed(active_device, attack) || WasPressed(active_device, attack2); };
            controls.gamepad_jump = () => { return WasPressed(active_device, jump); };
            controls.gamepad_use = () => { return WasPressed(active_device, use); };
            controls.gamepad_craft = () => { return WasPressed(active_device, craft); };
            controls.gamepad_accept = () => { return WasPressed(active_device, menu_accept); };
            controls.gamepad_cancel = () => { return WasPressed(active_device, menu_cancel); };
            controls.gamepad_pause = () => { return WasPressed(active_device, menu_pause); };

            controls.gamepad_camera = () => { return new Vector2(-GetAxis(active_device, camera_left) + GetAxis(active_device, camera_right), 0f); };

            controls.gamepad_move = () => { return GetTwoAxis(active_device, InputControlType.LeftStickX, InputControlType.LeftStickY); };
            controls.gamepad_freelook = () => { return GetTwoAxis(active_device, InputControlType.RightStickX, InputControlType.RightStickY); };
            controls.gamepad_menu = () => { return GetTwoAxisThreshold(active_device, InputControlType.LeftStickX, InputControlType.LeftStickY) + GetTwoAxisPress(active_device, InputControlType.DPadX, InputControlType.DPadY); };
            controls.gamepad_dpad = () => { return GetTwoAxisPress(active_device, InputControlType.DPadX, InputControlType.DPadY); };
        }

        void Update()
        {
            active_device = InputManager.ActiveDevice; // 更新当前激活的输入设备
        }

        private bool WasPressed(InputDevice device, InputControlType type)
        {
            if(device != null)
                return device.GetControl(type).WasPressed; // 检查输入设备上的指定控制是否被按下
            return false;
        }

        private float GetAxis(InputDevice device, InputControlType type)
        {
            if (device != null)
                return device.GetControl(type).Value; // 获取输入设备上指定控制的值
            return 0f;
        }

        private Vector2 GetTwoAxis(InputDevice device, InputControlType typeX, InputControlType typeY)
        {
            return new Vector2(GetAxis(device, typeX), GetAxis(device, typeY)); // 获取输入设备上两个轴的值
        }

        private float GetAxisPress(InputDevice device, InputControlType type)
        {
            if (device != null)
            {
                InputControl control = device.GetControl(type);
                return control.WasPressed ? control.Value : 0f; // 获取输入设备上指定控制按下时的值
            }
            return 0f;
        }

        private Vector2 GetTwoAxisPress(InputDevice device, InputControlType typeX, InputControlType typeY)
        {
            return new Vector2(GetAxisPress(device, typeX), GetAxisPress(device, typeY)); // 获取输入设备上两个轴按下时的值
        }

        private float GetAxisThreshold(InputDevice device, InputControlType type)
        {
            if (device != null)
            {
                InputControl control = device.GetControl(type);
                return Mathf.Abs(control.LastValue) < 0.5f && Mathf.Abs(control.Value) >= 0.5f ? Mathf.Sign(control.Value) : 0f; // 获取输入设备上指定控制的阈值
            }
            return 0f;
        }

        private Vector2 GetTwoAxisThreshold(InputDevice device, InputControlType typeX, InputControlType typeY)
        {
            return new Vector2(GetAxisThreshold(device, typeX), GetAxisThreshold(device, typeY)); // 获取输入设备上两个轴的阈值
        }
#endif
    }
}

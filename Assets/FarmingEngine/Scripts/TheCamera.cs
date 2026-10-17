using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    // 相机的自由视角模式枚举
    public enum FreelookMode
    {
        Hold = 0,    // 按住时启用自由视角
        Toggle = 10,  // 切换启用/禁用自由视角
        Always = 20,  // 始终启用自由视角
        Never = 30,   // 从不启用自由视角
    }

    /// <summary>
    /// 主相机脚本
    /// </summary>
    public class TheCamera : MonoBehaviour
    {
        public bool move_enabled = true; // 如果设置为 false，将禁用内置相机系统，允许使用自定义相机系统

        [Header("旋转/缩放")]
        public float rotate_speed = 120f; // 设置相机旋转速度，负值表示反向旋转
        public float zoom_speed = 0.5f;   // 缩放速度
        public float zoom_in_max = 0.5f;  // 最大缩小值
        public float zoom_out_max = 1f;   // 最大放大值

        [Header("平滑处理")]
        public bool smooth_camera = false; // 如果为 true，相机会平滑移动，但可能不够精确
        public float smooth_speed = 10f;   // 平滑移动的速度
        public float smooth_rotate_speed = 90f; // 平滑旋转的速度

        [Header("仅限移动设备")]
        public float rotate_speed_touch = 10f; // 移动设备上的旋转速度
        public float zoom_speed_touch = 1f;    // 移动设备上的缩放速度

        [Header("仅限第三人称视角")]
        public FreelookMode freelook_mode; // 自由视角模式
        public float freelook_speed_x = 150f; // 自由视角下的 X 轴旋转速度
        public float freelook_speed_y = 120f; // 自由视角下的 Y 轴旋转速度
        public float freelook_max_up = 0.4f; // 自由视角下的最大向上角度
        public float freelook_max_down = 0.8f; // 自由视角下的最大向下角度

        [Header("目标")]
        public GameObject follow_target;  // 跟随的目标
        public Vector3 follow_offset;     // 跟随目标的偏移量

        protected Vector3 custom_offset;    // 自定义偏移量
        protected Vector3 current_vel;      // 当前速度
        protected float current_zoom = 0f;  // 当前缩放值
        protected float add_rotate = 0f;    // 额外的旋转角度
        protected bool is_locked = false;   // 相机是否锁定

        protected Transform target_transform;     // 相机目标变换
        protected Transform cam_target_transform; // 相机目标的变换

        protected Camera cam; // 相机组件

        protected Vector3 shake_vector = Vector3.zero; // 摇动效果的向量
        protected float shake_timer = 0f;              // 摇动效果的计时器
        protected float shake_intensity = 1f;          // 摇动效果的强度

        protected static TheCamera _instance; // 单例实例

        protected virtual void Awake()
        {
            _instance = this;
            cam = GetComponent<Camera>();

            // 创建相机目标游戏对象
            GameObject cam_target = new GameObject("CameraTarget");
            target_transform = cam_target.transform;
            target_transform.position = transform.position - follow_offset;

            // 创建相机目标相机游戏对象
            GameObject cam_target_cam = new GameObject("CameraTargetCam");
            cam_target_transform = cam_target_cam.transform;
            cam_target_transform.SetParent(target_transform);
            cam_target_transform.localPosition = follow_offset;
            cam_target_transform.localRotation = transform.localRotation;
        }

        protected virtual void Start()
        {
            PlayerControlsMouse mouse = PlayerControlsMouse.Get();
            mouse.onRightClick += (Vector3 vect) => { ToggleLock(); }; // 右键点击时切换锁定状态
        }

        protected virtual void LateUpdate()
        {
            if (follow_target == null)
            {
                // 自动分配跟随目标
                PlayerCharacter first = PlayerCharacter.GetFirst();
                if (first != null)
                    follow_target = first.gameObject;
                return;
            }

            if (!move_enabled)
                return;

            UpdateControls();

            bool free_rotation = IsFreelook();
            if (free_rotation)
                UpdateFreeCamera();
            else
                UpdateCamera();

            // 如果锁定状态且 UI 面板阻挡了视角，则取消锁定
            if (is_locked && TheUI.Get() && TheUI.Get().IsBlockingPanelOpened())
                ToggleLock();

            // 摇动效果
            if (shake_timer > 0f)
            {
                shake_timer -= Time.deltaTime;
                shake_vector = new Vector3(Mathf.Cos(shake_timer * Mathf.PI * 8f) * 0.02f, Mathf.Sin(shake_timer * Mathf.PI * 7f) * 0.02f, 0f);
                transform.position += shake_vector * shake_intensity;
            }
        }

        protected virtual void UpdateControls()
        {
            if (TheUI.Get().IsBlockingPanelOpened())
                return;

            PlayerControls controls = PlayerControls.Get();
            PlayerControlsMouse mouse = PlayerControlsMouse.Get();

            // 旋转
            add_rotate = 0f;
            add_rotate += controls.GetRotateCam() * rotate_speed;
            add_rotate += mouse.GetTouchRotate() * rotate_speed_touch;

            // 缩放
            current_zoom += mouse.GetTouchZoom() * zoom_speed_touch; // 移动设备双指缩放
            current_zoom += mouse.GetMouseScroll() * zoom_speed; // 鼠标滚轮缩放
            current_zoom = Mathf.Clamp(current_zoom, -zoom_out_max, zoom_in_max);

            // 设置自由视角模式
            if (freelook_mode == FreelookMode.Hold)
                SetLockMode(mouse.IsMouseHoldRight());
            if (freelook_mode == FreelookMode.Always)
                SetLockMode(true);
            if (freelook_mode == FreelookMode.Never)
                SetLockMode(false);
            if (controls.IsGamePad())
                Cursor.visible = !is_locked && mouse.IsUsingMouse();
        }

        protected virtual void UpdateCamera()
        {
            // 旋转和移动
            float rot = target_transform.rotation.eulerAngles.y + add_rotate * Time.deltaTime;
            Quaternion targ_rot = Quaternion.Euler(target_transform.rotation.eulerAngles.x, rot, 0f);

            if (smooth_camera)
            {
                target_transform.position = Vector3.SmoothDamp(target_transform.position, follow_target.transform.position, ref current_vel, 1f / smooth_speed);
                target_transform.rotation = Quaternion.Slerp(target_transform.rotation, targ_rot, smooth_rotate_speed * Time.deltaTime);
            }
            else
            {
                target_transform.position = follow_target.transform.position;
                target_transform.rotation = targ_rot;
            }

            // 缩放
            Vector3 targ_zoom = (follow_offset + custom_offset) * (1f - current_zoom);
            cam_target_transform.localPosition = Vector3.Lerp(cam_target_transform.localPosition, targ_zoom, 10f * Time.deltaTime);

            // 移动到目标位置
            transform.rotation = cam_target_transform.rotation;
            transform.position = cam_target_transform.position;
        }

        protected virtual void UpdateFreeCamera()
        {
            // 控制
            PlayerControls controls = PlayerControls.Get();
            PlayerControlsMouse mouse = PlayerControlsMouse.Get();
            Vector2 mouse_delta = Vector2.zero;
            if (is_locked)
                mouse_delta += mouse.GetMouseDelta();
            if (controls.IsGamePad())
                mouse_delta += controls.GetFreelook();

            // 旋转和移动
            Quaternion rot_backup = target_transform.rotation;
            Quaternion targ_rot = target_transform.rotation;
            targ_rot = Quaternion.AngleAxis(freelook_speed_y * -mouse_delta.y * 0.5f * Time.deltaTime, target_transform.right) * targ_rot;
            targ_rot = Quaternion.Euler(0f, freelook_speed_x * mouse_delta.x * Time.deltaTime, 0) * targ_rot;
            targ_rot.eulerAngles = new Vector3(targ_rot.eulerAngles.x, targ_rot.eulerAngles.y, 0f);

            if (smooth_camera)
            {
                target_transform.position = Vector3.SmoothDamp(target_transform.position, follow_target.transform.position, ref current_vel, 1f / smooth_speed);
                target_transform.rotation = Quaternion.Slerp(target_transform.rotation, targ_rot, smooth_rotate_speed * Time.deltaTime);
            }
            else
            {
                target_transform.position = follow_target.transform.position;
                target_transform.rotation = targ_rot;
            }

            // 缩放
            Vector3 targ_zoom = (follow_offset + custom_offset) * (1f - current_zoom);
            cam_target_transform.localPosition = Vector3.Lerp(cam_target_transform.localPosition, targ_zoom, 10f * Time.deltaTime);

            // 锁定，避免过度旋转
            if (cam_target_transform.forward.y > freelook_max_up || cam_target_transform.forward.y < -freelook_max_down)
            {
                target_transform.rotation = rot_backup;
            }

            // 移动到目标位置
            transform.rotation = cam_target_transform.rotation;
            transform.position = cam_target_transform.position;
        }

        public virtual void SetLockMode(bool locked)
        {
            if (is_locked != locked)
            {
                is_locked = locked;
                Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !locked;
            }
        }

        public virtual void ToggleLock()
        {
            if (freelook_mode == FreelookMode.Toggle)
            {
                SetLockMode(!is_locked); // 切换锁定状态
            }
        }

        public virtual void MoveToTarget(Vector3 target)
        {
            Vector3 diff = target - target_transform.position;
            target_transform.position = target;
            transform.position += diff;
        }

        public virtual void Shake(float intensity = 2f, float duration = 0.5f)
        {
            shake_intensity = intensity;
            shake_timer = duration; // 设置摇动效果的强度和持续时间
        }

        public virtual void SetOffset(Vector3 offset)
        {
            custom_offset = offset; // 设置自定义偏移量
        }

        public virtual bool IsFreelook()
        {
            PlayerControls controls = PlayerControls.Get();
            return freelook_mode != FreelookMode.Never && (is_locked || controls.IsGamePad());
        }

        public bool IsFreeRotation() => IsFreelook(); // 旧版本函数名

        public virtual bool IsInside(Vector2 screen_pos)
        {
            return cam.pixelRect.Contains(screen_pos); // 检查屏幕位置是否在相机视野范围内
        }

        public virtual Quaternion GetFacingRotation()
        {
            Vector3 facing = GetFacingFront();
            return Quaternion.LookRotation(facing.normalized, Vector3.up); // 获取相机面朝的旋转角度
        }

        public Quaternion GetRotation() => GetFacingRotation(); // 旧版本函数名

        public Vector3 GetTargetPos()
        {
            return target_transform.position; // 获取目标位置
        }

        public Quaternion GetTargetRotation()
        {
            return target_transform.rotation; // 获取目标旋转
        }

        public Vector3 GetTargetPosOffsetFace(float dist)
        {
            return target_transform.position + GetFacingFront() * dist; // 获取相机面朝方向上的目标位置
        }

        public Vector3 GetFacingFront()
        {
            Vector3 dir = transform.forward;
            dir.y = 0f;
            return dir.normalized; // 获取相机面朝的前方向
        }

        public Vector3 GetFacingRight()
        {
            Vector3 dir = transform.right;
            dir.y = 0f;
            return dir.normalized; // 获取相机的右方向
        }

        public Vector3 GetFacingDir()
        {
            return transform.forward; // 获取相机的朝向方向
        }

        public bool IsLocked()
        {
            return is_locked; // 返回相机是否锁定
        }

        public Camera GetCam()
        {
            return cam; // 获取相机组件
        }

        public static Camera GetCamera()
        {
            Camera camera = _instance != null ? _instance.GetCam() : Camera.main;
            return camera; // 获取相机组件，如果实例不存在，则返回主相机
        }

        public static TheCamera Get()
        {
            return _instance; // 获取单例实例
        }
    }
}

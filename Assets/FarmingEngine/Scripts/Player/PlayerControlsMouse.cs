using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace FarmingEngine
{

    /// <summary>
    /// 鼠标/触摸控制管理器
    /// </summary>

    public class PlayerControlsMouse : MonoBehaviour
    {
        public LayerMask selectable_layer = ~0; // 可选择层
        public LayerMask floor_layer = (1 << 9); // 地板层，设为None以始终返回0作为地板高度

        public UnityAction<Vector3> onClick; // 左键单击时触发(Z不在UI上)
        public UnityAction<Vector3> onRightClick; // 右键单击时触发(Z不在UI上)
        public UnityAction<Vector3> onLongClick; // 按住左键1秒以上时触发
        public UnityAction<Vector3> onHold; // 按住左键时持续触发
        public UnityAction<Vector3> onRelease; // 释放鼠标按钮时触发
        public UnityAction<Vector3> onClickFloor; // 点击地板时触发
        public UnityAction<Selectable, Vector3> onClickObject; // 点击物体时触发

        private float mouse_scroll = 0f; // 鼠标滚轮滚动值
        private Vector2 mouse_delta = Vector2.zero; // 鼠标移动增量
        private bool mouse_hold_left = false; // 是否按住鼠标左键
        private bool mouse_hold_right = false; // 是否按住鼠标右键

        private float using_timer = 1f; // 使用计时器
        private float hold_timer = 0f; // 按住计时器
        private float hold_total_timer = 0f; // 总按住计时器
        private bool is_holding = false; // 是否正在按住鼠标左键
        private bool has_mouse = false; // 是否检测到鼠标
        private bool can_long_click = false; // 是否可以进行长按操作
        private Vector3 hold_start; // 按住开始位置
        private Vector3 last_pos; // 上一次鼠标位置
        private Vector3 floor_pos; // 鼠标指向的世界位置的地板位置

        private float zoom_value = 0f; // 缩放值
        private float rotate_value = 0f; // 旋转值
        private bool is_zoom_mode = false; // 是否处于缩放模式
        private Vector3 prev_touch1 = Vector3.zero; // 上一次触摸1位置
        private Vector3 prev_touch2 = Vector3.zero; // 上一次触摸2位置

        private HashSet<Selectable> raycast_list = new HashSet<Selectable>(); // 射线检测列表（Z：存储射线检测到的所有可交互对象（就是一条射线上的所有Selectable））

        private static PlayerControlsMouse _instance; // 单例实例

        void Awake()
        {
            _instance = this;
            last_pos = Input.mousePosition;
        }

        void Update()
        {
            // 如果不是移动设备，在 Update 中始终检测射线（移动设备只在点击后检测）
            if (!TheGame.IsMobile())
            {
                RaycastSelectables();
                RaycastFloorPos();
            }

            // 鼠标点击
            if (Input.GetMouseButtonDown(0) && IsInGameplay())
            {
                hold_start = Input.mousePosition;
                is_holding = true;
                can_long_click = true;
                hold_timer = 0f;
                hold_total_timer = 0f;
                OnMouseClick();
            }

            if (Input.GetMouseButtonUp(0))
            {
                OnMouseRelease();
            }

            if (Input.GetMouseButtonDown(1) && IsInGameplay())
            {
                OnRightMouseClick();
            }

            // 鼠标滚轮
            mouse_scroll = Input.mouseScrollDelta.y;

            // 鼠标增量
            mouse_delta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

            // 检测鼠标使用
            Vector3 diff = (Input.mousePosition - last_pos);
            float dist = diff.magnitude;
            if (dist > 0.01f)
            {
                using_timer = 0f;
                last_pos = Input.mousePosition;
            }

            mouse_hold_left = Input.GetMouseButton(0) && IsInGameplay();
            mouse_hold_right = Input.GetMouseButton(1) && IsInGameplay();
            if (mouse_hold_left || mouse_hold_right)
                using_timer = 0f;

            // 是否使用鼠标（相对于键盘）
            using_timer += Time.deltaTime;
            has_mouse = has_mouse || IsUsingMouse();

            // 长按鼠标
            float dist_hold = (Input.mousePosition - hold_start).magnitude;
            is_holding = is_holding && mouse_hold_left;
            can_long_click = can_long_click && mouse_hold_left && dist_hold < 5f;

            if (is_holding)
            {
                hold_timer += Time.deltaTime;
                hold_total_timer += Time.deltaTime;
                if (can_long_click && hold_timer > 0.5f)
                {
                    can_long_click = false;
                    hold_timer = 0f;
                    OnLongMouseClick();
                }
                else if (!can_long_click && hold_timer > 0.2f)//Z：在 “无法触发长按事件” 的前提下，按住鼠标左键持续 0.2 秒以上。（比如拖动/已经触发过长按了，但是还在按住）
                {
                    OnMouseHold();
                }
            }

            // 移动设备的摇杆和缩放
            if (TheGame.IsMobile())
            {
                zoom_value = 0f;
                rotate_value = 0f;
                if (Input.touchCount == 2)
                {
                    Vector2 pos1 = Input.GetTouch(0).position;
                    Vector2 pos2 = Input.GetTouch(1).position;
                    if (is_zoom_mode)
                    {
                        float distance = Vector2.Distance(pos1, pos2);
                        float prev_distance = Vector2.Distance(prev_touch1, prev_touch2);
                        zoom_value = (distance - prev_distance) / (float)Screen.height;

                        var pDir = prev_touch2 - prev_touch1;
                        var cDir = pos2 - pos1;
                        rotate_value = Vector2.SignedAngle(pDir, cDir);
                        rotate_value = Mathf.Clamp(rotate_value, -45f, 45f);
                    }
                    prev_touch1 = pos1;
                    prev_touch2 = pos2;
                    is_zoom_mode = true; // 等待一帧确保已计算出距离
                }
                else
                {
                    is_zoom_mode = false;
                }
            }
        }

        //Z：这段代码，更新鼠标射线下的所有Selectable物体，且将其设置为悬停状态
        public void RaycastSelectables()
        {
            foreach (Selectable select in raycast_list)
                select.SetHover(false);

            raycast_list.Clear();

            if (TheUI.Get().IsBlockingPanelOpened())
                return;

            PlayerUI ui = PlayerUI.GetFirst();
            if (ui != null && ui.IsBuildMode())
                return; // 建造模式下不要悬停/选择物体

            RaycastHit[] hits = Physics.RaycastAll(GetMouseCameraRay(), 99f, selectable_layer.value);
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider != null)
                {
                    Selectable select = hit.collider.GetComponentInParent<Selectable>();
                    if (select != null)
                    {
                        raycast_list.Add(select);
                        select.SetHover(true);
                    }
                }
            }
        }

        //Z：这里是为了得到鼠标指向的地板的位置
        public void RaycastFloorPos()
        {
            Ray ray = GetMouseCameraRay();
            RaycastHit hit;
            bool success = Physics.Raycast(ray, out hit, 100f, floor_layer.value, QueryTriggerInteraction.Ignore);
            if (success)//优先检测地面位置
            {
                floor_pos = ray.GetPoint(hit.distance);
            }
            else //后备方案：如果未击中地面，则使用数学平面计算交点
            {
                Plane plane = new Plane(Vector3.up, 0f);
                float dist;
                bool phit = plane.Raycast(ray, out dist);
                if (phit)
                {
                    floor_pos = ray.GetPoint(dist);//Z：等效于ray.origin + ray.direction * dist
                }
            }

            // Debug.DrawLine(TheCamera.GetCamera().transform.position, floor_pos);
        }

        private void MobileCheckRaycast()
        {
            // 如果是移动设备，只在点击后检测射线（桌面设备在 Update 中每帧检测）
            if (TheGame.IsMobile())
            {
                RaycastSelectables();
                RaycastFloorPos();
            }
        }

        //TODO:2025.07.20研究锄头锄地的功能

        //Z：这一段作用是，鼠标点击时，（）根据屏幕射线获取到点击到的selectable物品触发委托，没有就触发点击到地板委托
        private void OnMouseClick()
        {
            if (IsMouseOverUI())
                return;

            MobileCheckRaycast();

            Selectable hovered = GetNearestRaycastList(floor_pos);
            if (hovered != null)
            {
                //Debug.Log("PlayerControlsMouse.OnMouseClick()测试点击到了可交互的物体");
                onClick?.Invoke(hovered.transform.position);
                onClickObject?.Invoke(hovered, floor_pos);
            }
            else
            {
                //Debug.Log("PlayerControlsMouse.OnMouseClick()测试点击到了地板");
                onClick?.Invoke(floor_pos);
                onClickFloor?.Invoke(floor_pos);
            }
        }

        private void OnMouseRelease()
        {
            if (IsMouseOverUI())
                return;

            MobileCheckRaycast();

            onRelease?.Invoke(floor_pos);
        }

        private void OnRightMouseClick()
        {
            if (IsMouseOverUI())
                return;

            MobileCheckRaycast();

            onRightClick?.Invoke(floor_pos);
        }

        // 长按鼠标
        private void OnLongMouseClick()
        {
            if (IsMouseOverUI())
                return;

            MobileCheckRaycast();

            onLongClick?.Invoke(floor_pos);
        }

        private void OnMouseHold()
        {
            if (IsMouseOverUI())
                return;

            MobileCheckRaycast();

            onHold?.Invoke(floor_pos);
        }

        // 获取最近的射线检测物体
        public Selectable GetNearestRaycastList(Vector3 pos)
        {
            Selectable nearest = null;
            float min_dist = 99f;
            foreach (Selectable select in raycast_list)//Z：注意到，这里遍历的是鼠标射线中检测到的可交互物品，而且看样子是找其中距离点击位置最近的
            {
                if (select != null && select.CanBeClicked())
                {
                    float dist = (select.transform.position - pos).magnitude;
                    if (dist < min_dist)
                    {
                        min_dist = dist;
                        nearest = select;
                    }
                }
            }
            return nearest;
        }

        public Vector2 GetScreenPos()
        {
            // 以百分比返回鼠标位置
            Vector3 mpos = Input.mousePosition;
            return new Vector2(mpos.x / (float)Screen.width, mpos.y / (float)Screen.height);
        }

        public Vector3 GetPointingPos()
        {
            return floor_pos;
        }

        public bool IsInRaycast(Selectable select)
        {
            return raycast_list.Contains(select);
        }

        public HashSet<Selectable> GetRaycastList()
        {
            return raycast_list;
        }

        // 设备是否有鼠标（例如在游戏机上为 false）
        public bool HasMouse()
        {
            return has_mouse;
        }

        // 用户是否正在使用鼠标？
        public bool IsUsingMouse()
        {
            return IsMovingMouse(1f); // 如果在过去一秒内移动过鼠标则认为正在使用鼠标
        }

        public bool IsMovingMouse(float offset = 0.1f)
        {
            return GetTimeSinceLastMouseMove() <= offset;
        }

        public float GetTimeSinceLastMouseMove()
        {
            return using_timer;
        }

        public bool IsMouseHold()
        {
            return mouse_hold_left;
        }

        public bool IsMouseHoldRight()
        {
            return mouse_hold_right;
        }

        public bool IsMouseHoldUI()
        {
            return Input.GetMouseButton(0);
        }

        public float GetMouseHoldDuration()
        {
            return hold_total_timer;
        }

        public float GetMouseScroll()
        {
            return mouse_scroll;
        }

        public Vector2 GetMouseDelta()
        {
            return mouse_delta;
        }

        public float GetTouchZoom()
        {
            return zoom_value;
        }

        public float GetTouchRotate()
        {
            return rotate_value;
        }

        public bool IsDoubleTouch()
        {
            return Input.touchCount >= 2;
        }

        // 限制鼠标位置在屏幕内
        private Vector3 GetClampMousePos()
        {
            Vector3 mousePos = GetMousePosition();
            mousePos.x = Mathf.Clamp(mousePos.x, 0f, Screen.width);
            mousePos.y = Mathf.Clamp(mousePos.y, 0f, Screen.height);
            return mousePos;
        }

        // 获取从摄像机到鼠标的射线
        public Ray GetMouseCameraRay()
        {
            return TheCamera.GetCamera().ScreenPointToRay(GetCursorPosition());
        }

        // 返回鼠标位置，如果不可见则返回屏幕中心位置（通常是自由查看模式）
        public Vector2 GetCursorPosition()
        {
            Vector2 pos = GetClampMousePos();
            if (!Cursor.visible)
                pos = new Vector2(Screen.width / 2f, Screen.height / 2f);
            return pos;
        }

        // 获取鼠标的世界位置
        public Vector3 GetMouseWorldPosition()
        {
            Vector3 mousePos = GetMousePosition();
            Vector3 mouse = new Vector3(mousePos.x, mousePos.y, 10f);
            return TheCamera.GetCamera().ScreenToWorldPoint(mouse);
        }

        public Vector3 GetMousePosition()
        {
            return Input.mousePosition;
        }

        // 检查鼠标是否位于任何 UI 元素之上
        public bool IsMouseOverUI()
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            return results.Count > 0;
        }

        // 鼠标是否在摄像机范围内
        public bool IsMouseInsideCamera()
        {
            return TheCamera.Get().IsInside(GetMousePosition());
        }

        // 鼠标是否在摄像机范围内且不在 UI 上
        public bool IsInGameplay()
        {
            return !IsMouseOverUI() && IsMouseInsideCamera();
        }

        public static PlayerControlsMouse Get()
        {
            return _instance;
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    // 可选类型枚举
    public enum SelectableType
    {
        Interact = 0,       // 与物体的中心交互（物体的 transform.position）
        InteractBound = 5,  // 设置为 Bound 时，与碰撞体包围盒内最近的位置交互
        InteractSurface = 10,  // 表面交互，可以在表面的不同位置进行交互，而不仅仅是中心位置
        CantInteract = 20,  // 可以点击或悬停，但无法交互
        CantSelect = 30,    // 无法点击或悬停
    }

    /// <summary>
    /// 玩家可以与之交互的任何物体都是可选对象
    /// 大多数对象都是可选的（玩家可以点击的任何东西）。
    /// 可选对象可以包含动作。
    /// 当距离摄像机太远时，可选对象将被停用，以提升游戏性能。
    /// </summary>
    public class Selectable : MonoBehaviour
    {
        public SelectableType type;  // 可选对象的类型
        public float use_range = 2f; // 使用范围

        [Header("动作")]
        public SAction[] actions;   // 动作数组

        [Header("群组")]
        public GroupData[] groups;  // 群组数据数组

        [Header("优化")]
        public float active_range = 40f;  // 如果超出此范围，则为了优化而禁用
        public bool always_run_scripts = false;  // 设置为 true 时，即使不活动，也会运行其他脚本

        [Header("轮廓")]
        public GameObject outline;  // 轮廓的子对象
        public bool generate_outline = false;  // 自动生成轮廓（使用找到的第一个网格）
        public Material outline_material;  // 生成轮廓时使用的材质

        [HideInInspector]
        public bool dont_optimize = false;  // 如果为 true，将不会被优化器关闭

        [HideInInspector]
        public bool dont_destroy = false;   // 如果为 true，将不会自动销毁

        public UnityAction onSelect;  // 当用鼠标点击时，到达目标前触发
        public UnityAction<PlayerCharacter> onUse;  // 在点击后，角色到达使用距离时触发，或者在附近使用动作按钮时触发
        public UnityAction onDestroy;  // 销毁时触发

        private Collider[] colliders;   // 碰撞体数组
        private Destructible destruct;  // 可能为 null，不是所有的可选对象都有，使用前先检查是否为 null（用于优化的快速访问）
        private Character character;    // 可能为 null，不是所有的可选对象都有，使用前先检查是否为 null（用于优化的快速访问）
        private UniqueID unique_id;     // 可能为 null，不是所有的可选对象都有，使用前先检查是否为 null
        private Transform transf;       // 上次位置的快速访问
        private bool is_hovered = false;    // 是否悬停在上面
        private bool is_active = true;      // 是否激活

        private List<MonoBehaviour> scripts = new List<MonoBehaviour>();    // 脚本列表
        private List<Animator> animators = new List<Animator>();            // 动画器列表

        private List<GroupData> active_groups = new List<GroupData>();  // 激活的群组列表

        private static HashSet<Selectable> active_list = new HashSet<Selectable>();  // 激活的可选对象集合
        private static List<Selectable> selectable_list = new List<Selectable>();   // 可选对象列表
        private static GameObject fx_parent;

        void Awake()
        {
            destruct = GetComponent<Destructible>();
            character = GetComponent<Character>();
            unique_id = GetComponent<UniqueID>();
            colliders = GetComponentsInChildren<Collider>();
            selectable_list.Add(this);
            active_list.Add(this);
            transf = transform;
            is_active = true;
            scripts.AddRange(GetComponents<MonoBehaviour>());
            animators.AddRange(GetComponentsInChildren<Animator>());
            if (groups != null)
                active_groups.AddRange(groups);
        }

        void OnDestroy()
        {
            selectable_list.Remove(this);
            active_list.Remove(this);
        }

        void Start()
        {
            GenerateAutomaticOutline();

            if ((TheGame.IsMobile() || PlayerControls.IsAnyGamePad()) && groups.Length > 0 && AssetData.Get().item_merge_fx != null)
            {
                if (fx_parent == null)
                    fx_parent = new GameObject("FX");

                GameObject fx = Instantiate(AssetData.Get().item_merge_fx, transform.position, AssetData.Get().item_merge_fx.transform.rotation);
                fx.GetComponent<ItemMergeFX>().target = this;
                fx.transform.SetParent(fx_parent.transform);
            }
        }

        private void GenerateAutomaticOutline()
        {
            // 生成自动轮廓对象
            if (generate_outline && outline_material != null)
            {
                MeshRenderer[] renders = GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer render in renders)
                {
                    GameObject new_outline = Instantiate(render.gameObject, render.transform.position, render.transform.rotation);
                    new_outline.name = "OutlineMesh";
                    new_outline.transform.localScale = render.transform.lossyScale; // 保留父对象的比例
                    new_outline.SetActive(false);

                    foreach (MonoBehaviour script in new_outline.GetComponents<MonoBehaviour>())
                        script.enabled = false; // 禁用脚本

                    MeshRenderer out_render = new_outline.GetComponent<MeshRenderer>();
                    Material[] mats = new Material[out_render.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++)
                        mats[i] = outline_material;
                    out_render.sharedMaterials = mats;
                    out_render.allowOcclusionWhenDynamic = false;
                    out_render.receiveShadows = false;
                    out_render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    if (outline != null)
                    {
                        new_outline.transform.SetParent(outline.transform);
                    }
                    else
                    {
                        new_outline.transform.SetParent(transform);
                        outline = new_outline;
                    }
                }
            }
        }

        public void Select()
        {
            if (onSelect != null)
                onSelect.Invoke();
        }

        /// <summary>
        /// 当角色与此可选对象交互时，检查所有动作，看看是否有任何应该触发的动作。
        /// </summary>
        /// <param name="character"></param>
        /// <param name="pos"></param>
        public void Use(PlayerCharacter character, Vector3 pos)
        {
            if (enabled)
            {
                PlayerUI ui = PlayerUI.Get(character.player_id);
                ItemSlot slot = ui?.GetSelectedSlot();

                MAction maction = slot?.GetItem()?.FindMergeAction(this);
                AAction aaction = FindAutoAction(character);

                if (maction != null && maction.CanDoAction(character, slot, this))
                {
                    maction.DoAction(character, slot, this);
                    PlayerUI.Get(character.player_id)?.CancelSelection();
                }
                else if (aaction != null && aaction.CanDoAction(character, this))
                {
                    aaction.DoAction(character, this);
                }
                else if (actions.Length > 0)
                {
                    ActionSelector.Get(character.player_id)?.Show(character, this, pos);
                }

                if (onUse != null)
                    onUse.Invoke(character);
            }
        }

        // 这由 TheRender 使用，隐藏远处的可选对象
        public void SetActive(bool visible, bool turn_off_gameobject = false)
        {
            if (is_active != visible)
            {
                if (!dont_optimize || visible)
                {
                    if (turn_off_gameobject && !always_run_scripts)
                        gameObject.SetActive(visible);

                    this.enabled = visible;
                    is_active = visible;
                    is_hovered = false;

                    if (!turn_off_gameobject && !always_run_scripts)
                    {
                        foreach (MonoBehaviour script in scripts)
                        {
                            if (script != null)
                                script.enabled = visible;
                        }

                        foreach (Animator anim in animators)
                        {
                            if (anim != null)
                                anim.enabled = visible;
                        }
                    }

                    if (visible)
                        active_list.Add(this);
                    else
                        active_list.Remove(this);
                }
            }
        }

        public void SetHover(bool value)
        {
            is_hovered = value;

            if (outline != null && is_hovered != outline.activeSelf)
                outline.SetActive(is_hovered);
        }

        public AAction FindAutoAction(PlayerCharacter character)
        {
            foreach (SAction action in actions)
            {
                if (action != null && action is AAction)
                {
                    AAction aaction = (AAction)action;
                    if (aaction.CanDoAction(character, this))
                        return aaction;
                }
            }
            return null;
        }

        public virtual void Destroy(float delay = 0f)
        {
            if (!dont_destroy)
                Destroy(gameObject, delay);

            if (onDestroy != null)
                onDestroy.Invoke();
        }

        public Transform GetTransform()
        {
            return transf;
        }

        public Vector3 GetPosition()
        {
            return transf.position;
        }

        public bool IsHovered()
        {
            return is_hovered;
        }

        public bool IsActive()
        {
            return is_active && enabled;
        }

        public bool AreScriptsActive()
        {
            return is_active || always_run_scripts;
        }

        // 可以通过点击与之交互，如使用动作
        public bool CanBeClicked()
        {
            return is_active && enabled && type != SelectableType.CantSelect;
        }

        // 可以通过点击与之交互，如使用动作
        public bool CanBeInteracted()
        {
            return is_active && enabled && type != SelectableType.CantInteract && type != SelectableType.CantSelect;
        }

        // 可以自动与之交互
        public bool CanAutoInteract()
        {
            return CanBeInteracted() && (onUse != null || actions.Length > 0);
        }

        public void AddGroup(GroupData group)
        {
            if (!active_groups.Contains(group))
                active_groups.Add(group);
        }

        public void RemoveGroup(GroupData group)
        {
            if (active_groups.Contains(group))
                active_groups.Remove(group);
        }

        public bool HasGroup(GroupData group)
        {
            foreach (GroupData agroup in active_groups)
            {
                if (agroup == group)
                    return true;
            }
            return false;
        }

        public bool HasGroup(GroupData[] mgroups)
        {
            foreach (GroupData mgroup in mgroups)
            {
                foreach (GroupData agroup in active_groups)
                {
                    if (agroup == mgroup)
                        return true;
                }
            }
            return false;
        }

        public bool IsInUseRange(PlayerCharacter character)
        {
            Vector3 select_pos = GetClosestInteractPoint(character.transform.position);
            float dist = (select_pos - character.transform.position).magnitude;
            return dist <= use_range + character.interact_range;
        }

        public float GetUseRange(PlayerCharacter character)
        {
            return use_range + character.interact_range;
        }

        public bool IsNearCamera(float distance)
        {
            float dist = (transf.position - TheCamera.Get().GetTargetPos()).magnitude;
            return dist < distance;
        }

        public Vector3 GetClosestInteractPoint(Vector3 pos)
        {
            if (type == SelectableType.InteractBound || type == SelectableType.InteractSurface)
                return GetClosestPoint(pos);
            return transf.position; // 如果不是表面，始终从中心交互
        }

        public Vector3 GetClosestInteractPoint(Vector3 pos, Vector3 click_pos)
        {
            if (type == SelectableType.InteractBound)
                return GetClosestPoint(pos);
            if (type == SelectableType.InteractSurface)
                return click_pos; // 如果是表面，交互时使用点击的位置
            return transf.position; // 如果不是表面，始终从中心交互
        }

        public Vector3 GetClosestPoint(Vector3 pos)
        {
            // 不要每帧运行，有点慢
            Vector3 nearest = transf.position;
            float min_dist = (transf.position - pos).magnitude;
            foreach (Collider collide in colliders)
            {
                Vector3 npos = collide.bounds.ClosestPoint(pos);
                float dist = (npos - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = npos;
                }
            }
            return nearest;
        }

        public Destructible Destructible { get { return destruct; } } // 可能为 null，请注意！
        public Character Character { get { return character; } } // 可能为 null，请注意！

        public string GetUID()
        {
            if (unique_id != null)
                return unique_id.unique_id;
            return "";
        }

        public static Selectable GetNearestRaycast(float range = 10f)
        {
            float min_dist = range;
            Selectable nearest = null;
            Vector3 pos = TheCamera.Get().transform.position;
            foreach (Selectable select in PlayerControlsMouse.Get().GetRaycastList())
            {
                Vector3 dist = (select.transf.position - pos);
                if (dist.magnitude < min_dist)
                {
                    nearest = select;
                    min_dist = dist.magnitude;
                }
            }
            return nearest;
        }

        // 获取最近的活跃可选对象
        public static Selectable GetNearest(Vector3 pos, float range = 999f)
        {
            Selectable nearest = null;
            float min_dist = range;
            foreach (Selectable select in active_list)
            {
                if (select.enabled && select.gameObject.activeSelf)
                {
                    float dist = (select.transf.position - pos).magnitude;
                    if (dist < min_dist)
                    {
                        min_dist = dist;
                        nearest = select;
                    }
                }
            }
            return nearest;
        }

        // 获取最近的活跃悬停的可选对象
        public static Selectable GetNearestHover(Vector3 pos, float range = 999f)
        {
            Selectable nearest = null;
            float min_dist = range;
            foreach (Selectable select in active_list)
            {
                if (select.enabled && select.gameObject.activeSelf && select.IsHovered())
                {
                    float dist = (select.transf.position - pos).magnitude;
                    if (dist < min_dist)
                    {
                        min_dist = dist;
                        nearest = select;
                    }
                }
            }
            return nearest;
        }

        // 获取最近的活跃可与之自动交互的可选对象
        public static Selectable GetNearestAutoInteract(Vector3 pos, float range = 999f)
        {
            Selectable nearest = null;
            float min_dist = range;
            foreach (Selectable select in active_list)
            {
                if (select.enabled && select.gameObject.activeSelf && select.CanAutoInteract())
                {
                    float offset = select.type == SelectableType.InteractSurface ? 2f : 0f; // 表面优先级为 2f
                    float dist = (select.GetClosestInteractPoint(pos) - pos).magnitude + offset;
                    if (dist < min_dist)
                    {
                        min_dist = dist;
                        nearest = select;
                    }
                }
            }
            return nearest;
        }

        /// <summary>
        /// 获取最近的活跃属于特定群组的可选对象(Z：比如水源)
        /// </summary>
        /// <param name="group"></param>
        /// <param name="pos"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public static Selectable GetNearestGroup(GroupData group, Vector3 pos, float range = 999f)
        {
            Selectable nearest = null;
            float min_dist = range;
            foreach (Selectable select in active_list)
            {
                if (select.enabled && select.gameObject.activeSelf && select.HasGroup(group))
                {
                    float offset = select.type == SelectableType.InteractSurface ? 1f : 0f; // 表面优先级为 1f
                    float dist = (select.GetClosestInteractPoint(pos) - pos).magnitude + offset;
                    if (dist < min_dist)
                    {
                        min_dist = dist;
                        nearest = select;
                    }
                }
            }
            return nearest;
        }

        // 获取最近的活跃属于数组中任何群组的可选对象
        public static Selectable GetNearestGroup(GroupData[] groups, Vector3 pos, float range = 999f)
        {
            Selectable nearest = null;
            float min_dist = range;
            foreach (Selectable select in active_list)
            {
                if (select.enabled && select.gameObject.activeSelf && select.HasGroup(groups))
                {
                    float offset = select.type == SelectableType.InteractSurface ? 1f : 0f; // 表面优先级为 1f
                    float dist = (select.GetClosestInteractPoint(pos) - pos).magnitude + offset;
                    if (dist < min_dist)
                    {
                        min_dist = dist;
                        nearest = select;
                    }
                }
            }
            return nearest;
        }

        public static Selectable GetByUID(string uid)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                foreach (Selectable select in active_list)
                {
                    if (uid == select.GetUID())
                        return select;
                }
            }
            return null;
        }

        // 获取所有活跃的可选对象（活跃的可选对象是玩家视野中的所有可选对象，太远或超出相机视野之外的对象是非活跃的）
        public static HashSet<Selectable> GetAllActive()
        {
            return active_list;
        }

        // 获取所有可选对象（谨慎使用，对于大地图来说，列表可能非常大，不要每帧都在其中循环）
        public static List<Selectable> GetAll()
        {
            return selectable_list;
        }
    }
}

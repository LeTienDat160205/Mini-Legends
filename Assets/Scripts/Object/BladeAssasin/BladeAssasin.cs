using System.Collections.Generic;
using UnityEngine;

public class BladeAssasin : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private GameObject rangeIndicator;
    [SerializeField] private Stat stat;
    [SerializeField] private TeamMember teamMember;
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTrigger = "Attack";

    private Vector3 indicatorBaseScale;
    private readonly List<TeamMember> enemiesInRange = new List<TeamMember>();
    private readonly Dictionary<TeamMember, GameObject> highlights = new Dictionary<TeamMember, GameObject>();
    private static readonly int AttackX = Animator.StringToHash("AttackX");
    private static readonly int AttackY = Animator.StringToHash("AttackY");

    private void Awake()
    {
        if (stat == null) stat = GetComponent<Stat>();
        if (teamMember == null) teamMember = GetComponent<TeamMember>();
        if (animator == null) animator = GetComponent<Animator>();
        if (rangeIndicator == null) rangeIndicator = FindChild("RangeIndicator");

        if (rangeIndicator != null)
        {
            indicatorBaseScale = rangeIndicator.transform.localScale;
            rangeIndicator.SetActive(false);
        }
    }

    private void Update()
    {
        bool aiming = Input.GetMouseButton(0);
        if (rangeIndicator != null) rangeIndicator.SetActive(aiming);

        UpdateRangeIndicator();


        if (Input.GetMouseButtonDown(0))
            Attack();
    }

    public void Attack()
    {
        if (enemiesInRange.Count == 0) return;

        SetAttackDirectionFromMouse();
        animator?.SetTrigger(attackTrigger);

        foreach (TeamMember enemy in enemiesInRange)
            enemy.SendMessageUpwards("TakeDamage", stat != null ? stat.Attack : 0f,
                SendMessageOptions.DontRequireReceiver);
    }

    private void SetAttackDirectionFromMouse()
    {
        if (animator == null || Camera.main == null) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (Vector2)(mouseWorld - transform.position);
        if (direction.sqrMagnitude < 0.001f) return;

        direction.Normalize();
        animator.SetFloat(AttackX, direction.x);
        animator.SetFloat(AttackY, direction.y);
    }

    private void UpdateRangeIndicator()
    {
        if (rangeIndicator == null) return;

        float attackRange = stat != null ? stat.AttackRange : 1f;
        rangeIndicator.transform.localScale = indicatorBaseScale * Mathf.Max(0.01f, attackRange);
    }

    private GameObject FindChild(string childName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            if (child.name == childName) return child.gameObject;
        return null;
    }
}

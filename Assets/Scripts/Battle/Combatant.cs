using UnityEngine;
using System.Collections.Generic;

public enum Team { Ally, Enemy }
public enum AttackType { Melee, Ranged }

public interface ITargetable
{
    bool IsAlive { get; }
    Vector3 CenterPos { get; }
    void ApplyDamage(int amount);
    Team GetTeam();
}

[RequireComponent(typeof(CharacterController))]
public class Combatant : MonoBehaviour, ITargetable
{
    [Header("Team & Stats")]
    public Team team = Team.Ally;
    public CharacterStats stats = CharacterStats.Basic();
    public int currentHP;

    [Header("Combat")]
    public AttackType attackType = AttackType.Melee;
    [Min(0.1f)] public float attackRange = 2.0f;      // 근접/원거리 공통 사정거리(m)
    [Min(0.05f)] public float attackCooldown = 1.0f;
    public GameObject projectilePrefab;                // Ranged 전용
    public Transform firePoint;                        // 없으면 CenterPos 사용

    [Header("AI")]
    [Tooltip("타깃 재탐색 주기(초) — 이동/공격은 Update에서 매 프레임 처리")]
    public float thinkInterval = 0.2f;
    [Tooltip("타깃에 너무 파고들지 않도록 멈추는 최소 거리")]
    public float stopDistance = 1.0f;
    public LayerMask obstacleMask = ~0;                // (옵션)

    [Header("Center Override (Optional)")]
    [Tooltip("지정 시 이 위치를 캐릭터 중심으로 사용(피격/조준/거리 계산 기준)")]
    public Transform centerPoint;

    // --- Runtime ---
    private CharacterController cc;
    private float lastAttackTime = -999f;
    private ITargetable currentTarget;
    private System.Random rng;

    public bool IsAlive => currentHP > 0;

    // 월드 기준 중심점(메시 피벗/오프셋과 무관하게 일관된 기준 제공)
    public Vector3 CenterPos
    {
        get
        {
            if (centerPoint) return centerPoint.position;
            if (!cc) return transform.position; // Awake 이전 안전
            // CharacterController.center는 로컬 좌표 → 월드로 변환
            return transform.TransformPoint(cc.center);
        }
    }

    public Team GetTeam() => team;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        currentHP = stats.MaxHP;
        rng = new System.Random(GetInstanceID());
    }

    private void OnEnable()
    {
        InvokeRepeating(nameof(Think), 0f, Mathf.Max(0.05f, thinkInterval));
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(Think));
    }

    private void Update()
    {
        if (!IsAlive) return;
        if (currentTarget == null || !currentTarget.IsAlive) return;

        // ---- 이동 ----
        Vector3 to = currentTarget.CenterPos - transform.position;
        to.y = 0f;
        float dist = to.magnitude;

        float moveThreshold = Mathf.Max(attackRange - 0.1f, stopDistance);
        if (dist > moveThreshold)
        {
            Vector3 dir = (dist > 0.0001f) ? to / dist : Vector3.zero;

            // 수평 이동
            cc.Move(dir * stats.SPD * Time.deltaTime);

            // 바라보는 방향 보간
            if (dir.sqrMagnitude > 0.0001f)
                transform.forward = Vector3.Slerp(transform.forward, dir, 0.2f);
        }

        // 약한 접지(메시/피벗 차이로 미세하게 떠 있을 때 안정화)
        cc.Move(Physics.gravity * (0.05f * Time.deltaTime));

        // ---- 공격 ----
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            if (attackType == AttackType.Melee)
            {
                if (dist <= attackRange + 0.05f)
                {
                    DoMeleeHit(currentTarget);
                    lastAttackTime = Time.time;
                }
            }
            else // Ranged
            {
                // 원거리도 사정거리 안일 때만 발사
                if (dist <= attackRange + 0.05f)
                {
                    DoRangedFire(currentTarget);
                    lastAttackTime = Time.time;
                }
            }
        }
    }

    private void Think()
    {
        if (!IsAlive) return;

        if (currentTarget == null || !currentTarget.IsAlive)
            currentTarget = FindNearestEnemy();
    }

    private void DoMeleeHit(ITargetable t)
    {
        var dmg = new DamagePacket { baseDamage = stats.ATK, critChance = 0.15f, critMult = 1.8f };
        int final = dmg.Compute(GetDEF(t), rng);
        t.ApplyDamage(final);
        // TODO: 근접 히트 VFX/사운드
    }

    private void DoRangedFire(ITargetable t)
    {
        if (!projectilePrefab) return;

        Vector3 origin = firePoint ? firePoint.position : CenterPos;
        Vector3 dir = (t.CenterPos - origin);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        Quaternion rot = Quaternion.LookRotation(dir);

        var go = Instantiate(projectilePrefab, origin, rot);
        var proj = go.GetComponent<Projectile>();
        if (!proj) proj = go.AddComponent<Projectile>();

        proj.Init(new DamagePacket { baseDamage = stats.ATK, critChance = 0.1f, critMult = 2.0f },
                  this, t, speed: 18f, lifeTime: 4f);
    }

    private int GetDEF(ITargetable t)
    {
        if (t is Combatant other) return other.stats.DEF;
        return 0;
    }

    public void ApplyDamage(int amount)
    {
        if (!IsAlive) return;
        currentHP -= amount;
        // TODO: HP바/피격 VFX 연동
        if (currentHP <= 0)
        {
            currentHP = 0;
            // 사망 연출 후 비활성화
            gameObject.SetActive(false);
        }
    }

    private ITargetable FindNearestEnemy()
    {
        Combatant[] all = Object.FindObjectsByType<Combatant>(FindObjectsSortMode.None);
        ITargetable best = null;
        float bestDist = float.MaxValue;

        Vector3 my = this.CenterPos;

        foreach (var c in all)
        {
            if (c == this || !c.IsAlive) continue;
            if (c.team == this.team) continue;

            float d = Vector3.Distance(c.CenterPos, my);
            if (d < bestDist)
            {
                bestDist = d;
                best = c;
            }
        }
        return best;
    }

    // ── Gizmos (디버그 시각화) ───────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, attackRange));

        if (Application.isPlaying && currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(this.CenterPos, currentTarget.CenterPos);
            Gizmos.DrawWireSphere(currentTarget.CenterPos, 0.4f);
        }
    }
}

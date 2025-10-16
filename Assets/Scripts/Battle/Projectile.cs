// Projectile.cs
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class Projectile : MonoBehaviour
{
    private DamagePacket dmg;
    private ITargetable owner;
    private ITargetable target;
    private float speed;
    private float life;
    private float spawnTime;

    // (옵션) 타겟 예측 사격을 원하면 lead 계수를 추가할 수 있음
    public void Init(DamagePacket dmg, ITargetable owner, ITargetable target,
                     float speed = 20f, float lifeTime = 3f)
    {
        this.dmg = dmg;
        this.owner = owner;
        this.target = target;
        this.speed = speed;
        this.life = lifeTime;
        spawnTime = Time.time;

        var col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.2f; // 미세 튜닝
    }

    void Update()
    {   
        if (target == null) { Destroy(gameObject); return; }

        if (Time.time - spawnTime > life) { Destroy(gameObject); return; }
        if (target == null || !target.IsAlive) { // 타겟이 죽었거나 없으면 직진 후 소멸
            transform.position += transform.forward * speed * Time.deltaTime;
            return;
        }

        // 단순 유도(없애고 직진만 하게 할 수도)
        Vector3 dir = (target.CenterPos - transform.position);
        dir.y = 0; // 수평만
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);

        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (target == null || !target.IsAlive) return;

        var hit = other.GetComponentInParent<Combatant>();
        if (!hit) return;

        // 같은 팀 피격 방지
        if (owner != null && hit.GetTeam() == owner.GetTeam()) return;

        // 실제 타겟이든, 타겟 근처 아군이든(친선피해 on/off는 여기서 결정)
        if ((object)hit == (object)target)
        {
            // 명중!
            int final = dmg.Compute(hit.stats.DEF, new System.Random(GetInstanceID()));
            hit.ApplyDamage(final);
            // TODO: 히트 VFX/사운드
            Destroy(gameObject);
        }
        else
        {
            // 스쳐지나감(친선피해 끄려면 그냥 무시)
            // Destroy(gameObject); // 벽/장애물에만 파괴하고 싶으면 Layer로 분기
        }
    }
}

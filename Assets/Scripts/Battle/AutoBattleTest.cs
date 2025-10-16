// AutoBattleTest.cs (스폰 위치만 안전하게 개선)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoBattleTest : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject allyPrefab;
    public GameObject enemyPrefab;
    public int allyCount = 3;
    public int enemyCount = 3;

    [Header("Lines (씬에서 직접 위치/방향 지정)")]
    public Transform allyLine;   // 방 안쪽 왼편(또는 아래) 기준점
    public Transform enemyLine;  // 방 안쪽 오른편(또는 위) 기준점
    public float lineSpacing = 2.0f;  // 한 명당 간격
    public bool horizontal = true;    // true: 가로 일렬, false: 세로 일렬

    [Header("Runtime")]
    public Transform runtimeRoot;

    private List<Combatant> allies = new();
    private List<Combatant> enemies = new();

    public event Action<Team> OnBattleFinished;

    void Awake()
    {
        if (!runtimeRoot) runtimeRoot = this.transform;
    }

    public void RunOneBattle(Action<Team> onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(Co_RunOneBattle(onComplete));
    }

    private IEnumerator Co_RunOneBattle(Action<Team> onComplete)
    {
        Cleanup();

        allies = SpawnLine(allyPrefab, Team.Ally, allyCount, allyLine, +1);
        enemies = SpawnLine(enemyPrefab, Team.Enemy, enemyCount, enemyLine, -1);

        foreach (var a in allies) a?.SetEnemyProvider(() => enemies);
        foreach (var e in enemies) e?.SetEnemyProvider(() => allies);

        while (!AllDead(allies) && !AllDead(enemies))
            yield return null;

        Team winner = AllDead(enemies) ? Team.Ally : Team.Enemy;
        OnBattleFinished?.Invoke(winner);
        onComplete?.Invoke(winner);
    }

    private List<Combatant> SpawnLine(GameObject prefab, Team team, int count, Transform line, int dirSign)
    {
        var list = new List<Combatant>(count);
        if (!line) { Debug.LogWarning("라인 앵커가 비었습니다."); return list; }

        Vector3 step = horizontal ? (line.right * lineSpacing * dirSign)
                                  : (line.forward * lineSpacing * dirSign);

        // 가운데 정렬: 첫 번째를 중앙에서 -((n-1)/2)*step 위치에 두고, 이후 +step씩
        Vector3 start = line.position - step * (count - 1) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = start + step * i;
            var go = Instantiate(prefab, pos, Quaternion.LookRotation(-step.normalized, Vector3.up), runtimeRoot);
            var c = go.GetComponent<Combatant>();
            if (!c) c = go.AddComponent<Combatant>();
            c.team = team;
            list.Add(c);
        }
        return list;
    }

    private bool AllDead(List<Combatant> list)
    {
        foreach (var c in list) if (c && c.IsAlive) return false;
        return true;
    }

    public void Cleanup()
    {
        if (runtimeRoot)
        {
            for (int i = runtimeRoot.childCount - 1; i >= 0; --i)
                Destroy(runtimeRoot.GetChild(i).gameObject);
        }
        allies.Clear();
        enemies.Clear();
    }
}

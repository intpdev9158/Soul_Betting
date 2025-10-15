// AutoBattleTest.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoBattleTest : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject allyPrefab;   // Combatant 포함
    public GameObject enemyPrefab;  // Combatant 포함
    public int allyCount = 3;
    public int enemyCount = 3;

    [Header("Layout")]
    public float spacing = 2.5f;
    public Vector3 allyOrigin = new Vector3(-8, 0, 0);
    public Vector3 enemyOrigin = new Vector3(8, 0, 0);

    [Header("Match")]
    public float maxDuration = 120f;

    void Start()
    {
        StartCoroutine(RunMatch());
    }

    IEnumerator RunMatch()
    {
        var allies = SpawnLine(allyPrefab, Team.Ally, allyCount, allyOrigin, Vector3.forward);
        var enemies = SpawnLine(enemyPrefab, Team.Enemy, enemyCount, enemyOrigin, Vector3.forward);

        float t0 = Time.time;

        while (Time.time - t0 < maxDuration)
        {
            if (AllDead(allies)) { Debug.Log("<color=red>패배</color>"); yield break; }
            if (AllDead(enemies)) { Debug.Log("<color=green>승리</color>"); yield break; }
            yield return null;
        }

        Debug.Log("<color=yellow>시간 초과 - 무승부</color>");
    }

    List<Combatant> SpawnLine(GameObject prefab, Team team, int count, Vector3 origin, Vector3 dir)
    {
        var list = new List<Combatant>(count);
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = origin + dir.normalized * (i * spacing);
            var go = Instantiate(prefab, pos, Quaternion.identity);
            var c = go.GetComponent<Combatant>();
            if (!c) c = go.AddComponent<Combatant>();
            c.team = team;
            list.Add(c);
        }
        return list;
    }

    bool AllDead(List<Combatant> list)
    {
        foreach (var c in list) if (c && c.IsAlive) return false;
        return true;
    }
}

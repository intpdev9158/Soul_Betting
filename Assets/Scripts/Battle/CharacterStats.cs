// CharacterStats.cs
using UnityEngine;

[System.Serializable]
public struct CharacterStats
{
    public int MaxHP;
    public int ATK;     // 공격력
    public int DEF;     // 방어력 (피해 경감)
    public float SPD;   // 이속 (m/s)

    public static CharacterStats Basic(int hp=100, int atk=10, int def=0, float spd=4f)
        => new CharacterStats { MaxHP = hp, ATK = atk, DEF = def, SPD = spd };
}

public struct DamagePacket
{
    public int baseDamage;
    public float critChance;   // 0~1
    public float critMult;     // 2.0 같은 배수

    public int Compute(int targetDEF, System.Random rng)
    {
        // 간단 공식: (ATK - DEF) 최소 1, 크리티컬 시 배수
        int raw = Mathf.Max(1, baseDamage - targetDEF);
        bool crit = rng.NextDouble() < critChance;
        float mul = crit ? critMult : 1f;
        return Mathf.Max(1, Mathf.RoundToInt(raw * mul));
    }
}

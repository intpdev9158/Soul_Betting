using System.Collections.Generic;
using UnityEngine;

public class DungeonGen : MonoBehaviour
{
    [Header("Prefabs & Root")]
    [Tooltip("방 프리팹(문/캡만 포함, 피벗=방 중앙 바닥)")]
    public GameObject roomShellPrefab;
    [Tooltip("가로 복도 프리팹(길이=CorridorLen, 피벗=정중앙, +X 방향)")]
    public GameObject corridorPrefabX;
    [Tooltip("세로 복도 프리팹(길이=CorridorLen, 피벗=정중앙, +Z 방향)")]
    public GameObject corridorPrefabZ;
    public Transform root;

    [Header("World Sizes (정배수, 스케일 변경 없음)")]
    public float RoomSize = 20f;     // 방 외곽 길이
    public float CorridorLen = 20f;   // 복도 길이

    [Header("Generation")]
    [Min(1)] public int maxRooms = 20;           // 전체 방 수 상한
    [Range(1,4)] public int maxOpeningsPerRoom = 3; // 한 방이 가질 수 있는 최대 연결(1~4)
    [Range(0f,1f)] public float extraBranchChance = 0.6f; // 같은 방에서 추가 분기 시도 확률
    public bool allowLoops = true;               // 이미 존재하는 이웃과도 연결(루프 허용)
    public int seed = 0;
    public bool autoGenerate = true;

    // 내부 상태
    private System.Random rng;
    private readonly Dictionary<Vector3Int, RoomData> map = new();
    private readonly List<Vector3Int> frontier = new();

    private float Pitch => RoomSize + CorridorLen;

    // === 자료형 ===
    public struct RoomData
    {
        public Vector3Int grid; // 피치 격자 좌표
        public int doorMask;    // 4비트: X+, X-, Z+, Z-

        public RoomData(Vector3Int g) { grid = g; doorMask = 0; }
    }

    void Start()
    {
        if (autoGenerate) GenerateDungeon();
    }


    [ContextMenu("Generate Dungeon")]
    public void GenerateDungeon()
    {
        // 정리
        if (root != null)
        {
            for (int i = root.childCount - 1; i >= 0; --i)
                DestroyImmediate(root.GetChild(i).gameObject);
        }
        map.Clear();
        frontier.Clear();

        rng = (seed == 0) ? new System.Random() : new System.Random(seed);

        // 시작 방
        var start = AddRoom(Vector3Int.zero);
        frontier.Add(start.grid);

        // 메인 루프
        while (map.Count < maxRooms && frontier.Count > 0)
        {
            // 무작위 프런티어 선택
            int idx = rng.Next(frontier.Count);
            var cur = frontier[idx];

            bool grew = TryCarveFrom(cur);

            // 더 이상 확장 못 하면 프런티어에서 제거
            if (!grew) frontier.RemoveAt(idx);
        }

        // 배치
        foreach (var kv in map) BuildRoom(kv.Value);
        foreach (var kv in map) BuildCorridors(kv.Key, kv.Value);
    }

    // === 그래프 생성 로직 ===

    private bool TryCarveFrom(Vector3Int cur)
    {
        if (!map.TryGetValue(cur, out var curData)) return false;

        // 현재 열린 수
        int openings = CountBits4(curData.doorMask);
        if (openings >= maxOpeningsPerRoom) return false;

        // 방향 섞기
        var dirs = new List<Dir> { Dir.XPlus, Dir.XMinus, Dir.ZPlus, Dir.ZMinus };
        Shuffle(dirs);

        bool addedAny = false;

        // 가능한 모든 방향을 훑되,
        // maxOpeningsPerRoom까지, maxRooms를 넘지 않게 확장
        foreach (var d in dirs)
        {
            if (openings >= maxOpeningsPerRoom) break;

            var next = cur + DirUtil.Step(d);

            // 이미 이웃이 있는가?
            if (map.ContainsKey(next))
            {
                // 이미 연결돼 있으면 패스
                if (IsConnected(curData, map[next], d)) continue;

                // 루프 허용이면 연결만 추가(방 수 증가 없음)
                if (allowLoops)
                {
                    Connect(cur, next, d);
                    openings++;
                    addedAny = true;
                }

                // 루프 비허용이면 스킵
                continue;
            }

            // 새 방을 둘 수 있고 전체 수를 넘기지 않는가?
            if (map.Count >= maxRooms) break;

            // 분기 확률 체크(첫 연결은 보장, 이후는 확률로)
            if (openings > 0 && rng.NextDouble() > extraBranchChance) continue;

            // 신규 방 추가 + 연결
            AddAndConnect(cur, next, d);
            openings++;
            addedAny = true;

            // 다음 분기 계속 시도할 수 있음(확률/제한에 따름)
        }

        return addedAny;
    }

    private void AddAndConnect(Vector3Int a, Vector3Int b, Dir dirAB)
    {
        AddRoom(b);
        Connect(a, b, dirAB);
        // 새 이웃도 프런티어 후보에 올림
        if (!frontier.Contains(b)) frontier.Add(b);
    }

    private void Connect(Vector3Int a, Vector3Int b, Dir dirAB)
    {
        var A = map[a];
        var B = map[b];

        A.doorMask |= (1 << DirUtil.Bit(dirAB));
        B.doorMask |= (1 << DirUtil.Bit(DirUtil.Opp(dirAB)));

        map[a] = A;
        map[b] = B;
    }

    private bool IsConnected(RoomData a, RoomData b, Dir dirAB)
    {
        int ba = 1 << DirUtil.Bit(dirAB);
        int bb = 1 << DirUtil.Bit(DirUtil.Opp(dirAB));
        return ((a.doorMask & ba) != 0) && ((b.doorMask & bb) != 0);
    }

    private RoomData AddRoom(Vector3Int grid)
    {
        var data = new RoomData(grid);
        map[grid] = data;
        return data;
    }

    private int CountBits4(int mask)
    {
        int c = 0;
        for (int i = 0; i < 4; i++) if ((mask & (1 << i)) != 0) c++;
        return c;
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // === 배치 ===

    private void BuildRoom(RoomData r)
    {
        Vector3 world = new Vector3(r.grid.x * Pitch, 0f, r.grid.z * Pitch);
        var go = Instantiate(roomShellPrefab, world, Quaternion.identity, root);

        var comp = go.GetComponent<Room>(); // 문/캡 토글
        if (comp) comp.ApplyDoorMask(r.doorMask);
    }

    private void BuildCorridors(Vector3Int grid, RoomData r)
    {
        Vector3 a = new Vector3(grid.x * Pitch, 0f, grid.z * Pitch);

        // X+ 복도는 한 번만(소유 규칙)
        if ((r.doorMask & (1 << DirUtil.Bit(Dir.XPlus))) != 0)
        {
            var nb = grid + DirUtil.Step(Dir.XPlus);
            if (map.ContainsKey(nb))
            {
                Vector3 b = new Vector3(nb.x * Pitch, 0f, nb.z * Pitch);
                Vector3 mid = (a + b) * 0.5f;
                Instantiate(corridorPrefabX, mid, Quaternion.identity, root);
            }
        }

        // Z+ 복도도 한 번만
        if ((r.doorMask & (1 << DirUtil.Bit(Dir.ZPlus))) != 0)
        {
            var nb = grid + DirUtil.Step(Dir.ZPlus);
            if (map.ContainsKey(nb))
            {
                Vector3 b = new Vector3(nb.x * Pitch, 0f, nb.z * Pitch);
                Vector3 mid = (a + b) * 0.5f;
                Instantiate(corridorPrefabZ, mid, Quaternion.identity, root);
            }
        }
    }
}

// === 방향 유틸 ===
public enum Dir { XPlus, XMinus, ZPlus, ZMinus }

public static class DirUtil
{   
    public static Vector3Int Step(Dir d) => d switch
    {
        Dir.XPlus => new(1, 0, 0),
        Dir.XMinus => new(-1, 0, 0),
        Dir.ZPlus => new(0, 0, 1),
        Dir.ZMinus => new(0, 0, -1),
        _ => Vector3Int.zero
    };

    public static int Bit(Dir d) => d switch
    {
        Dir.XPlus => 0, Dir.XMinus => 1, Dir.ZPlus => 2, Dir.ZMinus => 3, _ => 0
    };

    public static Dir Opp(Dir d) => d switch
    {
        Dir.XPlus => Dir.XMinus, Dir.XMinus => Dir.XPlus,
        Dir.ZPlus => Dir.ZMinus, Dir.ZMinus => Dir.ZPlus, _ => d
    };
}

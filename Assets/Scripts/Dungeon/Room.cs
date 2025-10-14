using UnityEngine;

[System.Serializable]
public class WallSet
{
    public Dir dir;
    public GameObject door; // 열릴 때 ON
    public GameObject wall;  // 닫힐 때 ON
}

public class Room : MonoBehaviour
{
    public WallSet[] walls;

    // doorMask: 4비트 (X+, X-, Z+, Z-)
    public void ApplyDoorMask(int doorMask)
    {
        foreach (var w in walls)
        {
            int bit = DirUtil.Bit(w.dir);
            bool open = (doorMask & (1 << bit)) != 0;

            if (w.door) w.door.SetActive(open);
            if (w.wall) w.wall.SetActive(!open);
        }
    }
}

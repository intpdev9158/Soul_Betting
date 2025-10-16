using UnityEngine;

public class GameWallet : MonoBehaviour
{
    public static GameWallet I { get; private set; }

    [Header("초기 코인")]
    public int startCoins = 1000;
    public int Coins { get; private set; }

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        Coins = startCoins;
    }

    public void Add(int amount)  => Coins += amount;

    public bool TrySpend(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        return true;
    }
}

using Alchemy.Inspector;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int Fares { get; private set; }

    public event System.Action<int> OnFareCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [Header("Debug (read-only)")]
    [SerializeField, ReadOnly] private int debugFares;

    public void AddFare()
    {
        Debug.Log($"Fare completed — total fares: {Fares}");
        Fares++;
        debugFares = Fares;
        OnFareCompleted?.Invoke(Fares);
    }
    public void ResetShift()
    {
        Fares = 0;
    }
}

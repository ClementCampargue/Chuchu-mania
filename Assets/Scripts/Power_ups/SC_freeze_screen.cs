using UnityEngine;

public class SC_freeze_screen : MonoBehaviour
{
    public float freezeDuration = 1f;
    public static bool freeze;
    public static SC_freeze_screen Instance;
    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
    }
    public void FreezeAllExceptPlayer()
    {
        freeze = true;
        Invoke(nameof(UnfreezeAll), freezeDuration);
    }

    void UnfreezeAll()
    {
        freeze = false;

    }
}
using UnityEngine;

public class SC_discord_manager : MonoBehaviour
{
    public static SC_discord_manager Instance;

    private Discord.Discord discord;
    private Discord.ActivityManager activityManager;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

    }

    private void Start()
    {
        // Ne crée Discord qu'une seule fois
        discord = new Discord.Discord(
            1536726840159244340,
            (ulong)Discord.CreateFlags.NoRequireDiscord
        );

        activityManager = discord.GetActivityManager();

        // Présence initiale
        ChangeActivity("Playing", "");
    }

    public void ChangeActivity(string state, string largeImage)
    {
        if (discord == null || activityManager == null)
            return;

        Discord.Activity activity = new Discord.Activity
        {
            State = state
        };

        if (!string.IsNullOrEmpty(largeImage))
        {
            activity.Assets.LargeImage = largeImage;
        }

        activityManager.UpdateActivity(activity, (result) =>
        {
            Debug.Log("Discord Activity updated : " + result);
        });
    }

    private void Update()
    {
        discord?.RunCallbacks();
    }

    private void OnApplicationQuit()
    {
        discord?.Dispose();
        discord = null;
    }

    private void OnDestroy()
    {
        discord?.Dispose();
        discord = null;
    }
}

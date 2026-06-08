using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SC_rumbleManager : MonoBehaviour
{
    public static SC_rumbleManager instance;

    private Coroutine rumbleRoutine;

    private float currentPriority = 0f;
    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

    }
    public void PlayRumble(float low, float high, float duration, float priority = 1f)
    {
        if (!SC_controller_manager.instance.using_controller)
            return;
        if (!SC_controller_manager.instance.using_controller)
        {
            return;
        }

        var gamepad = Gamepad.current;
        if (gamepad == null)
            return;

        if (PlayerPrefs.GetInt("Rumble", 1) == 0)
            return;

        if (rumbleRoutine != null && priority < currentPriority)
            return;

        currentPriority = priority;

        if (rumbleRoutine != null)
            StopCoroutine(rumbleRoutine);

        rumbleRoutine = StartCoroutine(RumbleCoroutine(low, high, duration, priority));
    }

    private IEnumerator RumbleCoroutine(float low, float high, float duration, float priority)
    {
        var gamepad = Gamepad.current;
        if (gamepad == null)
            yield break;

        gamepad.SetMotorSpeeds(low, high);

        yield return new WaitForSecondsRealtime(duration);

        if (priority >= currentPriority)
        {
            gamepad.SetMotorSpeeds(0f, 0f);
            currentPriority = 0f;
        }

        rumbleRoutine = null;
    }

    public void StopRumble()
    {
        var gamepad = Gamepad.current;
        if (gamepad != null)
            gamepad.SetMotorSpeeds(0f, 0f);

        if (rumbleRoutine != null)
        {
            StopCoroutine(rumbleRoutine);
            rumbleRoutine = null;
        }

        currentPriority = 0f;
    }

    private void OnDisable()
    {
        StopRumble();
    }

    private void OnDestroy()
    {
        StopRumble();
    }
}
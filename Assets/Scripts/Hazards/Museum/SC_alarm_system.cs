using System.Collections.Generic;
using UnityEngine;

public class SC_alarm_system : MonoBehaviour
{
    public static SC_alarm_system Instance;

    [Header("Alarm")]
    [SerializeField] public bool alarmActive = false;

    // Liste NON static
    public List<SC_levier> levers = new List<SC_levier>();
    public List<SC_guard_movement> guards = new List<SC_guard_movement>();

    public AudioClip default_music;
    public AudioClip alarm_music;
    public Animator anim;
    public SC_enemy_track_player robot;

    public bool hidden;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            StartAlarm();
        }
    }

    // =========================================================
    // ENREGISTREMENT DES LEVIERS
    // =========================================================

    public void RegisterLever(SC_levier lever)
    {
        if (lever == null)
            return;

        if (!levers.Contains(lever))
        {
            levers.Add(lever);
        }
    }

    public void UnregisterLever(SC_levier lever)
    {
        if (lever == null)
            return;

        levers.Remove(lever);
    }

    // =========================================================
    // DECLENCHEMENT DE L'ALARME
    // =========================================================

    public void StartAlarm()
    {
        if (hidden)
            return;

        if (alarmActive)
            return;

        anim.enabled = true;
        robot.start_tracking();

        alarmActive = true;

        anim.SetBool("on", true);

        SC_music_manager.instance.update_music(alarm_music);

        Debug.Log("ALARME ACTIVÉE");

        // Active tous les leviers
        foreach (SC_levier lever in levers)
        {
            if (lever != null)
            {
                lever.ActivateLever();
            }
        }

        foreach (SC_guard_movement guard in guards)
        {
           // guard.SetChaseMode(true);
        }
    }

    // =========================================================
    // LEVIER ACTIVÉ
    // =========================================================

    public void LeverActivated(SC_levier lever)
    {
        CheckAllLevers();
    }

    // =========================================================
    // VERIFICATION
    // =========================================================

    private void CheckAllLevers()
    {
        if (!alarmActive)
            return;

        foreach (SC_levier lever in levers)
        {
            if (lever == null)
                continue;

            if (lever.IsActivated())
            {
                return;
            }
        }

        StopAlarm();
    }

    // =========================================================
    // ARRET DE L'ALARME
    // =========================================================

    public void StopAlarm()
    {
        if (!alarmActive)
            return;

        anim.SetBool("on", false);

        robot.end_tracking();

        SC_music_manager.instance.update_music(default_music);

        alarmActive = false;

        foreach (SC_guard_movement guard in guards)
        {
           // guard.SetChaseMode(false);
        }

        Debug.Log("ALARME ARRÊTÉE");
    }

    // =========================================================
    // UTILITAIRE
    // =========================================================

    public bool IsAlarmActive()
    {
        return alarmActive;
    }
}

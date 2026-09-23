using UnityEngine;

public class SC_spot : MonoBehaviour
{
    private SC_alarm_system alarm_system;
    public Animator animator;

    void Start()
    {
        alarm_system = SC_alarm_system.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !alarm_system.alarmActive)
        {
            alarm_system.StartAlarm();
            if(animator != null) {
            animator.SetBool("on",true);
        }
        }
    }
}


using UnityEngine;
using UnityEngine.Events;

public class AnimationEventTrigger : MonoBehaviour
{
    [Header("Animation à surveiller")]
    public string animationName;

    [Header("Déclenchement avant la fin (en secondes)")]
    public float triggerBeforeEnd = 0.2f;

    [Header("Event à déclencher")]
    public UnityEvent onAnimationFinished;

    private Animator animator;
    private bool eventTriggered;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        if (state.IsName(animationName))
        {
            float remainingTime = (1f - (state.normalizedTime % 1f))
                                  * state.length;

            if (!eventTriggered && remainingTime <= triggerBeforeEnd
                && !animator.IsInTransition(0))
            {
                eventTriggered = true;
                onAnimationFinished?.Invoke();
            }
        }
        else
        {
            eventTriggered = false;
        }
    }
}

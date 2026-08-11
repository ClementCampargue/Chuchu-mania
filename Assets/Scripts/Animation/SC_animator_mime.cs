using UnityEngine;

public class SC_animator_mime : MonoBehaviour
{
    public Animator source;
    public Animator target;

    private void Update()
    {
        if (source == null || target == null)
            return;

        // Synchronise la vitesse
        target.speed = source.speed;

        // Synchronise les paramètres
        foreach (AnimatorControllerParameter parameter in source.parameters)
        {
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Float:
                    target.SetFloat(
                        parameter.nameHash,
                        source.GetFloat(parameter.nameHash)
                    );
                    break;

                case AnimatorControllerParameterType.Int:
                    target.SetInteger(
                        parameter.nameHash,
                        source.GetInteger(parameter.nameHash)
                    );
                    break;

                case AnimatorControllerParameterType.Bool:
                    target.SetBool(
                        parameter.nameHash,
                        source.GetBool(parameter.nameHash)
                    );
                    break;
            }
        }

        // Synchronise les states
        int layers = Mathf.Min(source.layerCount, target.layerCount);

        for (int i = 0; i < layers; i++)
        {
            AnimatorStateInfo sourceState = source.GetCurrentAnimatorStateInfo(i);

            target.Play(
                sourceState.fullPathHash,
                i,
                sourceState.normalizedTime
            );
        }
    }
}
using UnityEngine;

public class AnimatorTargetBubbleTune : MonoBehaviour
{
    private Animator Animator => GetComponent<Animator>();

    private void Update()
    {
        Animator.SetBool("Shine", !GetComponentInParent<TargetBubble>().enabled);
        var anyActive = false;
        foreach (var targetBubble in FindObjectsByType<TargetBubble>(FindObjectsSortMode.None))
            if (targetBubble.enabled)
            {
                anyActive = true;
                break;
            }

        Animator.SetBool("Dead", !anyActive);
    }

    public void StarDead()
    {
        GetComponentInParent<TargetBubble>().onDead.Invoke();
    }
}
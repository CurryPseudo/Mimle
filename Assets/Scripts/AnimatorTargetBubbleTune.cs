using UnityEngine;

public class AnimatorTargetBubbleTune : MonoBehaviour
{
    private Animator Animator => GetComponent<Animator>();

    private void Update()
    {
        Animator.SetBool("Shine", !GetComponentInParent<TargetBubble>().enabled);
    }
}
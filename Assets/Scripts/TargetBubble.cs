using UnityEngine;
using UnityEngine.Events;

public class TargetBubble : MonoBehaviour
{
    public UnityEvent onDead = new();
    public bool isDead;

    private void Start()
    {
        onDead.AddListener(Dead);
    }

    private void Dead()
    {
        isDead = true;
    }
}
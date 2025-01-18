using Unity.Mathematics;
using UnityEngine;

public class TargetBubbleTune : MonoBehaviour
{
    public Transform star;
    public float starFinalScale = 5.0f;
    public float starFinalScaleTime = 1.0f;
    private float _starInitialScale = 1.0f;

    private float StarTransitionValue
    {
        get => math.unlerp(_starInitialScale, starFinalScale, star.transform.localScale.x);
        set => star.transform.localScale = Vector3.one * Mathf.Lerp(_starInitialScale, starFinalScale, value);
    }

    public TargetBubble TargetBubble => GetComponent<TargetBubble>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        _starInitialScale = star.localScale.x;
    }

    private void Update()
    {
        var sign = TargetBubble.enabled ? -1.0f : 1.0f;
        var speed = 1.0f / starFinalScaleTime;
        StarTransitionValue += sign * speed * Time.deltaTime;
        StarTransitionValue = math.clamp(StarTransitionValue, 0.0f, 1.0f);
    }
}
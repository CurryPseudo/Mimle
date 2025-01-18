using UnityEngine;

[RequireComponent(typeof(BubbleColor), typeof(TargetBubble), typeof(SpriteRenderer))]
public class DevelopBubbleColorTune : MonoBehaviour
{
    private Color _originalColor;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        _originalColor = GetComponent<SpriteRenderer>().color;
    }

    // Update is called once per frame
    private void Update()
    {
        if (GetComponent<TargetBubble>().enabled)
            GetComponent<SpriteRenderer>().color = _originalColor;
        else
            GetComponent<SpriteRenderer>().color = GetComponent<BubbleColor>().color;
    }
}
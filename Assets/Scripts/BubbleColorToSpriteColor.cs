using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[ExecuteInEditMode]
public class BubbleColorToSpriteColor : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
        var bubbleColor = GetComponent<BubbleColor>();
        if (bubbleColor == null) bubbleColor = GetComponentInParent<BubbleColor>();

        if (bubbleColor == null) return;
        GetComponent<SpriteRenderer>().color = bubbleColor.color;
    }
}
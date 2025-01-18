using UnityEngine;

[RequireComponent(typeof(BubbleColor), typeof(SpriteRenderer))]
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
        GetComponent<SpriteRenderer>().color = GetComponent<BubbleColor>().color;
    }
}
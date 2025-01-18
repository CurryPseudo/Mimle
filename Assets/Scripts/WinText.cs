using System.Linq;
using TMPro;
using UnityEngine;

public class WinText : MonoBehaviour
{
    private TextMeshProUGUI TextMesh => GetComponent<TextMeshProUGUI>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
        var bubbles = FindObjectsByType<TargetBubble>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ToList();
        bubbles.RemoveAll(targetBubble => !targetBubble.enabled);
        var bubbleCount = bubbles.Count;
        TextMesh.enabled = bubbleCount == 0;
    }
}
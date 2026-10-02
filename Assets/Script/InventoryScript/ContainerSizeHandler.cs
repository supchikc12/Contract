using UnityEngine;

public class ContainerSizeHandler : MonoBehaviour
{
    [SerializeField] private RectTransform containerRect;
    [SerializeField] private RectTransform gridRect;
    [SerializeField] private Vector2 padding = new Vector2(70f, 100f);
    [SerializeField] private float maxWidth = 1250f;
    [SerializeField] private float maxHeight = 800f;
    [SerializeField] private float minWidth = 200f;
    [SerializeField] private float minHeight = 200f;

    private void Update()
    {
        if (containerRect == null || gridRect == null) return;

        Vector2 targetSize = gridRect.sizeDelta + padding;
        float width = Mathf.Clamp(targetSize.x, minWidth, maxWidth);
        float height = Mathf.Clamp(targetSize.y, minHeight, maxHeight);
        containerRect.sizeDelta = new Vector2(width, height);

        containerRect.sizeDelta = new Vector2(width, height);
    }
}

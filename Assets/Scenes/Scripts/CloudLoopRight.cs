using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class CloudLoopRight : MonoBehaviour
{
    public float speed = 30f;           // How fast it moves right
    public bool useUnscaledTime = true; // Keep moving even if Time.timeScale is 0
    public RectTransform partner;       // Optional: assign a duplicate; auto-created if null

    private RectTransform rectTransform;
    private RectTransform parentRect;
    private float tileWidth;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRect = rectTransform.parent as RectTransform;
    }

    void Start()
    {
        tileWidth = rectTransform.rect.width;

        if (partner == null)
        {
            GameObject clone = Instantiate(gameObject, rectTransform.parent);
            partner = clone.GetComponent<RectTransform>();
            CloudLoopRight cloneScript = clone.GetComponent<CloudLoopRight>();
            if (cloneScript != null)
            {
                Destroy(cloneScript);
            }

            // Keep the partner on the same UI layer (same sibling index).
            int index = rectTransform.GetSiblingIndex();
            partner.SetSiblingIndex(index);
        }

        // Place partner to the left so there is always a tile behind.
        Vector2 p = rectTransform.anchoredPosition;
        partner.anchoredPosition = new Vector2(p.x - tileWidth, p.y);
    }

    void Update()
    {
        // Move to the right
        float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        rectTransform.anchoredPosition += Vector2.right * speed * delta;
        partner.anchoredPosition += Vector2.right * speed * delta;

        if (parentRect == null)
        {
            return;
        }

        float viewLeft = parentRect.rect.xMin;
        float half = tileWidth * 0.5f;

        RectTransform left = rectTransform.anchoredPosition.x <= partner.anchoredPosition.x ? rectTransform : partner;
        RectTransform right = left == rectTransform ? partner : rectTransform;

        float leftEdge = left.anchoredPosition.x - half;

        // If the left tile's left edge is inside view, move the right tile behind it.
        if (leftEdge > viewLeft)
        {
            right.anchoredPosition = new Vector2(left.anchoredPosition.x - tileWidth, right.anchoredPosition.y);
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SwipeNavigation : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public RectTransform contentPanel; // Parent panel that holds all child screens
    public Button nextButton, prevButton;
    public float swipeThreshold = 0.2f; // Minimum swipe distance as a fraction of screen width
    public float smoothTime = 0.3f; // Transition duration

    private int totalPages;
    private int currentPage = 0;
    public int targetSwipePos;
    private Vector2 targetPosition;
    private Vector2 velocity = Vector2.zero;

    void Start()
    {
        totalPages = contentPanel.childCount;
        targetPosition = contentPanel.anchoredPosition;
        UpdateButtonVisibility();

        nextButton.onClick.AddListener(() => Navigate(1));
        prevButton.onClick.AddListener(() => Navigate(-1));
    }

    void Update()
    {
        contentPanel.anchoredPosition = Vector2.SmoothDamp(contentPanel.anchoredPosition, targetPosition, ref velocity, smoothTime);
    }

    public void OnDrag(PointerEventData eventData)
    {
        contentPanel.anchoredPosition += new Vector2(eventData.delta.x, 0);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float screenWidth = Screen.width;
        float dragDistance = eventData.position.x - eventData.pressPosition.x;

        if (Mathf.Abs(dragDistance) > screenWidth * swipeThreshold)
        {
            if (dragDistance < 0 && currentPage < totalPages - 1)
                Navigate(1);
            else if (dragDistance > 0 && currentPage > 0)
                Navigate(-1);
        }
        else
        {
            SetPage(currentPage);
        }
    }

    private void Navigate(int direction)
    {
        currentPage = Mathf.Clamp(currentPage + direction, 0, totalPages - 1);
        SetPage(currentPage);
    }

    private void SetPage(int page)
    {
        targetPosition = new Vector2(-page * Screen.width, 0);
        UpdateButtonVisibility();
    }

    private void UpdateButtonVisibility()
    {
        prevButton.gameObject.SetActive(currentPage > 0);
        nextButton.gameObject.SetActive(currentPage < totalPages - 1);
    }
}

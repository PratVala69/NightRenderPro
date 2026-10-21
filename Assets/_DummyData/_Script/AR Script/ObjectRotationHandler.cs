using UnityEngine;

public class ObjectRotationHandler : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 100f; // Degrees per second
    [SerializeField] private Vector3 rotationAxis = Vector3.up; // Default: Y-axis
    private bool isDragging = false;
    private Vector2 lastTouchPosition;

    void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // Handle touch input (mobile)
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                isDragging = true;
                lastTouchPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Moved && isDragging)
            {
                Vector2 currentTouchPosition = touch.position;
                float deltaX = currentTouchPosition.x - lastTouchPosition.x;
                RotateObject(deltaX);
                lastTouchPosition = currentTouchPosition;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isDragging = false;
            }
        }
        // Handle mouse input (desktop)
        else if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastTouchPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            Vector2 currentMousePosition = Input.mousePosition;
            float deltaX = currentMousePosition.x - lastTouchPosition.x;
            RotateObject(deltaX);
            lastTouchPosition = currentMousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    private void RotateObject(float delta)
    {
        // Rotate in one direction based on drag (positive delta = clockwise, negative = counterclockwise)
        float rotationAmount = delta * rotationSpeed * Time.deltaTime;
        transform.Rotate(rotationAxis, rotationAmount, Space.World);
    }
}
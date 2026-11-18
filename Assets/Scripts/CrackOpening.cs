using UnityEngine;

public class CrackOpening : MonoBehaviour
{

    [SerializeField] private Transform leftEdge;
    [SerializeField] private Transform rightEdge;
    [SerializeField] private GameObject leftGrabInteraction;
    [SerializeField] private GameObject rightGrabInteraction;

    [SerializeField] private float maxTravelDistance = 0.3f;
    [SerializeField] private float openThreshold = 0.9f;

    private float leftStartX;
    private float rightStartX;
    private bool isOpen = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        leftStartX = leftEdge.localPosition.x;
        rightStartX = rightEdge.localPosition.x;
    }

    // Update is called once per frame
    void Update()
    {
        if (isOpen)
            return;

        float leftDistance = leftStartX - leftEdge.localPosition.x;
        float rightDistance = rightEdge.localPosition.x - rightStartX;    

        float leftProgress = Mathf.Clamp01(leftDistance / maxTravelDistance);
        float rightProgress = Mathf.Clamp01(rightDistance / maxTravelDistance);

        if (leftProgress >= openThreshold && rightProgress >= openThreshold)
        {
            isOpen = true;
            LockCrackOpen();
            // Optionally, you can add any additional logic here when the crack is fully open
        }
    }

    private void LockCrackOpen()
    {
        // Disable grabbing on both edges
        leftGrabInteraction.SetActive(false);
        rightGrabInteraction.SetActive(false);

        // Snap the edges to their fully open positions
        Vector3 leftPosition = leftEdge.localPosition;
        leftPosition.x = leftStartX - maxTravelDistance;
        leftEdge.localPosition = leftPosition;

        Vector3 rightPosition = rightEdge.localPosition;
        rightPosition.x = rightStartX + maxTravelDistance;
        rightEdge.localPosition = rightPosition;

        // Stop physics from moving the edges
        leftEdge.GetComponent<Rigidbody>().isKinematic = true;
        rightEdge.GetComponent<Rigidbody>().isKinematic = true;

        Debug.Log("Crack locked open");
    }


    
}

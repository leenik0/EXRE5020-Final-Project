using UnityEngine;

public class CrackDiscovery : MonoBehaviour
{

    public Transform playerHead;
    public float revealDistance = 1.5f;
    public float fadeDuration = 1.5f;

    private MeshRenderer cubeRenderer;
    private bool isRevealed = false;
    private AudioSource audioSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cubeRenderer = GetComponent<MeshRenderer>();
        cubeRenderer.enabled = false; // Initially hide the cube
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isRevealed || playerHead == null)
            return;

            // Calculate the distance between the player's head and the cube
            float distance = Vector3.Distance(
                playerHead.position, 
                transform.position      
            );

        if (distance <= revealDistance)
        {
            cubeRenderer.enabled = true; // Reveal the cube
            isRevealed = true; // Mark as revealed to prevent further checks

            StartCoroutine(FadeOutAudio());
        }    
    }

   private System.Collections.IEnumerator FadeOutAudio()
{
    if (audioSource == null)
        yield break;

    float startingVolume = audioSource.volume;
    float elapsedTime = 0f;

    while (elapsedTime < fadeDuration)
    {
        elapsedTime += Time.deltaTime;

        audioSource.volume = Mathf.Lerp(
            startingVolume,
            0f,
            elapsedTime / fadeDuration
        );

        yield return null;
    }

    audioSource.volume = 0f;
    audioSource.Stop();
}
}

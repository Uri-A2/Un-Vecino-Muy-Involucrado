
using UnityEngine;
using System.Collections;

public class CameraMenuController : MonoBehaviour
{
    [Header("Camera References")]
    public Transform mainCamera;
    public Transform mainCameraPosition;
    public Transform optionsCameraPosition;

    [Header("Canvas References")]
    public GameObject mainCanvas;
    public GameObject optionsCanvas;

    [Header("Settings")]
    public float transitionDuration = 1.5f;

    private bool isTransitioning = false;

    public void OpenOptions()
    {
        if (!isTransitioning)
        {
            StartCoroutine(MoveCamera(
                optionsCameraPosition,
                mainCanvas,
                optionsCanvas
            ));
        }
    }

    private IEnumerator MoveCamera(
        Transform targetPosition,
        GameObject currentCanvas,
        GameObject nextCanvas)
    {
        isTransitioning = true;

        Vector3 startPosition = mainCamera.position;
        Quaternion startRotation = mainCamera.rotation;

        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = elapsedTime / transitionDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            mainCamera.position = Vector3.Lerp(
                startPosition,
                targetPosition.position,
                t
            );

            mainCamera.rotation = Quaternion.Slerp(
                startRotation,
                targetPosition.rotation,
                t
            );

            yield return null;
        }

        mainCamera.position = targetPosition.position;
        mainCamera.rotation = targetPosition.rotation;

        currentCanvas.SetActive(false);
        nextCanvas.SetActive(true);

        isTransitioning = false;
    }

    public void CloseOptions()
    {
        if (!isTransitioning)
        {
            StartCoroutine(MoveCamera(
                mainCameraPosition,
                optionsCanvas,
                mainCanvas
            ));
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CinematicIntroCameraManager : MonoBehaviour
{
    public Camera mainCamera;
    public List<Transform> cameraPositions;
    public float moveDuration = 5f;
    private Coroutine currentMovement;

    public bool IsMoving { get; private set; }

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Start()
    {
        if (mainCamera == null)
        {
            Debug.LogError("Camera is not set in CinematicIntroCameraManager and no Main Camera was found");
        }

        if (cameraPositions.Count == 0)
        {
            Debug.LogError("No camera positions set in CinematicIntroCameraManager");
        }
    }

    public void MoveCameraToPosition(int positionIndex)
    {
        if (positionIndex < 0 || positionIndex >= cameraPositions.Count)
        {
            Debug.LogError("Invalid position index: " + positionIndex);
            return;
        }

        if (currentMovement != null)
        {
            StopCoroutine(currentMovement);
        }

        currentMovement = StartCoroutine(MoveCameraToPositionCoroutine(cameraPositions[positionIndex]));
    }

    public void MoveCameraToPositionWithFOV(int positionIndex, float duration, float targetFOV)
    {
        if (positionIndex < 0 || positionIndex >= cameraPositions.Count)
        {
            Debug.LogError("Invalid position index: " + positionIndex);
            return;
        }

        if (currentMovement != null)
        {
            StopCoroutine(currentMovement);
        }

        currentMovement = StartCoroutine(MoveCameraToPositionWithFOVCoroutine(cameraPositions[positionIndex], duration, targetFOV));
    }

    private IEnumerator MoveCameraToPositionCoroutine(Transform targetPosition)
    {
        IsMoving = true;
        Vector3 startPosition = mainCamera.transform.position;
        Quaternion startRotation = mainCamera.transform.rotation;
        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveDuration;
            
            // Use smoothstep interpolation for more natural movement
            float smoothT = t * t * (3f - 2f * t);
            
            mainCamera.transform.position = Vector3.Lerp(startPosition, targetPosition.position, smoothT);
            mainCamera.transform.rotation = Quaternion.Lerp(startRotation, targetPosition.rotation, smoothT);
            
            yield return null;
        }

        mainCamera.transform.position = targetPosition.position;
        mainCamera.transform.rotation = targetPosition.rotation;
        IsMoving = false;
    }

    private IEnumerator MoveCameraToPositionWithFOVCoroutine(Transform targetPosition, float duration, float targetFOV)
    {
        IsMoving = true;
        Vector3 startPosition = mainCamera.transform.position;
        Quaternion startRotation = mainCamera.transform.rotation;
        float startFOV = mainCamera.fieldOfView;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // Use smoothstep interpolation for more natural movement
            float smoothT = t * t * (3f - 2f * t);
            
            mainCamera.transform.position = Vector3.Lerp(startPosition, targetPosition.position, smoothT);
            mainCamera.transform.rotation = Quaternion.Lerp(startRotation, targetPosition.rotation, smoothT);
            mainCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, smoothT);
            
            yield return null;
        }

        mainCamera.transform.position = targetPosition.position;
        mainCamera.transform.rotation = targetPosition.rotation;
        mainCamera.fieldOfView = targetFOV;
        IsMoving = false;
    }
}

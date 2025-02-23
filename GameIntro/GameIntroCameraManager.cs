using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameIntroCameraManager : MonoBehaviour
{
    [SerializeField]
    private Camera camera;

    [SerializeField] 
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    public List<Transform> cameraPositions;
    public float moveDuration = 5f; // Duration in seconds to reach target
    private Coroutine currentMovement;

    public bool IsMoving { get; private set; }

    private void Awake()
    {
        if (camera == null)
        {
            camera = Camera.main;
        }
    }

    private void Start()
    {
        if (camera == null)
        {
            Debug.LogError("Camera is not set in GameIntroCameraManager and no Main Camera was found");
        }

        if (cameraPositions.Count == 0)
        {
            Debug.LogError("No camera positions set in GameIntroCameraManager");
        }
    }

    public void MoveCameraToPosition(int positionIndex)
    {
        if (positionIndex < 0 || positionIndex >= cameraPositions.Count)
        {
            Debug.LogError("Invalid position index: " + positionIndex);
            return;
        }

        // Stop any existing camera movement
        if (currentMovement != null)
        {
            StopCoroutine(currentMovement);
        }

        // If this is the first position (0), snap to it instantly
        if (positionIndex == 0)
        {
            camera.transform.position = cameraPositions[0].position;
            camera.transform.rotation = cameraPositions[0].rotation;
            
            // If we have more positions, start moving to the next one
            if (cameraPositions.Count > 1)
            {
                currentMovement = StartCoroutine(MoveCameraToPositionCoroutine(cameraPositions[1]));
            }
        }
        else
        {
            currentMovement = StartCoroutine(MoveCameraToPositionCoroutine(cameraPositions[positionIndex]));
        }
    }

    private IEnumerator MoveCameraToPositionCoroutine(Transform targetPosition)
    {
        IsMoving = true;
        Vector3 startPosition = camera.transform.position;
        Quaternion startRotation = camera.transform.rotation;
        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveDuration;
            
            // Use smoothstep interpolation for more natural movement
            float smoothT = t * t * (3f - 2f * t);
            
            camera.transform.position = Vector3.Lerp(startPosition, targetPosition.position, smoothT);
            camera.transform.rotation = Quaternion.Lerp(startRotation, targetPosition.rotation, smoothT);
            
            yield return null;
        }

        camera.transform.position = targetPosition.position;
        camera.transform.rotation = targetPosition.rotation;
        IsMoving = false;
    }
}

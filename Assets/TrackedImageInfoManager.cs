using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Networking;
using System.Diagnostics;
using UnityEngine.Networking.Match;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.Networking.Types;
/// This component listens for images detected by the <c>XRImageTrackingSubsystem</c>
/// and overlays some information as well as the source Texture2D on top of the
/// detected image.
/// </summary>
[RequireComponent(typeof(ARTrackedImageManager))]
public class TrackedImageInfoManager : NetworkBehaviour
{
    [SerializeField]
    [Tooltip("The camera to set on the world space UI canvas for each instantiated image info.")]
    Camera m_WorldSpaceCanvasCamera;

    /// <summary>
    /// The prefab has a world space UI canvas,
    /// which requires a camera to function properly.
    /// </summary>
    public Camera worldSpaceCanvasCamera
    {
        get { return m_WorldSpaceCanvasCamera; }
        set { m_WorldSpaceCanvasCamera = value; }
    }

    ARTrackedImageManager m_TrackedImageManager;

    private Touch touch;
    private Vector2 touchPosition;

    [SyncVar]
    private Quaternion rotationY;

    [SyncVar]
    private float deltaMagnitudeDiff = 0f;

    private Quaternion savedRotation;
    private Vector3 savedScale;

    void Awake()
    {
        m_TrackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    void OnEnable()
    {
        m_TrackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        m_TrackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    void UpdateInfo(ARTrackedImage trackedImage)
    {

        // Disable the visual plane if it is not being tracked
        if (trackedImage.trackingState != TrackingState.None)
        {

            // The image extents is only valid when the image is being tracked
            if (savedRotation == null) savedRotation = trackedImage.transform.rotation;
            if (savedScale == null) savedScale = trackedImage.transform.localScale;

            // Update size of Object
            if (deltaMagnitudeDiff != 0f)
            {
                var newX = Mathf.Clamp(savedScale.x + deltaMagnitudeDiff, 0.001f, 10);
                var newY = Mathf.Clamp(savedScale.y + deltaMagnitudeDiff, 0.001f, 10);
                var newZ = Mathf.Clamp(savedScale.z + deltaMagnitudeDiff, 0.001f, 10);
                trackedImage.transform.localScale = new Vector3(newX, newY, newZ);
                savedScale = trackedImage.transform.localScale;
                deltaMagnitudeDiff = 0f;
            }
            if (isServer)
            {
                UnityEngine.Debug.Log(rotationY);
            }
            else
            {
                UnityEngine.Debug.Log(rotationY);
            }
            // Update rotation of Object

            trackedImage.transform.rotation = savedRotation * rotationY;
            savedRotation = trackedImage.transform.rotation;
            rotationY = Quaternion.Euler(0f, 0f, 0f);

        }
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {

        if (isLocalPlayer && isClient)
        {

            foreach (var trackedImage in eventArgs.added)
            {
                // Give the initial image a reasonable default scale
                trackedImage.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
            }



            foreach (var trackedImage in eventArgs.updated)
                UpdateInfo(trackedImage);
        }

    }



    private float rotateSpeedModifier = 0.2f;
    private Vector2 startPos;

    // Update is called once per frame
    void Update()
    {
        if(isLocalPlayer)
        {
            if (isClient)
            {

                if (!isServer)
                {
                    UnityEngine.Debug.Log(rotationY);
                }

                if (Input.touchCount == 1)
                {
                    Touch touch = Input.GetTouch(0);
                    switch (touch.phase)
                    {
                        //When a touch has first been detected, change the message and record the starting position
                        case TouchPhase.Began:
                            startPos = touch.position;
                            break;

                        case TouchPhase.Moved:
                            // If we have moved we want to rotate
                            rotationY = Quaternion.Euler(0f, -touch.deltaPosition.x * rotateSpeedModifier, 0f);
                            UnityEngine.Debug.Log(rotationY);
                            break;

                        case TouchPhase.Ended:
                            // If when we ended the finger hadn't moved, it's a tap
                            if (touch.position == startPos)
                            {
                                Ray raycast = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
                                RaycastHit raycastHit;
                                if (Physics.Raycast(raycast, out raycastHit))
                                {
                                    if (raycastHit.collider != null)
                                    {
                                        UnityEngine.Debug.Log("Tapped " + raycastHit.transform.gameObject.name);
                                    }
                                }
                            }
                            break;
                    }
                }
                else if (Input.touchCount == 2)
                {
                    // Store both of the touches on screen.
                    Touch touchZero = Input.GetTouch(0);
                    Touch touchOne = Input.GetTouch(1);

                    // Find the position in the previous frame of each touch.
                    Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                    Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

                    // Find the magnitude of the vector (the distance) between the touches in each frame.
                    float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                    float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

                    // Find the difference in the distances between each frame.
                    deltaMagnitudeDiff = (prevTouchDeltaMag - touchDeltaMag) * -0.0001f;
                }
                if (!isServer) CmdUpdateRotation(rotationY);
            }
            
        }
    }

    [Command]
    void CmdUpdateRotation(Quaternion rotation)
    {
        rotationY = rotation;
    }

    [Command]
    void CmdUpdateScale(float deltaMagnitude)
    {
        deltaMagnitudeDiff = deltaMagnitude;
    }
}

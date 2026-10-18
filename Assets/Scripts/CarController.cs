using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    [Header("Wheel Visual Pivots")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("Car Elements (5 Total)")]
    // Element 1: Steering
    public float maxSteerAngle = 30f;
    // Element 2: Throttle (Drive Wheels)
    public float maxMotorTorque = 1500f;
    // Element 3: Brakes
    public float maxBrakeTorque = 3000f;
    public GameObject[] brakeLights;
    // Element 4: Headlights
    public GameObject[] headlights;
    // Element 5: Driver Door
    public Transform driverDoor;
    public Vector3 doorClosedEuler = Vector3.zero;
    public Vector3 doorOpenEuler = new Vector3(0f, 65f, 0f);

    [Header("Camera Rig")]
    public Camera targetCamera;
    public Transform camChaseTarget;
    public Transform camSteeringTarget;
    public Transform camThrottleTarget;
    public Transform camBrakesTarget;
    public Transform camHeadlightsTarget;
    public Transform camDoorTarget;
    public float cameraTransitionSpeed = 8f;

    [Header("Current Inputs")]
    public float steerInput;
    public float throttleInput;
    public float brakeInput;
    public bool headlightsOn;
    public bool doorIsOpen;
    public int cameraFocusIndex;

    private Rigidbody rb;
    private Vector3 currentCamLocalPos;
    private Quaternion currentCamLocalRot;
    private bool camInitialized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, -0.4f, 0f);
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Start()
    {
        Transform initialTarget = GetTargetAnchor(cameraFocusIndex);
        if (initialTarget != null)
        {
            currentCamLocalPos = transform.InverseTransformPoint(initialTarget.position);
            currentCamLocalRot = Quaternion.Inverse(transform.rotation) * initialTarget.rotation;
            camInitialized = true;
        }
    }

    public void ApplyInputs(CarControlData data)
    {
        steerInput = Mathf.Clamp(data.steer, -1f, 1f);
        throttleInput = Mathf.Clamp01(data.throttle);
        brakeInput = Mathf.Clamp01(data.brake);
        headlightsOn = data.headlights;
        doorIsOpen = data.doorOpen;
        cameraFocusIndex = data.cameraFocus;
    }

    private void Update()
    {
        // Keyboard fallback when testing directly in the Editor
        if (Application.isEditor && Keyboard.current != null && steerInput == 0f && throttleInput == 0f && brakeInput == 0f)
        {
            float h = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;

            float v = 0f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1f;

            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
            {
                steerInput = h;
                throttleInput = Mathf.Clamp01(v);
                brakeInput = Mathf.Clamp01(-v);
            }
        }

        // Element 4: Update Headlights
        if (headlights != null)
        {
            foreach (var hl in headlights)
            {
                if (hl != null && hl.activeSelf != headlightsOn)
                    hl.SetActive(headlightsOn);
            }
        }

        // Element 5: Update Driver Door Hinge
        if (driverDoor != null)
        {
            Quaternion targetRot = Quaternion.Euler(doorIsOpen ? doorOpenEuler : doorClosedEuler);
            driverDoor.localRotation = Quaternion.Slerp(driverDoor.localRotation, targetRot, Time.deltaTime * 6f);
        }

        // Element 3: Update Brake Lights
        bool isBraking = brakeInput > 0.1f;
        if (brakeLights != null)
        {
            foreach (var bl in brakeLights)
            {
                if (bl != null && bl.activeSelf != isBraking)
                    bl.SetActive(isBraking);
            }
        }
    }

    private void FixedUpdate()
    {
        // Element 1: Steering
        float steer = steerInput * maxSteerAngle;
        if (frontLeftWheel != null) frontLeftWheel.steerAngle = steer;
        if (frontRightWheel != null) frontRightWheel.steerAngle = steer;

        // Element 2: Throttle
        float motor = throttleInput * maxMotorTorque;
        if (rearLeftWheel != null) rearLeftWheel.motorTorque = motor;
        if (rearRightWheel != null) rearRightWheel.motorTorque = motor;

        // Element 3: Brakes
        float brake = brakeInput * maxBrakeTorque;
        if (frontLeftWheel != null) frontLeftWheel.brakeTorque = brake;
        if (frontRightWheel != null) frontRightWheel.brakeTorque = brake;
        if (rearLeftWheel != null) rearLeftWheel.brakeTorque = brake;
        if (rearRightWheel != null) rearRightWheel.brakeTorque = brake;
    }

    private void LateUpdate()
    {
        // Sync wheel visual pivots after physics interpolation
        UpdateWheelMesh(frontLeftWheel, frontLeftMesh);
        UpdateWheelMesh(frontRightWheel, frontRightMesh);
        UpdateWheelMesh(rearLeftWheel, rearLeftMesh);
        UpdateWheelMesh(rearRightWheel, rearRightMesh);

        // Smooth camera transition in car-local space to prevent motion jitter when zoomed in
        if (targetCamera == null) return;

        Transform activeAnchor = GetTargetAnchor(cameraFocusIndex);
        if (activeAnchor == null) return;

        Vector3 targetLocalPos = transform.InverseTransformPoint(activeAnchor.position);
        Quaternion targetLocalRot = Quaternion.Inverse(transform.rotation) * activeAnchor.rotation;

        if (!camInitialized)
        {
            currentCamLocalPos = targetLocalPos;
            currentCamLocalRot = targetLocalRot;
            camInitialized = true;
        }
        else
        {
            float t = 1f - Mathf.Exp(-cameraTransitionSpeed * Time.deltaTime);
            currentCamLocalPos = Vector3.Lerp(currentCamLocalPos, targetLocalPos, t);
            currentCamLocalRot = Quaternion.Slerp(currentCamLocalRot, targetLocalRot, t);
        }

        targetCamera.transform.SetPositionAndRotation(
            transform.TransformPoint(currentCamLocalPos),
            transform.rotation * currentCamLocalRot
        );
    }

    private Transform GetTargetAnchor(int index)
    {
        return index switch
        {
            1 => camSteeringTarget,
            2 => camThrottleTarget,
            3 => camBrakesTarget,
            4 => camHeadlightsTarget,
            5 => camDoorTarget,
            _ => camChaseTarget
        };
    }

    private void UpdateWheelMesh(WheelCollider col, Transform wheelPivot)
    {
        if (col == null || wheelPivot == null) return;
        col.GetWorldPose(out Vector3 pos, out Quaternion rot);
        wheelPivot.SetPositionAndRotation(pos, rot);
    }

    public void ResetPosition()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        transform.SetPositionAndRotation(new Vector3(0f, 0.5f, 0f), Quaternion.identity);
    }
}

using System;

[Serializable]
public class CarControlData
{
    // Element 1: Steering (-1.0 to 1.0)
    public float steer;

    // Element 2: Throttle (0.0 to 1.0)
    public float throttle;

    // Element 3: Brake (0.0 to 1.0)
    public float brake;

    // Element 4: Headlights (true = ON, false = OFF)
    public bool headlights;

    // Element 5: Door (true = OPEN, false = CLOSED)
    public bool doorOpen;

    // Camera Focus Part:
    // 0 = Default Chase Cam
    // 1 = Steering (Front Wheels)
    // 2 = Throttle (Drive Wheels)
    // 3 = Brakes (Tail Lights & Brakes)
    // 4 = Headlights
    // 5 = Driver Door
    public int cameraFocus;
}

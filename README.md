# Unity Client-Server Car Control (Raw TCP Sockets)

## Overview
This project is an implementation of the **Unity Client-Server Car Control** assignment using Unity and raw TCP sockets (`System.Net.Sockets`).

- **Desktop Server (`Assets/Scenes/ServerScene.unity`)**:
  - Runs on Windows.
  - Hosts the car simulation using `WheelCollider`s and `Rigidbody`.
  - Listens for client commands over a raw TCP socket (`TcpListener` on port `7777`).
  - Animates the 5 car elements and switches camera focus based on client commands.
- **Mobile Client (`Assets/Scenes/ClientScene.unity`)**:
  - Runs on Android (in landscape orientation).
  - Connects to the desktop server over raw TCP (`TcpClient`).
  - Sends real-time driving controls and element commands.

---

## 5 Controlled Car Elements & Camera Focus
1. **Steering (Element 1)**: Turns the front wheels (`WheelCollider.steerAngle`).
2. **Throttle (Element 2)**: Accelerates the rear drive wheels (`WheelCollider.motorTorque`).
3. **Brakes (Element 3)**: Applies brake torque (`WheelCollider.brakeTorque`) and activates red brake lights.
4. **Headlights (Element 4)**: Toggles front headlights on/off.
5. **Driver Door (Element 5)**: Smoothly opens/closes the driver door hinge.

**Camera Feed Focus**:
The camera feed on the server smoothly switches between:
- **Chase Cam** (Default overview)
- **Steering** (Front wheels)
- **Wheels** (Drive wheels)
- **Brakes** (Brake lights & rear)
- **Headlights** (Front lights)
- **Driver Door** (Side door)

---

## Network Communication
Communication uses raw TCP sockets with lightweight JSON formatting (`JsonUtility`):
```csharp
[Serializable]
public class CarControlData
{
    public float steer;       // -1.0 to 1.0
    public float throttle;    // 0.0 to 1.0
    public float brake;       // 0.0 to 1.0
    public bool headlights;   // Element 4 toggle
    public bool doorOpen;     // Element 5 toggle
    public int cameraFocus;   // 0: Chase, 1: Steering, 2: Wheels, 3: Brakes, 4: Headlights, 5: Door
}
```

---

## How to Run

### 1. In Unity Editor
- Open **`Assets/Scenes/ServerScene.unity`** and press **Play**.
- The server displays the local LAN IP address and listens on port `7777`.
*(Note: You can also drive the car locally with WASD / Arrow keys for quick testing in Editor)*.

### 2. On Android Mobile
- Build **`Assets/Scenes/ClientScene.unity`** for Android (`File -> Build Settings -> Switch to Android -> Build`).
- Install and open the APK on your Android phone connected to the same Wi-Fi network.
- Enter the desktop IP address shown on the server screen and tap **Connect**.
- Use the touch buttons to steer, accelerate, brake, toggle headlights/doors, and switch camera views.

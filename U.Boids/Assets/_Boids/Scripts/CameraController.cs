using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float cameraSpeed = 50f;
    private float deltaTime = 0;
    private float head = 0f;
    private float pitch = -90f;
    private float radius = 50f;

    void Update()
    {
        deltaTime = Time.deltaTime;
        GetInput();
        UpdateTransform();
    }

    private void GetInput()
    {
        if (Input.GetKey(KeyCode.W))
            pitch += deltaTime * cameraSpeed;
        if (Input.GetKey(KeyCode.S))
            pitch -= deltaTime * cameraSpeed;
        if (Input.GetKey(KeyCode.D))
            head -= deltaTime * cameraSpeed;
        if (Input.GetKey(KeyCode.A))
            head += deltaTime * cameraSpeed;

        head = head % 360;
        pitch = pitch % 360;

        if (Input.GetKey(KeyCode.Escape))
            Application.Quit();
    }

    private void UpdateTransform()
    {
        transform.position = new Vector3(
            radius * Mathf.Sin(Mathf.Deg2Rad * pitch) * Mathf.Sin(Mathf.Deg2Rad * head),
            radius * Mathf.Cos(Mathf.Deg2Rad * pitch),
            radius * Mathf.Sin(Mathf.Deg2Rad * pitch) * Mathf.Cos(Mathf.Deg2Rad * head)
            );

        transform.LookAt(Vector3.zero);
    }
}

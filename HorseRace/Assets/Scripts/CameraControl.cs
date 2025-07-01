using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraControl : MonoBehaviour
{
	private new Camera camera;

	private float orthographicSize;
	private float orthoVelocity;

	private Vector3 position;
	private Vector3 velocity;

	private void Awake()
	{
		this.camera = this.GetComponent<Camera>();
		this.orthographicSize = this.camera.orthographicSize;

		this.position = this.transform.position;
	}

	private void Update()
	{
		this.orthographicSize = Mathf.Clamp(this.orthographicSize - Input.GetAxis("Mouse ScrollWheel"), 1, 20);
		this.camera.orthographicSize = Mathf.SmoothDamp(this.camera.orthographicSize, this.orthographicSize, ref this.orthoVelocity, 0.1f);

		if (Input.GetMouseButton(2))
		{
			this.position -= new Vector3(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 0.3f;
		}

		this.transform.position = Vector3.SmoothDamp(this.transform.position, this.position, ref this.velocity, 0.1f);
	}

	public void UI_Reset()
	{
		this.orthographicSize = 5.4f;
		this.position = Vector3.back * 10;
	}
}
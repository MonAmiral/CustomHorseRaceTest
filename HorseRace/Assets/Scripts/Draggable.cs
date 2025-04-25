using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Draggable : MonoBehaviour
{
	private bool isDragged;
	private Vector3 velocity;

	public void LoadPosition()
	{
		this.transform.position = new Vector3(PlayerPrefs.GetFloat(this.name + "_x", this.transform.position.x), PlayerPrefs.GetFloat(this.name + "_y", this.transform.position.y), this.transform.position.z);
	}

	private void OnDestroy()
	{
		PlayerPrefs.SetFloat(this.name + "_x", this.transform.position.x);
		PlayerPrefs.SetFloat(this.name + "_y", this.transform.position.y);
		PlayerPrefs.Save();
	}

	public void OnBeginDrag()
	{
		this.isDragged = true;
		this.velocity = Vector3.zero;
	}

	protected virtual void Update()
	{
		if (this.isDragged)
		{
			Vector3 goalPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			goalPosition.z = this.transform.position.z;
			this.transform.position = Vector3.SmoothDamp(this.transform.position, goalPosition, ref this.velocity, 0.1f);
		}
	}

	public void OnEndDrag()
	{
		this.isDragged = false;
	}
}
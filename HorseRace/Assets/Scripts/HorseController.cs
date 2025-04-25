using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class HorseController : Draggable
{
	public SpriteRenderer SpriteRenderer;
	public TextMeshPro NameLabel;
	public Rigidbody2D Rigidbody;

	[System.NonSerialized]
	public Sprite WinnerSprite;

	public AudioSource AudioSource;
	[System.NonSerialized]
	public AudioClip BounceClip, WinnerClip;

	private Vector2 movement;

	private RaycastHit2D[] hitsCache = new RaycastHit2D[1];
	private float lastBounceTime;

	public void Start()
	{
	}

	public void StartRace()
	{
		this.movement = Random.insideUnitCircle.normalized * 2;
		this.SpriteRenderer.transform.localEulerAngles = this.movement.x > 0 ? Vector3.zero : Vector3.up * 180;
	}

	public void StopRace()
	{
		this.movement = Vector2.zero;
	}

	protected override void Update()
	{
		base.Update();

		this.Update_Move();
	}

	private void Update_Move()
	{
		if (this.movement == Vector2.zero)
		{
			return;
		}

		if (Time.timeSinceLevelLoad > this.lastBounceTime + 0.1f)
		{
			int hits = this.Rigidbody.Cast(this.movement, this.hitsCache, this.movement.magnitude * Time.deltaTime);
			if (hits == 1)
			{
				Debug.DrawRay(this.hitsCache[0].point, this.hitsCache[0].normal, Color.red, 1);

				// Check for victory.
				if (this.hitsCache[0].transform.tag == "Goal")
				{
					GameManager manager = this.GetComponentInParent<GameManager>();
					manager.StopAllCoroutines();
					manager.StartCoroutine(manager.Win(this));
					return;
				}

				this.movement = Vector2.Reflect(this.movement, this.hitsCache[0].normal);
				this.SpriteRenderer.transform.localEulerAngles = this.movement.x > 0 ? Vector3.zero : Vector3.up * 180;

				this.lastBounceTime = Time.timeSinceLevelLoad;

				if (!this.AudioSource.isPlaying)
				{
					this.AudioSource.pitch = Random.Range(0.8f, 1.2f);
					this.AudioSource.PlayOneShot(this.BounceClip);
				}

				// Add a bit of random 30% of the time.
				if (Random.Range(0, 100) < 30)
				{
					this.movement = (this.movement + Random.insideUnitCircle).normalized * this.movement.magnitude;
				}
			}
		}

		this.transform.position += (Vector3)this.movement * Time.deltaTime;
	}
}
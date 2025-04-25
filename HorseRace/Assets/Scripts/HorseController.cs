using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class HorseController : Draggable
{
	private static float KissAvailabilityTime;

	public SpriteRenderer SpriteRenderer;
	public TextMeshPro NameLabel;
	public Rigidbody2D Rigidbody;

	[System.NonSerialized]
	public Sprite WinnerSprite;

	public AudioSource AudioSource;
	[System.NonSerialized]
	public AudioClip BounceClip, WinnerClip;
	public AudioClip KissClip;

	public ParticleSystem KissParticles;

	private Vector2 movement;

	private RaycastHit2D[] hitsCache = new RaycastHit2D[3];
	private float lastBounceTime;

	public void Start()
	{
		HorseController.KissAvailabilityTime = 0;
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
			bool isInKissingZone = false;
			int hits = this.Rigidbody.Cast(this.movement, this.hitsCache, this.movement.magnitude * Time.deltaTime);
			for (int i = 0; i < hits; i++)
			{
				RaycastHit2D hit = this.hitsCache[i];
				Debug.DrawRay(hit.point, hit.normal, Color.red, 1);
				if (hit.collider.isTrigger)
				{
					// Check for victory.
					switch (hit.transform.tag)
					{
						case "Goal":
							GameManager manager = this.GetComponentInParent<GameManager>();
							manager.StopAllCoroutines();
							manager.StartCoroutine(manager.Win(this));
							return;

						case "Kiss":
							isInKissingZone = true;
							break;
					}
				}
				else
				{
					this.movement = Vector2.Reflect(this.movement, hit.normal);
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

					if (isInKissingZone && hit.collider.transform.tag == "Horse")
					{
						// Kiss!!
						if (Time.timeSinceLevelLoad > HorseController.KissAvailabilityTime)
						{
							HorseController.KissAvailabilityTime = Time.timeSinceLevelLoad + 0.5f;
							this.KissParticles.Emit(5);

							if (Random.Range(0, 100) < 20)
							{
								this.AudioSource.PlayOneShot(this.KissClip);
							}
						}
					}
				}
			}
		}

		this.transform.position += (Vector3)this.movement * Time.deltaTime;
	}
}
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour
{
	public HorseController HorsePrefab;
	public Transform HorseSpawnPoint;

	public Animator UIAnimator;
	public TMPro.TextMeshProUGUI TimeLabel;
	public UnityEngine.UI.Image VictoryImage;

	public SpriteRenderer Level, Background, Start, Goal;
	public Draggable[] Draggables;

	private List<HorseController> horses = new List<HorseController>();

	public AudioSource AudioSource;
	public AudioClip DefaultCountdownClip;
	public AudioClip DefaultBounceClip;
	public AudioClip DefaultWinnerClip;
	public AudioClip DefaultMusic;

	private bool hasReloadedFlagPositions;

	public IEnumerator ReloadHorses()
	{
		string folderPath = Application.persistentDataPath.Replace("/", "\\");
		if (!System.IO.Directory.Exists(folderPath))
		{
			yield break;
		}

		// Delete existing horses.
		foreach (HorseController horse in this.horses)
		{
			GameObject.Destroy(horse.gameObject);
		}

		this.horses.Clear();

		// Load bounce audio.
		AudioClip bounceClip = this.DefaultBounceClip;
		string defaultAudioPath = Application.persistentDataPath + "\\Bounce.mp3";
		if (System.IO.File.Exists(defaultAudioPath))
		{
			using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip("file://" + defaultAudioPath, AudioType.MPEG))
			{
				yield return request.SendWebRequest();
				AudioClip overrideClip = DownloadHandlerAudioClip.GetContent(request);
				if (overrideClip)
				{
					bounceClip = overrideClip;
				}
			}
		}

		// Create horses.
		foreach (string filePath in System.IO.Directory.EnumerateFiles(folderPath, "*_Body.png"))
		{
			string winnerFilePath = filePath.Replace("_Body.png", "_Winner.png");

			// Must have a body texture and a winner texture.
			Texture2D bodyTexture = this.LoadTexture(filePath);
			if (bodyTexture == null)
			{
				continue;
			}

			Texture2D winnerTexture = this.LoadTexture(winnerFilePath);
			if (winnerTexture == null)
			{
				continue;
			}

			HorseController horse = GameObject.Instantiate(this.HorsePrefab, this.transform);
			horse.SpriteRenderer.sprite = Sprite.Create(bodyTexture, new Rect(0, 0, bodyTexture.width, bodyTexture.height), Vector2.one * 0.5f);
			horse.WinnerSprite = Sprite.Create(winnerTexture, new Rect(0, 0, winnerTexture.width, winnerTexture.height), Vector2.one * 0.5f);
			horse.SpriteRenderer.gameObject.AddComponent<PolygonCollider2D>();
			horse.BounceClip = bounceClip;
			horse.NameLabel.text = System.IO.Path.GetFileNameWithoutExtension(filePath).Replace("_Body", "");

			// Position is vaguely random. Physics will do the spacing.
			horse.transform.position = this.HorseSpawnPoint.position + (Vector3)Random.insideUnitCircle;

			// Load custom audio if relevant.
			string audioOverridePath = filePath.Replace("_Body.png", "_Bounce.mp3");
			if (System.IO.File.Exists(audioOverridePath))
			{
				using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip("file://" + audioOverridePath, AudioType.MPEG))
				{
					yield return request.SendWebRequest();
					AudioClip overrideClip = DownloadHandlerAudioClip.GetContent(request);
					if (overrideClip)
					{
						horse.BounceClip = overrideClip;
					}
				}
			}

			audioOverridePath = filePath.Replace("_Body.png", "_Winner.mp3");
			if (System.IO.File.Exists(audioOverridePath))
			{
				using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip("file://" + audioOverridePath, AudioType.MPEG))
				{
					yield return request.SendWebRequest();
					AudioClip overrideClip = DownloadHandlerAudioClip.GetContent(request);
					if (overrideClip)
					{
						horse.WinnerClip = overrideClip;
					}
				}
			}

			this.horses.Add(horse);

			yield return null;
		}
	}

	public IEnumerator ReloadLevel()
	{
		string filePath = Application.persistentDataPath + "\\Level.png";
		if (System.IO.File.Exists(filePath))
		{
			Texture2D levelTexture = this.LoadTexture(filePath);
			if (levelTexture != null)
			{
				this.Level.sprite = Sprite.Create(levelTexture, new Rect(0, 0, levelTexture.width, levelTexture.height), Vector2.one * 0.5f);
				GameObject.Destroy(this.Level.gameObject.GetComponent<PolygonCollider2D>());
				this.Level.gameObject.AddComponent<PolygonCollider2D>();

				yield return null;
			}
		}

		filePath = Application.persistentDataPath + "\\Background.png";
		if (System.IO.File.Exists(filePath))
		{
			Texture2D backgroundTexture = this.LoadTexture(filePath);
			if (backgroundTexture != null)
			{
				this.Background.sprite = Sprite.Create(backgroundTexture, new Rect(0, 0, backgroundTexture.width, backgroundTexture.height), Vector2.one * 0.5f);
			}
		}
	}

	public IEnumerator ReloadFlags()
	{
		string filePath = Application.persistentDataPath + "\\Start.png";
		if (System.IO.File.Exists(filePath))
		{
			Texture2D startTexture = this.LoadTexture(filePath);
			if (startTexture != null)
			{
				this.Start.sprite = Sprite.Create(startTexture, new Rect(0, 0, startTexture.width, startTexture.height), Vector2.one * 0.5f);
				yield return null;
			}
		}

		filePath = Application.persistentDataPath + "\\Goal.png";
		if (System.IO.File.Exists(filePath))
		{
			Texture2D goalTexture = this.LoadTexture(filePath);
			if (goalTexture != null)
			{
				this.Goal.sprite = Sprite.Create(goalTexture, new Rect(0, 0, goalTexture.width, goalTexture.height), Vector2.one * 0.5f);
			}
		}

		if (!this.hasReloadedFlagPositions)
		{
			this.hasReloadedFlagPositions = true;
			foreach (Draggable draggable in this.Draggables)
			{
				draggable.LoadPosition();
			}
		}
	}

	public IEnumerator ReloadMusic()
	{
		// Load music audio.
		AudioClip musicClip = this.DefaultMusic;
		string audioOverridePath = Application.persistentDataPath + "\\Music.mp3";
		if (System.IO.File.Exists(audioOverridePath))
		{
			using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip("file://" + audioOverridePath, AudioType.MPEG))
			{
				yield return request.SendWebRequest();
				AudioClip overrideClip = DownloadHandlerAudioClip.GetContent(request);
				if (overrideClip)
				{
					musicClip = overrideClip;
				}
			}
		}

		this.AudioSource.clip = musicClip;
	}

	public IEnumerator StartRace()
	{
		// Play countdown.
		yield return this.PlayClip("Countdown.mp3", this.DefaultCountdownClip);
		this.UIAnimator.Play("GameStart");

		// Wait.
		yield return new WaitForSeconds(3);

		// Release the beasts.
		for (int i = 0; i < this.horses.Count; i++)
		{
			this.horses[i].StartRace();
		}

		yield return new WaitForSeconds(1);

		this.AudioSource.Play();

		// StopAllCoroutines will be called before Win().
		float time = 0f;
		while (true)
		{
			time += Time.deltaTime;
			this.TimeLabel.text = ((int)time).ToString();

			if (Input.GetKeyDown(KeyCode.RightArrow))
			{
				Time.timeScale++;
			}

			if (Input.GetKeyDown(KeyCode.LeftArrow))
			{
				Time.timeScale = Mathf.Max(1, Time.timeScale - 1);
			}

			yield return null;
		}
	}

	public IEnumerator Win(HorseController winner)
	{
		// Stop all horses.
		foreach (HorseController horse in this.horses)
		{
			horse.StopRace();
		}

		Time.timeScale = 1;

		this.UIAnimator.Play("GameOver");
		this.VictoryImage.sprite = winner.WinnerSprite;
		this.AudioSource.Stop();

		if (winner.WinnerClip != null)
		{
			this.AudioSource.PlayOneShot(winner.WinnerClip);
		}
		else
		{
			yield return this.PlayClip("Winner.mp3", this.DefaultWinnerClip);
		}

		yield break;
	}

	public void UI_NavigateToHorseFolder()
	{
		string folderPath = Application.persistentDataPath.Replace("/", "\\");

		if (!System.IO.Directory.Exists(folderPath))
		{
			System.IO.Directory.CreateDirectory(folderPath);
		}

		System.Diagnostics.Process process = new System.Diagnostics.Process();
		process.StartInfo = new System.Diagnostics.ProcessStartInfo("explorer.exe");
		process.StartInfo.Arguments = folderPath;
		Debug.Log(process.StartInfo.Arguments);
		process.Start();
	}

	public void UI_ReloadHorses()
	{
		this.StartCoroutine(this.ReloadHorses());
	}

	public void UI_ReloadLevel()
	{
		this.StartCoroutine(this.ReloadLevel());
	}

	public void UI_ReloadFlags()
	{
		this.StartCoroutine(this.ReloadFlags());
	}

	public void UI_ReloadMusic()
	{
		this.StartCoroutine(this.ReloadMusic());
	}

	public void UI_StartRace()
	{
		if (this.horses.Count == 0)
		{
			return;
		}

		this.StartCoroutine(this.StartRace());
	}

	public void UI_Restart()
	{
		this.UIAnimator.Play("Restart");

		foreach (HorseController horse in this.horses)
		{
			horse.transform.position = this.HorseSpawnPoint.position + (Vector3)Random.insideUnitCircle;
		}
	}

	public void UI_StopRace()
	{
		Time.timeScale = 1;
		this.AudioSource.Stop();

		this.UIAnimator.Play("Abort");

		// Stop all horses.
		foreach (HorseController horse in this.horses)
		{
			horse.StopRace();
		}
	}

	public void UI_ToggleFullScreen()
	{
		if (Screen.fullScreenMode != FullScreenMode.Windowed)
		{
			Screen.SetResolution(Screen.width, Screen.height, FullScreenMode.Windowed);
		}
		else
		{
			Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
		}
	}

	public void UI_Quit()
	{
		Application.Quit();
	}

	private IEnumerator PlayClip(string fileName, AudioClip defaultClip)
	{
		// Load audio.
		string path = Application.persistentDataPath + "\\" + fileName;
		if (System.IO.File.Exists(path))
		{
			using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.MPEG))
			{
				yield return request.SendWebRequest();
				AudioClip overrideClip = DownloadHandlerAudioClip.GetContent(request);
				if (overrideClip)
				{
					defaultClip = overrideClip;
				}
			}
		}

		this.AudioSource.PlayOneShot(defaultClip);
	}

	private Texture2D LoadTexture(string path)
	{
		try
		{
			byte[] bytes = System.IO.File.ReadAllBytes(path);

			Texture2D texture = new Texture2D(2, 2);
			texture.LoadImage(bytes);

			if (Mathf.IsPowerOfTwo(texture.width) && Mathf.IsPowerOfTwo(texture.height))
			{
				texture.Compress(true);
			}

			return texture;
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
		}

		return null;
	}
}
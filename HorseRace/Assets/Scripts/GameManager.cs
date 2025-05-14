using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TwitchSDK;
using TwitchSDK.Interop;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
	public HorseController HorsePrefab;
	public Transform HorseSpawnPoint;

	public Animator UIAnimator;
	public TMPro.TextMeshProUGUI TimeLabel;
	public Image VictoryImage;

	public SpriteRenderer Level, Background, Foreground, StartSprite, Goal, KissZone;
	public Draggable[] Draggables;

	public Button ConnectButton, StartPredictionButton, CancelPredictionButton;

	private List<HorseController> horses = new List<HorseController>();

	public AudioSource AudioSource;
	public AudioClip DefaultCountdownClip;
	public AudioClip DefaultBounceClip;
	public AudioClip DefaultWinnerClip;
	public AudioClip DefaultMusic;

	private bool hasReloadedFlagPositions;

	private GameTask<AuthenticationInfo> authenticationInfoTask;
	private GameTask<AuthState> authStateTask;

	private GameTask<Prediction> predictionTask;
	private Prediction prediction;

	public TMPro.TextMeshProUGUI StatusLabel;

	public Image Cursor;
	private float timeSinceCursorMovement;
	public CanvasGroup StopButton;

	private void Start()
	{
		Application.targetFrameRate = 60;
		AudioSource.volume = 0.5f;

		Twitch.API.LogOut();

		this.authStateTask = Twitch.API.GetAuthState();
		this.authenticationInfoTask = Twitch.API.GetAuthenticationInfo(TwitchOAuthScope.Channel.ManagePredictions);

		UnityEngine.Cursor.visible = false;
	}

	private void Update()
	{
		this.timeSinceCursorMovement += Time.deltaTime;

		if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
		{
			this.timeSinceCursorMovement = 0;
		}

		this.StopButton.alpha = Mathf.Clamp01(3 - this.timeSinceCursorMovement);
		this.Cursor.color = new Color(1, 1, 1, this.StopButton.alpha);
	}

	private void LateUpdate()
	{
		this.Cursor.rectTransform.anchorMin = Camera.main.ScreenToViewportPoint(Input.mousePosition);
		this.Cursor.rectTransform.anchorMax = this.Cursor.rectTransform.anchorMin;
	}

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
			Texture2D texture = this.LoadTexture(filePath);
			if (texture != null)
			{
				this.Level.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
				GameObject.Destroy(this.Level.gameObject.GetComponent<PolygonCollider2D>());
				this.Level.gameObject.AddComponent<PolygonCollider2D>();

				yield return null;
			}
		}

		filePath = Application.persistentDataPath + "\\Background.png";
		if (System.IO.File.Exists(filePath))
		{
			Texture2D texture = this.LoadTexture(filePath);
			if (texture != null)
			{
				this.Background.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
			}
		}

		filePath = Application.persistentDataPath + "\\Foreground.png";
		if (System.IO.File.Exists(filePath))
		{
			Texture2D texture = this.LoadTexture(filePath);
			if (texture != null)
			{
				this.Foreground.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
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
				this.StartSprite.sprite = Sprite.Create(startTexture, new Rect(0, 0, startTexture.width, startTexture.height), Vector2.one * 0.5f);
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
		this.KissZone.enabled = false;

		this.StartPredictionButton.interactable = false;

		// Play countdown.
		this.UIAnimator.Play("GameStart");
		yield return new WaitForSeconds(1);
		yield return this.PlayClip("Countdown.mp3", this.DefaultCountdownClip);

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

		this.EndPrediction(winner);

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

	public bool CreatePrediction(string[] names)
	{
		if (this.authStateTask.MaybeResult == null)
		{
			return false;
		}

		switch (this.authStateTask.MaybeResult.Status)
		{
			case AuthStatus.Loading:
				return false;

			case AuthStatus.LoggedOut:
			case AuthStatus.WaitingForCode:
				if (this.authenticationInfoTask.MaybeResult != null)
				{
					Application.OpenURL(this.authenticationInfoTask.MaybeResult.Uri);
					this.StatusLabel.text = "Connecting in browser...";

					this.StartCoroutine(this.WaitForConnectionToFinish());
				}

				return false;

			case AuthStatus.LoggedIn:
				this.predictionTask = Twitch.API.NewPrediction(new PredictionDefinition()
				{
					Title = "Who will win?",
					Duration = 60,
					Outcomes = names
				});

				this.StartCoroutine(this.WaitForPredictionToStart());
				return true;

			default:
				throw new System.Exception();
		}
	}

	private IEnumerator WaitForConnectionToFinish()
	{
		while (true)
		{
			yield return new WaitForSeconds(1);

			this.authStateTask = Twitch.API.GetAuthState();
			if (this.authStateTask.MaybeResult != null && this.authStateTask.MaybeResult.Status == AuthStatus.LoggedIn)
			{
				this.StatusLabel.text = "Connected!";
				yield break;
			}
		}
	}

	private IEnumerator WaitForPredictionToStart()
	{
		while (true)
		{
			yield return new WaitForSeconds(1);

			if (this.predictionTask.MaybeResult == null)
			{
				continue;
			}

			this.prediction = this.predictionTask.MaybeResult;
			this.StatusLabel.text = "Prediction started.";

			this.StartPredictionButton.gameObject.SetActive(false);
			this.CancelPredictionButton.gameObject.SetActive(true);

			yield break;
		}
	}

	public void EndPrediction(HorseController winner)
	{
		if (this.prediction == null)
		{
			return;
		}

		this.StartPredictionButton.gameObject.SetActive(true);
		this.CancelPredictionButton.gameObject.SetActive(false);
		this.StartPredictionButton.interactable = true;

		for (int i = 0; i < this.prediction.Info.Outcomes.Length; i++)
		{
			if (this.prediction.Info.Outcomes[i].Title == winner.NameLabel.text)
			{
				this.prediction.Resolve(this.prediction.Info.Outcomes[i]);

				this.StatusLabel.text = $"Prediction ended. {winner.NameLabel.text} won!";
				this.prediction = null;
				return;
			}
		}

		this.prediction.Cancel();
		this.StatusLabel.text = "Prediction ended. Winner not found.";
		this.prediction = null;
	}

	public void CancelPrediction()
	{
		if (this.prediction == null)
		{
			return;
		}

		this.prediction.Cancel();
		this.StatusLabel.text = "Prediction cancelled.";
		this.prediction = null;

		this.StartPredictionButton.gameObject.SetActive(true);
		this.CancelPredictionButton.gameObject.SetActive(false);
		this.StartPredictionButton.interactable = true;
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

		this.KissZone.enabled = true;
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

		this.KissZone.enabled = true;
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
		this.CancelPrediction();
		Application.Quit();
	}

	public void UI_CreatePrediction()
	{
		if (this.horses.Count == 0)
		{
			this.StatusLabel.text = "Reload horses first!";
			return;
		}

		string[] names = new string[this.horses.Count];
		for (int i = 0; i < this.horses.Count; i++)
		{
			names[i] = this.horses[i].NameLabel.text;
		}

		this.CreatePrediction(names);
	}

	public void UI_CancelPrediction()
	{
		this.CancelPrediction();
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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	public HorseController HorsePrefab;
	public Transform HorseSpawnPoint;

	public Animator UIAnimator;
	public TMPro.TextMeshProUGUI TimeLabel;
	public UnityEngine.UI.Image VictoryImage;

	public SpriteRenderer Level, Background;

	private List<HorseController> horses = new List<HorseController>();

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

		// Fetch horse textures.
		foreach (string filePath in System.IO.Directory.EnumerateFiles(folderPath, "*_Body.png"))
		{
			string winnerFilePath = filePath.Replace("_Body.png", "_Winner.png");

			Texture2D bodyTexture = this.LoadTexture(filePath, 128);
			if (bodyTexture == null)
			{
				continue;
			}

			Texture2D winnerTexture = this.LoadTexture(winnerFilePath, 10000);
			if (winnerTexture == null)
			{
				continue;
			}

			HorseController horse = GameObject.Instantiate(this.HorsePrefab, this.transform);
			horse.SpriteRenderer.sprite = Sprite.Create(bodyTexture, new Rect(0, 0, bodyTexture.width, bodyTexture.height), Vector2.one * 0.5f);
			horse.WinnerSprite = Sprite.Create(winnerTexture, new Rect(0, 0, winnerTexture.width, winnerTexture.height), Vector2.one * 0.5f);
			horse.SpriteRenderer.gameObject.AddComponent<PolygonCollider2D>();

			horse.transform.position = this.HorseSpawnPoint.position + (Vector3)Random.insideUnitCircle;

			this.horses.Add(horse);

			yield return null;
		}
	}

	public IEnumerator StartRace()
	{
		// Play countdown.
		this.UIAnimator.Play("GameStart");

		yield return new WaitForSeconds(3);

		// Release the beasts.
		for (int i = 0; i < this.horses.Count; i++)
		{
			this.horses[i].StartRace();
		}

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
		this.UIAnimator.Play("GameOver");
		this.VictoryImage.sprite = winner.WinnerSprite;

		// Stop all horses.
		foreach (HorseController horse in this.horses)
		{
			horse.StopRace();
		}

		yield return new WaitForSeconds(5);

		UnityEngine.SceneManagement.SceneManager.LoadScene(0);
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
		string folderPath = Application.persistentDataPath.Replace("/", "\\");
		if (!System.IO.Directory.Exists(folderPath))
		{
			return;
		}

		string filePath = folderPath + "\\Level.png";
		Texture2D levelTexture = this.LoadTexture(filePath, 1920);
		if (levelTexture != null)
		{
			this.Level.sprite = Sprite.Create(levelTexture, new Rect(0, 0, levelTexture.width, levelTexture.height), Vector2.one * 0.5f);
			GameObject.Destroy(this.Level.gameObject.GetComponent<PolygonCollider2D>());
			this.Level.gameObject.AddComponent<PolygonCollider2D>();
		}

		filePath = folderPath + "\\Background.png";
		Texture2D backgroundTexture = this.LoadTexture(filePath, 1920);
		if (backgroundTexture != null)
		{
			this.Background.sprite = Sprite.Create(backgroundTexture, new Rect(0, 0, backgroundTexture.width, backgroundTexture.height), Vector2.one * 0.5f);
		}
	}

	public void UI_StartRace()
	{
		if (this.horses.Count == 0)
		{
			return;
		}

		this.StartCoroutine(this.StartRace());
	}

	private Texture2D LoadTexture(string path, int maxSize = 2048)
	{
		try
		{
			byte[] bytes = System.IO.File.ReadAllBytes(path);

			Texture2D texture = new Texture2D(2, 2);
			texture.LoadImage(bytes);

			if (texture.width > maxSize || texture.height > maxSize)
			{
				float resizeRatio = Mathf.Min(maxSize * 1f / texture.width, maxSize * 1f / texture.height);
				texture.Reinitialize((int)(texture.width * resizeRatio), (int)(texture.height * resizeRatio));
				texture.Apply();
			}
			else
			{
				texture.Compress(false);
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
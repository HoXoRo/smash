using System.Collections.Generic;
using DG.Tweening;
using Level;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI
{
	public class BallSignController : MonoBehaviour
	{
		[SerializeField]
		private GameObject stagedRoot;

		[SerializeField]
		private GameObject nonStagedRoot;

		[SerializeField]
		[Header("Staged")]
		private TextMeshProUGUI stagedBallsText;

		[SerializeField]
		private TextMeshProUGUI stagedBallsTitleText;

		[SerializeField]
		private GameObject staged2Root;

		[SerializeField]
		private GameObject staged3Root;

		[SerializeField]
		private Image staged2Fill;

		[SerializeField]
		private Image staged3Fill;

		[Header("Non-Staged")]
		[SerializeField]
		private TextMeshProUGUI nonStagedBallsText;

		[SerializeField]
		private TextMeshProUGUI nonStagedBallsTitleText;

		[SerializeField]
		[Header("Refs for Difficulty")]
		private Image nonStagedBGImage;

		[SerializeField]
		private Image stagedBGImage;

		[SerializeField]
		private List<Image> ballsTextBGImages;

		[SerializeField]
		private Image dots2BGImage;

		[SerializeField]
		private Image dots3BGImage;

		[Header("Difficulty sprites")]
		[SerializeField]
		private Sprite[] nonStagedBGSprites;

		[SerializeField]
		private Sprite[] stagedBGSprites;

		[SerializeField]
		private Sprite[] ballsTextBGSprites;

		[SerializeField]
		private Sprite[] dots2BGSprites;

		[SerializeField]
		private Sprite[] dots3BGSprites;

		[SerializeField]
		[Header("Difficulty text materials")]
		private Material[] textMaterials;

		private const float Staged2Value0 = 0.45f;

		private const float Staged3Value0 = 0.297f;

		private const float Staged3Value1 = 0.634f;

		private Image _activeFillImage;

		private int _stageCount;

		public void Init(LevelData levelData)
		{
			KillFillTweens();
			_stageCount = levelData != null ? levelData.GetStageCount() : 1;
			UpdateImagesForDifficulty(levelData != null ? levelData.difficulty : LevelDifficulty.Normal);

			bool staged = _stageCount > 1;
			if (stagedRoot != null)
			{
				stagedRoot.SetActive(staged);
			}
			if (nonStagedRoot != null)
			{
				nonStagedRoot.SetActive(!staged);
			}
			if (staged2Root != null)
			{
				staged2Root.SetActive(_stageCount == 2);
			}
			if (staged3Root != null)
			{
				staged3Root.SetActive(_stageCount == 3);
			}
			_activeFillImage = _stageCount == 2 ? staged2Fill : _stageCount == 3 ? staged3Fill : null;
			Fill(0, 0f);
		}

		private void UpdateImagesForDifficulty(LevelDifficulty difficulty)
		{
			int diff = Mathf.Clamp((int)difficulty, 0, Mathf.Max(0, textMaterials != null ? textMaterials.Length - 1 : 0));
			SetSprite(nonStagedBGImage, nonStagedBGSprites, diff);
			SetSprite(stagedBGImage, stagedBGSprites, diff);
			SetSprite(dots2BGImage, dots2BGSprites, diff);
			SetSprite(dots3BGImage, dots3BGSprites, diff);
			if (ballsTextBGImages != null)
			{
				foreach (Image image in ballsTextBGImages)
				{
					SetSprite(image, ballsTextBGSprites, diff);
				}
			}
			Material material = textMaterials != null && diff < textMaterials.Length ? textMaterials[diff] : null;
			if (material != null)
			{
				if (stagedBallsText != null)
				{
					stagedBallsText.fontMaterial = material;
				}
				if (nonStagedBallsText != null)
				{
					nonStagedBallsText.fontMaterial = material;
				}
				if (stagedBallsTitleText != null)
				{
					stagedBallsTitleText.fontMaterial = material;
				}
				if (nonStagedBallsTitleText != null)
				{
					nonStagedBallsTitleText.fontMaterial = material;
				}
			}
		}

		public void Fill(int stageIndex, float duration)
		{
			if (_activeFillImage == null)
			{
				return;
			}

			_activeFillImage.DOKill();

			float target = 0f;
			if (_stageCount == 2)
			{
				target = stageIndex <= 0 ? Staged2Value0 : 1f;
			}
			else if (_stageCount == 3)
			{
				target = stageIndex <= 0 ? Staged3Value0 : stageIndex == 1 ? Staged3Value1 : 1f;
			}

			if (duration <= 0f)
			{
				_activeFillImage.fillAmount = target;
			}
			else
			{
				_activeFillImage.DOFillAmount(target, duration);
			}
		}

		private void OnDestroy()
		{
			KillFillTweens();
		}

		private void OnDisable()
		{
			KillFillTweens();
		}

		public void UpdateRemainingMoves(int moves)
		{
			string text = moves.ToString();
			if (stagedBallsText != null)
			{
				stagedBallsText.text = text;
			}
			if (nonStagedBallsText != null)
			{
				nonStagedBallsText.text = text;
			}
		}

		private static void SetSprite(Image image, Sprite[] sprites, int index)
		{
			if (image != null && sprites != null && index >= 0 && index < sprites.Length)
			{
				image.sprite = sprites[index];
			}
		}

		private void KillFillTweens()
		{
			staged2Fill?.DOKill();
			staged3Fill?.DOKill();
		}
	}
}

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// UI Interface for contract paper stacks.
/// </summary>
public class CUIContractStackIcon : MonoBehaviour
{
	private GameObject[] _paperGobs;
	private CUIContractStackIconPaper[] _paperIcons;

	private float _scale;
	private Color _primaryColor;
	private Color _activeFrameColor;
	private Color _inactiveFrameColor;
	private RectTransform _parentRect;

	public void Init(int PaperCount, float Scale, Color PrimaryColor, Color ActiveColor, Color InactiveColor)
	{
		_scale = Scale;
		_primaryColor = PrimaryColor;
		_activeFrameColor = ActiveColor;
		_inactiveFrameColor = InactiveColor;
		_parentRect = gameObject.GetComponent<RectTransform>();
		SetPaperCount(PaperCount);
	}

	public void SetPaperCount(int PaperCount)
	{
		if (_paperGobs == null || _paperGobs.Length != PaperCount)
		{
			int addStart = 0;
			int addAmount = 0;

			GameObject[] tempGobs = new GameObject[PaperCount];
			CUIContractStackIconPaper[] tempIcons = new CUIContractStackIconPaper[PaperCount];

			if (_paperGobs == null)
			{
				addAmount = PaperCount;
			}
			else if (_paperGobs.Length < PaperCount)
			{
				addStart = _paperGobs.Length;
				addAmount = PaperCount - addStart;
				Array.Copy(_paperGobs, tempGobs, _paperGobs.Length);
				Array.Copy(_paperIcons, tempIcons, _paperIcons.Length);
			}
			else
			{
				addAmount = 0;
				Array.Copy(_paperGobs, tempGobs, PaperCount);
				Array.Copy(_paperIcons, tempIcons, PaperCount);

				for (int i = PaperCount; i < _paperGobs.Length; ++i)
					GameObject.Destroy(_paperGobs[i]);
			}

			for (int i = addStart; i < addAmount; ++i)
			{
				/*
				tempGobs[i] = GameObject.Instantiate(CGame.UIResources.ContractStackPaperPrefab);
				tempIcons[i] = tempGobs[i].GetComponent<CUIContractStackIconPaper>();
				tempIcons[i].SetTransform(_parentRect, new Vector2(0.0f, i * 10.0f * _scale), _scale);
				tempIcons[i].SetColor(_primaryColor, _activeFrameColor);
				*/
			}

			_paperGobs = tempGobs;
			_paperIcons = tempIcons;
		}
	}
}

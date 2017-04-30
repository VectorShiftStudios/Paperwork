using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class FoldOutTitle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
	public Image BackingImage;
	public Image ArrowImage;
	public Color HighlightColor;
	public Color BackgroundColor;
	public Sprite ArrowOpen;
	public Sprite ArrowClosed;
	public GameObject Content;

	public void OnPointerEnter(PointerEventData EventData)
	{
		BackingImage.color = HighlightColor;
	}

	public void OnPointerExit(PointerEventData EventData)
	{
		BackingImage.color = BackgroundColor;
	}

	public void OnPointerDown(PointerEventData EventData)
	{
		SetFold(!Content.activeInHierarchy);
	}

	public void SetFold(bool Value)
	{
		if (Content.activeSelf == Value)
			return;

		Content.SetActive(Value);

		if (Content.activeSelf)
			ArrowImage.sprite = ArrowOpen;
		else
			ArrowImage.sprite = ArrowClosed;
	}

	void Start()
	{
		
	}
}

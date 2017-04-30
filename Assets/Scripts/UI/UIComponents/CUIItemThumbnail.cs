using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class CUIItemThumbnail : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	private int _guid;

	private CItemAsset _asset;

	public void SetDetails(CItemAsset Asset)
	{
		_asset = Asset;
	}

	void IPointerEnterHandler.OnPointerEnter(PointerEventData EventData)
	{

	}

	void IPointerExitHandler.OnPointerExit(PointerEventData EventData)
	{

	}
}
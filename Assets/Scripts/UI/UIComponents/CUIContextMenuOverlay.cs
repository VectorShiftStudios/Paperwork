using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class CUIContextMenuOverlay : MonoBehaviour, IPointerClickHandler
{
	public void OnPointerClick(PointerEventData EventData)
	{
		CGame.UIManager.mContextMenu.Hide();
	}
}
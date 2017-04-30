using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class CUIRolloverSound : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public int mSoundIndex;

	void IPointerEnterHandler.OnPointerEnter(PointerEventData EventData)
	{
		CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[mSoundIndex]);
	}

	void IPointerExitHandler.OnPointerExit(PointerEventData EventData)
	{
	}
}
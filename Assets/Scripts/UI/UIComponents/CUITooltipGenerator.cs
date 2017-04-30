using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class CUITooltipGenerator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public string mTitle;
	public string mDetails;
	public string mDescription;
	public Vector2 mOffset;
	public Vector2 mPivot;

	private bool _showingToolTip;

	public void SetDetails(string Title, string Details, string Description, Vector2 Offset, Vector2 Pivot)
	{
		mTitle = Title;
		mDetails = Details;
		mDescription = Description;
		mOffset = Offset;
		mPivot = Pivot;
		_showingToolTip = false;
	}

	void IPointerEnterHandler.OnPointerEnter(PointerEventData EventData)
	{
		Vector3 elementWorldPos = GetComponent<RectTransform>().position;
		Vector2 pos = CGame.UIManager.ConvertScreenSpaceToUISpace(new Vector2(elementWorldPos.x, elementWorldPos.y));
		CGame.UIManager.mTooltip.Set(mTitle, mDetails, mDescription, pos + mOffset, mPivot, false);
		_showingToolTip = true;
	}

	void IPointerExitHandler.OnPointerExit(PointerEventData EventData)
	{

		CGame.UIManager.mTooltip.Hide(false);
		_showingToolTip = false;
	}

	public void HideTooltip()
	{
		CGame.UIManager.mTooltip.Hide(false);
		_showingToolTip = false;
	}

	void OnDestroy()
	{
		if (_showingToolTip)
			CGame.UIManager.mTooltip.Hide(false);
	}
}
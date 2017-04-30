using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class CUIListBox : MonoBehaviour 
{
	public GameObject EntryPrefab;
	public GameObject ListBoxEntryPanel;

	private List<CUIListBoxEntry> _entryList;
	public int mSelectedIndex;

	public void Init()
	{
		_entryList = new List<CUIListBoxEntry>();
		mSelectedIndex = -1;
	} 

	public void AddEntry(string Text, int ID)
	{
		GameObject entry = GameObject.Instantiate(EntryPrefab) as GameObject;
		entry.transform.SetParent(ListBoxEntryPanel.transform);
		entry.transform.localScale = Vector3.one;
		CUIListBoxEntry entryComp = entry.GetComponent<CUIListBoxEntry>();

		if (entryComp == null)
		{
			Debug.LogError("Entry Comp Null");
		}

		entryComp.Init(Text, ID);
		int listCount = _entryList.Count;		
		entryComp.EntryButton.onClick.AddListener(() => OnClickSelect(listCount));
		_entryList.Add(entryComp);
	}

	public void Clear()
	{
		for (int i = 0; i < _entryList.Count; ++i)
			GameObject.Destroy(_entryList[i].gameObject);

		_entryList.Clear();
		mSelectedIndex = -1;
	}

	public void SelectIndex(int Index)
	{
		if (Index < 0 || Index >= _entryList.Count)
			Index = -1;
		
		if (mSelectedIndex != -1)
			_entryList[mSelectedIndex].Deselect();

		if (Index != -1)
			_entryList[Index].Select();

		mSelectedIndex = Index;
	}

	public void OnClickSelect(int Index)
	{
		SelectIndex(Index);
	}

	public int GetSelectedID()
	{
		if (mSelectedIndex == -1)
			return -1;

		return _entryList[mSelectedIndex].mID;
	}
}
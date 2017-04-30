using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class CUIListBoxEntry : MonoBehaviour
{	
	public Text EntryText;
	public Button EntryButton;
	public GameObject SelectedIcon;

	public int mID;

	public void Init(string Text, int ID)
	{
		EntryText.text = Text;
		mID = ID;		
	}

	public void Select()
	{	
		SelectedIcon.SetActive(true);
		ColorBlock cols = EntryButton.colors;
		//cols.normalColor = new Color(0.4f, 0.4f, 0.4f, 1.0f);
		EntryButton.colors = cols;
	}

	public void Deselect()
	{
		SelectedIcon.SetActive(false);
		ColorBlock cols = EntryButton.colors;
		//cols.normalColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
		EntryButton.colors = cols;
	}
}
using System;
using System.Collections.Generic;
using UnityEngine;

public enum ESelectionType
{
	ITEM,
	UNIT,
	PICKUP
}

/// <summary>
/// Can be selected in the main viewport.
/// </summary>
public interface ISelectable
{
	ESelectionType GetType();
	string GetInfo();
	void PrintInfo();
	int GetID();
	bool IsStillActive();
	Vector3 GetScreenPos();
	Vector3 GetVisualPos();
	CStateView GetStateView();
	void Select();
	void Deselect();
	void Hover();
	void HoverOut();
	bool Intersect(Ray R, ref float D);
	void GetRenderers(List<Renderer> Renderers);
}
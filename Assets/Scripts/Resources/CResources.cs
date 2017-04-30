using System;
using System.Collections.Generic;
using UnityEngine;

public class CResources
{
	private AudioClip[][] _AudioGroups;

	public CResources()
	{
		// Resources
		_AudioGroups = new AudioClip[10][];

		// Footstep1
		_AudioGroups[0] = new[] { CGame.PrimaryResources.AudioClips[0], CGame.PrimaryResources.AudioClips[1], CGame.PrimaryResources.AudioClips[2] };
		// Swing
		_AudioGroups[1] = new[] { CGame.PrimaryResources.AudioClips[3] };
		// Hit
		_AudioGroups[2] = new[] { CGame.PrimaryResources.AudioClips[4] };
	}

	public AudioClip GetAudioClip(int GroupId)
	{
		int rnd = (int)(CGame.UniversalRandom.GetNextFloat() * _AudioGroups[GroupId].Length);
		return _AudioGroups[GroupId][rnd];
	}
}
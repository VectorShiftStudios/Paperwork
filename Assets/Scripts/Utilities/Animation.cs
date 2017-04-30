using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// This tells the main game what animations are available, and tells the animation importer
/// how to split up the animation FBX files.
/// </summary>
public class Animation
{
	public class CAnimationEntry
	{
		public string mName;
		public float mFPS;
		public float mStartTime;
		public float mDuration;
		public bool mLoopable;
		public float mSpeed;

		public CAnimationEntry(string Name, float Duration, bool Loopable = false)
		{
			mFPS = 60.0f;
			mName = Name;
			mDuration = Duration;
			mStartTime = 0.0f;
			mSpeed = 1.0f;
			mLoopable = Loopable;
		}
	}

	public static CAnimationEntry[] FBXEntries = {

		new CAnimationEntry("angry", 1.0f, true),
		new CAnimationEntry("angry_2", 1.0f, true),
		new CAnimationEntry("happy", 1.0f, true),
		new CAnimationEntry("happy_2", 1.0f, true),

		new CAnimationEntry("combat_bashing", 1.0f),
		new CAnimationEntry("combat_briefcase_shooting", 1.0f),
		new CAnimationEntry("combat_briefcase_shooting_2", 1.0f),
		new CAnimationEntry("combat_ranged", 1.0f),
		new CAnimationEntry("combat_slashing", 1.0f),

		new CAnimationEntry("complaining", 1.0f, true),

		new CAnimationEntry("consume_food", 1.0f, true),

		new CAnimationEntry("dying_1", 1.0f),
		new CAnimationEntry("dying_2", 1.0f),

		new CAnimationEntry("idle_sitting_action_1", 1.0f, true),
		new CAnimationEntry("idle_sitting_action_2", 1.0f, true),
		new CAnimationEntry("idle_sitting_action_3", 1.0f, true),
		new CAnimationEntry("idle_sitting_action_4", 1.0f, true),
		new CAnimationEntry("idle_sitting_general", 1.0f, true),

		new CAnimationEntry("idle_standing_action_1", 1.0f),
		new CAnimationEntry("idle_standing_action_2", 1.0f),
		new CAnimationEntry("idle_standing_general", 1.0f, true),

		new CAnimationEntry("inspector_idle_1", 1.5f),
		new CAnimationEntry("inspector_idle_2", 1.0f),
		new CAnimationEntry("inspector_idle_3", 1.0f),
		new CAnimationEntry("inspector_idle_4", 1.0f),

		new CAnimationEntry("inspector_walking", 1.0f),

		new CAnimationEntry("lost_or_confused", 1.5f),
		new CAnimationEntry("lost_or_confused_2", 1.5f),

		new CAnimationEntry("resting_sitting_on_couch", 1.0f),

		new CAnimationEntry("running_no_item", 1.0f, true),
		new CAnimationEntry("running_no_item_plus", 1.0f, true),
		new CAnimationEntry("running_with_item", 1.0f, true),

		new CAnimationEntry("use_object_standing", 1.0f, true),

		new CAnimationEntry("walking_director", 1.0f, true),
		new CAnimationEntry("walking_exhausted", 1.0f, true),
		new CAnimationEntry("walking_fired_quit", 1.5f, true),
		new CAnimationEntry("walking_food_in_hand", 1.0f, true),
		new CAnimationEntry("walking_no_item", 1.0f, true),
		new CAnimationEntry("walking_with_item", 1.0f, true),

		new CAnimationEntry("work_working_action_1", 1.0f, true),
		new CAnimationEntry("work_working_action_1a", 1.0f, true),
		new CAnimationEntry("work_working_action_2", 1.0f, true),
		new CAnimationEntry("work_working_action_3", 1.0f, true),
		new CAnimationEntry("work_working_action_3a", 1.0f, true),
	};

	public static CAnimationEntry GetAnimEntry(string Name)
	{
		for (int i = 0; i < FBXEntries.Length; ++i)
		{
			if (FBXEntries[i].mName == Name)
				return FBXEntries[i];
		}	 

		return null;
	}

	public static string GetDefaultAnimation()
	{
		return "idle_standing_general";
	}
}

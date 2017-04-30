using System;
using System.Collections.Generic;
using UnityEngine;

public class CEventDispatcher
{
	public delegate void EventDelegate(int Param);

	private class CCallback
	{
		public object mObject;
		public EventDelegate mDelegate;
 
		public CCallback(object Object, EventDelegate Delegate)
		{
			mObject = Object;
			mDelegate = Delegate;
		}
	}

	private Dictionary<string, List<CCallback>> _listeners = new Dictionary<string,List<CCallback>>();

	public void RegisterListener(string Event, object Object, EventDelegate Delegate)
	{
		List<CCallback> callbacks;
		if (!_listeners.TryGetValue(Event, out callbacks))
		{
			callbacks = new List<CCallback>();
			_listeners.Add(Event, callbacks);
		}

		callbacks.Add(new CCallback(Object, Delegate));
	}

	private void _RemoveCallback(List<CCallback> Callbacks, object Object)
	{
		for (int i = 0; i < Callbacks.Count; ++i)
		{
			if (Callbacks[i].mObject == Object)
			{
				Callbacks.RemoveAt(i);
				--i;
			}
		}
	}

	public void RemoveListener(string Event, object Object)
	{
		List<CCallback> callbacks;
		if (_listeners.TryGetValue(Event, out callbacks))
			_RemoveCallback(callbacks, Object);
	}

	public void RemoveListener(object Object)
	{
		foreach (KeyValuePair<string, List<CCallback>> entry in _listeners)
			_RemoveCallback(entry.Value, Object);
	}

	public void Dispatch(string Event, int Param)
	{
		List<CCallback> callbacks;
		if (_listeners.TryGetValue(Event, out callbacks))
		{
			for (int i = 0; i < callbacks.Count; ++i)
				callbacks[i].mDelegate(Param);
		}		
	}
}

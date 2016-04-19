using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RogoDigital.Lipsync;

public static class LipSyncExtensions {
	/// <summary>
	/// Finds a named child or grandchild of a Transform.
	/// </summary>
	/// <param name="aParent"></param>
	/// <param name="aName"></param>
	/// <returns></returns>
	public static Transform FindDeepChild(this Transform aParent, string aName) {
		var result = aParent.Find(aName);
		if (result != null)
			return result;
		foreach (Transform child in aParent) {
			result = child.FindDeepChild(aName);
			if (result != null)
				return result;
		}
		return null;
	}

	public static EmotionMarker PreviousMarker(this List<EmotionMarker> list, EmotionMarker current) {
		int index = list.IndexOf(current) - 1;
		if (index >= 0)
			return list[index];
		return null;
	}
	public static EmotionMarker NextMarker(this List<EmotionMarker> list, EmotionMarker current) {
		int index = list.IndexOf(current) + 1;
		if (index < list.Count)
			return list[index];
		return null;
	}
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace RogoDigital.Lipsync{
	public class LipSyncProject : ScriptableObject {
		[SerializeField]
		public string[] emotions;
		[SerializeField]
		public Color[] emotionColors;
		[SerializeField]
		public List<string> gestures;
	}
}
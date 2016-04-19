using UnityEngine;
using System.Collections.Generic;

namespace RogoDigital.Lipsync {
	[System.Serializable]
	public class SpriteManager : MonoBehaviour {
		[SerializeField]
		public List<Sprite> availableSprites = new List<Sprite>();
		[SerializeField]
		public List<SpriteGroup> groups = new List<SpriteGroup>();

		[System.Serializable]
		public class SpriteGroup {
			[SerializeField]
			public string groupName;
			[SerializeField]
			public SpriteRenderer spriteRenderer;

			public SpriteGroup(string groupName) {
				this.groupName = groupName;
			}
		}
	}
}

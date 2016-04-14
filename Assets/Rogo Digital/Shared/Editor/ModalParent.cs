using UnityEngine;
using UnityEditor;
using System.Collections;

namespace RogoDigital {
	public class ModalParent : EditorWindow {
		public ModalWindow currentModal = null;
		public Vector2 center {
			get {
				return new Vector2(position.x+(position.width/2) , position.y+(position.height/2));
			}
		}

		public virtual void OnModalGUI () {	
		}
		
		void OnGUI () {
			//EditorGUI.BeginDisabledGroup(currentModal != null);
			if(currentModal != null){
				GUI.color = new Color(0.6f , 0.6f , 0.6f);
				GUI.Box(new Rect(0,0,position.width,position.height) ,"");
				Event.current = null;
			}
			OnModalGUI();
			GUI.color = Color.white;
			//EditorGUI.EndDisabledGroup();
		}

		void OnFocus () {
			if(currentModal != null){
				EditorApplication.Beep();
				currentModal.Focus();
			}
		}
	}
}
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using System.Collections;

namespace RogoDigital {
	public class WizardWindow : EditorWindow {

		public int currentStep = 1;
		public int totalSteps = 1;
		public bool canContinue = true;
		public string topMessage = "";

		private AnimFloat progressBar;
		private Texture2D white;

		void OnEnable () {
			progressBar = new AnimFloat(0 , Repaint);
			progressBar.speed = 3;
			white = Resources.Load<Texture2D>("Lipsync/white");
		}

		void OnGUI () {
			Rect topbar = EditorGUILayout.BeginHorizontal();
			GUI.Box(topbar , "" , EditorStyles.toolbar);
			GUILayout.FlexibleSpace();
			GUILayout.Box (topMessage+" Step "+currentStep.ToString()+"/"+totalSteps.ToString() , EditorStyles.label);
			GUILayout.FlexibleSpace();
			GUILayout.Box("" , EditorStyles.toolbar);
			EditorGUILayout.EndHorizontal();
			GUI.color = Color.grey;
			GUI.DrawTexture(new Rect(0 , topbar.height , topbar.width , 3) , white);
			GUI.color = new Color(1f , 0.77f, 0f);
			progressBar.target = (topbar.width/totalSteps)*currentStep;
			GUI.DrawTexture(new Rect(0 , topbar.height , progressBar.value , 3) , white);
			GUI.color = Color.white;
			GUILayout.Space(20);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(20);
			EditorGUILayout.BeginVertical();

			OnWizardGUI();

			EditorGUILayout.EndVertical();
			GUILayout.Space(20);
			EditorGUILayout.EndHorizontal();

			GUILayout.FlexibleSpace();
			Rect bottomBar = EditorGUILayout.BeginHorizontal(GUILayout.Height(50));
			EditorGUILayout.BeginVertical();
			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginHorizontal();
			GUI.Box(bottomBar , "" , EditorStyles.helpBox);
			GUILayout.FlexibleSpace();
			GUILayout.Space(20);
			if(GUILayout.Button((currentStep == 1)?"Cancel":"Back" , GUILayout.Height(30) , GUILayout.MaxWidth(200))){
				OnBackPressed();
				if(currentStep > 1) {
					currentStep--;
				}else{
					Close();
				}
			}
			GUILayout.Space(10);
			GUILayout.FlexibleSpace();
			GUILayout.Space(10);
			if(canContinue){
				if(GUILayout.Button((currentStep == totalSteps)?"Finish":"Continue" , GUILayout.Height(30) , GUILayout.MaxWidth(200))){
					OnContinuePressed();
					GUI.FocusControl("");
					if(currentStep < totalSteps) {
						currentStep++;
					}else{
						Close();
					}
				}
			}else{
				GUI.color = Color.grey;
				GUILayout.Box("Continue" , (GUIStyle)"button" , GUILayout.Height(30) , GUILayout.MaxWidth(200));
				GUI.color = Color.white;
			}
			GUILayout.Space(20);
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
		}
			
		public virtual void OnContinuePressed () {
		}

		public virtual void OnBackPressed () {
		}

		public virtual void OnWizardGUI () {
		}
	}
}
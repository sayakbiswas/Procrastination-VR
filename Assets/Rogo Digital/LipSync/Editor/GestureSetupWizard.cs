using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.AnimatedValues;

using System.Collections;
using System.Collections.Generic;
using RogoDigital.Lipsync;
using RogoDigital;

public class GestureSetupWizard : WizardWindow {
	private LipSync component;
	private AnimatorController controller;
	private LipSyncProject settings;

	// Step 1
	private int newLayerChoice = 0;
	private string newLayerName = "LipSync Gestures";
	private int layerSelected = 0;

	// Step 2
	private float transitionTime = 0.2f;
	private bool allowGestureInterrupts = true;
	private string[] triggerNames;

	public override void OnWizardGUI () {
		
		switch (currentStep){
		case 1:
			newLayerChoice = GUILayout.Toolbar(newLayerChoice , new string[]{"Create New Layer" , "Use Existing Layer"});
			GUILayout.Space(10);
			if(newLayerChoice == 0) {
				GUILayout.BeginHorizontal(GUILayout.Height(25));
				newLayerName = EditorGUILayout.TextField("New Layer Name" , newLayerName , GUILayout.Height(20));
				GUILayout.EndHorizontal();

				// Logic
				if(string.IsNullOrEmpty(newLayerName)){
					canContinue = false;
				}else{
					canContinue = true; 
				}
			}else{
				GUILayout.Label("Chose a Layer");
				GUILayout.Space(10);
				for(int a = 0 ; a < controller.layers.Length ; a++) {
					GUILayout.BeginHorizontal(GUILayout.Height(25));
					GUILayout.Space(10);
					bool selected = EditorGUILayout.Toggle(layerSelected == a , EditorStyles.radioButton , GUILayout.Width(30));
					layerSelected = selected?a:layerSelected;
					GUILayout.Space(5);
					GUILayout.Label(controller.layers[a].name);
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();

					canContinue = true;
				}
			}
			break;
		case 2:
			GUILayout.Label("Layer Settings");
			GUILayout.Space(5);
			transitionTime = EditorGUILayout.FloatField("Transition Time" , transitionTime);
			allowGestureInterrupts = EditorGUILayout.Toggle(new GUIContent("Allow Gesture Interrupts" , "Should hitting a new Gesture marker interrupt the previous one, or should it be queued?") , allowGestureInterrupts);
			GUILayout.Space(15);
			GUILayout.Label("Trigger Settings");
			GUILayout.Space(5);
			for(int a = 0 ; a < triggerNames.Length ; a++) {
				GUILayout.BeginHorizontal(GUILayout.Height(25));
				GUILayout.Space(4);
				GUILayout.Label("Trigger for '"+settings.gestures[a]+"' is called: ");
				triggerNames[a] = GUILayout.TextField(triggerNames[a]);
				GUILayout.EndHorizontal();
			}
			break;
		}
	}

	public override void OnContinuePressed () {
		switch (currentStep){
		case 1:
			triggerNames = new string[settings.gestures.Count];
			for(int a = 0 ; a < settings.gestures.Count ; a++) {
				triggerNames[a] = settings.gestures[a]+"_trigger";
			}

			break;
		case 2:
			if(newLayerChoice == 0){
				controller.AddLayer(newLayerName);
				layerSelected = controller.layers.Length-1;
			}

			// Create Triggers
			for(int a = 0 ; a < settings.gestures.Count ; a ++) {
				controller.AddParameter(triggerNames[a] , AnimatorControllerParameterType.Trigger);
			}
				
			AnimatorStateMachine sm = controller.layers[layerSelected].stateMachine;

			// Create States and transitions
			AnimatorState defaultState = null;
			defaultState = sm.AddState("None");
			sm.defaultState = defaultState;

			for(int a = 0 ; a < settings.gestures.Count ; a ++) {
				AnimatorState newState = null;

				newState = sm.AddState(settings.gestures[a]);
				newState.motion = component.gestures[a].clip;
				AnimatorStateTransition transition = null;

				transition = defaultState.AddTransition(newState);	
				transition.duration = transitionTime;
				transition.interruptionSource = allowGestureInterrupts?TransitionInterruptionSource.SourceThenDestination:TransitionInterruptionSource.None;
				transition.AddCondition(AnimatorConditionMode.If , 0 , triggerNames[a]);

				transition = newState.AddTransition(defaultState);
				transition.hasExitTime = true;
				transition.duration = transitionTime;
				transition.interruptionSource = TransitionInterruptionSource.Destination;

				component.gestures[a].triggerName = triggerNames[a];
			}
			component.gesturesLayer = layerSelected;

			break;
		}
	}

	public static void ShowWindow(LipSync component , AnimatorController controller) {
		GestureSetupWizard window = EditorWindow.GetWindow <GestureSetupWizard> (true);
		window.component = component;
		window.controller = controller;
		window.topMessage = "Setting up Gestures for "+controller.name+".";
		window.totalSteps = 2;
		window.Focus();
		window.titleContent = new GUIContent("Gesture Setup Wizard");

		//Get Settings File
		window.settings = (LipSyncProject)AssetDatabase.LoadAssetAtPath("Assets/Rogo Digital/LipSync/ProjectSettings.asset" , typeof(LipSyncProject));
		if(window.settings == null){
			window.settings = ScriptableObject.CreateInstance<LipSyncProject>();

			LipSyncProject newSettings = ScriptableObject.CreateInstance<LipSyncProject>();
			newSettings.emotions = new string[]{"default"};
			newSettings.emotionColors = new Color[]{new Color(1f,0.7f,0.1f)};

			EditorUtility.CopySerialized (newSettings, window.settings);
			AssetDatabase.CreateAsset(window.settings , "Assets/Rogo Digital/LipSync/ProjectSettings.asset");
			AssetDatabase.Refresh();
			DestroyImmediate(newSettings);
		}
	}
}

using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using RogoDigital;
using System.Text;
using RogoDigital.Lipsync;
using System.Collections.Generic;

[CustomEditor(typeof(EyeController))]
public class EyeControllerEditor : Editor {
	private EyeController myTarget;

	private Texture2D logo;

	private SerializedObject serializedTarget;

	// Blinking
	private AnimBool showBlinking;
	private BlendSystemEditor blendSystemEditor;

	private SerializedProperty leftEyeBlinkBlendshape;
	private SerializedProperty rightEyeBlinkBlendshape;
	private SerializedProperty minimumBlinkGap;
	private SerializedProperty maximumBlinkGap;
	private SerializedProperty blinkSpeed;

	// Looking Shared
	private AnimBool showLookShared;
	private SerializedProperty lefteye;
	private SerializedProperty righteye;

	private SerializedProperty eyeRotationRangeX;
	private SerializedProperty eyeRotationRangeY;
	private SerializedProperty eyeLookOffset;
	private SerializedProperty eyeTurnSpeed;

	// Random Looking
	private AnimBool showRandomLook;
	private SerializedProperty minimumChangeDirectionGap;
	private SerializedProperty maximumChangeDirectionGap;

	// Look Target
	private AnimBool showLookTarget;
	private SerializedProperty viewTarget;
	private SerializedProperty targetWeight;

	private SerializedProperty autoTarget;
	private AnimBool showAutoTarget;
	private SerializedProperty autoTargetTag;
	private SerializedProperty autoTargetDistance;

	private int blendSystemNumber = 0;
	private List<System.Type> blendSystems;
	private List<string> blendSystemNames;

	private GUIContent[] blendables;
	private int[] blendShapeNumbers;

	void OnEnable () {
		if(!EditorGUIUtility.isProSkin){
			logo = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Eye Controller/Light/EyeController_logo.png");
		} else {
			logo = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Eye Controller/Dark/EyeController_logo.png");
		}

		myTarget = (EyeController)target;
		serializedTarget = new SerializedObject(target);
		if(myTarget.blendSystem == null) myTarget.blendSystem = myTarget.GetComponent<BlendSystem>();

		leftEyeBlinkBlendshape = serializedTarget.FindProperty("leftEyeBlinkBlendshape");
		rightEyeBlinkBlendshape = serializedTarget.FindProperty("rightEyeBlinkBlendshape");
		minimumBlinkGap = serializedTarget.FindProperty("minimumBlinkGap");
		maximumBlinkGap = serializedTarget.FindProperty("maximumBlinkGap");
		blinkSpeed = serializedTarget.FindProperty("blinkSpeed");

		lefteye = serializedTarget.FindProperty("lefteye");
		righteye = serializedTarget.FindProperty("righteye");
		eyeRotationRangeX = serializedTarget.FindProperty("eyeRotationRangeX");
		eyeRotationRangeY = serializedTarget.FindProperty("eyeRotationRangeY");
		eyeLookOffset = serializedTarget.FindProperty("eyeLookOffset");
		eyeTurnSpeed = serializedTarget.FindProperty("eyeTurnSpeed");

		minimumChangeDirectionGap = serializedTarget.FindProperty("minimumChangeDirectionGap");
		maximumChangeDirectionGap = serializedTarget.FindProperty("maximumChangeDirectionGap");

		viewTarget = serializedTarget.FindProperty("viewTarget");
		targetWeight = serializedTarget.FindProperty("targetWeight");

		autoTarget = serializedTarget.FindProperty("autoTarget");
		autoTargetTag = serializedTarget.FindProperty("autoTargetTag");
		autoTargetDistance = serializedTarget.FindProperty("autoTargetDistance");

		CreateBlendSystemEditor();

		showBlinking = new AnimBool(myTarget.blinkingEnabled , Repaint);
		showRandomLook = new AnimBool(myTarget.randomLookingEnabled , Repaint);
		showLookTarget = new AnimBool(myTarget.targetEnabled , Repaint);
		showAutoTarget = new AnimBool(myTarget.autoTarget , Repaint);
		showLookShared = new AnimBool(myTarget.randomLookingEnabled||myTarget.targetEnabled , Repaint);

		if(myTarget.blendSystem != null){
			if(myTarget.blendSystem.isReady){
				GetBlendShapes();
			}
		}
	}

	void ChangeBlendSystem () {
		if(myTarget.GetComponent<BlendSystem>() != null){
			if(blendSystems[blendSystemNumber] != myTarget.GetComponent<BlendSystem>().GetType()){
				BlendSystem[] oldSystems = myTarget.GetComponents<BlendSystem>();
				foreach(BlendSystem system in oldSystems){
					DestroyImmediate(system);
				}

				myTarget.gameObject.AddComponent(blendSystems[blendSystemNumber]);
				myTarget.blendSystem = myTarget.GetComponent<BlendSystem>();
				CreateBlendSystemEditor();
			}
		}else{
			myTarget.gameObject.AddComponent(blendSystems[blendSystemNumber]);
			myTarget.blendSystem = myTarget.GetComponent<BlendSystem>();
			CreateBlendSystemEditor();
		}
	}

	public override void OnInspectorGUI () {
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Box(logo , GUIStyle.none);
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		GUILayout.Space(20);

		serializedTarget.Update();

		Rect lineRect;
		EditorGUILayout.HelpBox("Enable or disable Eye Controller functionality below." , MessageType.Info);
		GUILayout.Space(10);
			
		// Blinking
		lineRect = EditorGUILayout.BeginHorizontal();
		GUI.Box(lineRect , "" , (GUIStyle)"flow node 0");
		GUILayout.Space(10);
		myTarget.blinkingEnabled = EditorGUILayout.ToggleLeft("Blinking" , myTarget.blinkingEnabled , EditorStyles.largeLabel , GUILayout.ExpandWidth(true) , GUILayout.Height(20));
		showBlinking.target = myTarget.blinkingEnabled;
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
		EditorGUI.indentLevel++;
		if(EditorGUILayout.BeginFadeGroup(showBlinking.faded)){
			GUILayout.Space(5);
			if(blendSystems == null) {
				FindBlendSystems();
			}
			if(blendSystems.Count == 0){
				EditorGUILayout.Popup("Blend System" , 0 , new string[]{"No BlendSystems Found"});
			}else{
				if(myTarget.blendSystem == null){
					EditorGUI.BeginChangeCheck();
					blendSystemNumber = EditorGUILayout.Popup("Blend System" , blendSystemNumber , blendSystemNames.ToArray() , GUIStyle.none);
					if(EditorGUI.EndChangeCheck()){
						ChangeBlendSystem();
					}
					GUI.Box(new Rect(EditorGUIUtility.labelWidth+GUILayoutUtility.GetLastRect().x , GUILayoutUtility.GetLastRect().y , GUILayoutUtility.GetLastRect().width , GUILayoutUtility.GetLastRect().height) , "Select a BlendSystem" , EditorStyles.popup);
				}else{
					EditorGUI.BeginChangeCheck();
					blendSystemNumber = EditorGUILayout.Popup("Blend System" , blendSystemNumber , blendSystemNames.ToArray());
					if(EditorGUI.EndChangeCheck()){
						ChangeBlendSystem();
					}
				}
			}

			EditorGUILayout.Space();
			if(myTarget.blendSystem == null){
				GUILayout.Label("No BlendSystem Selected");
			}else{
				if(blendSystemEditor == null) CreateBlendSystemEditor();
				blendSystemEditor.OnInspectorGUI();
				if(!myTarget.blendSystem.isReady){
					GUILayout.Space(10);
					GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					if(GUILayout.Button("Continue" , GUILayout.MaxWidth(200))) {
						myTarget.blendSystem.OnVariableChanged();
					}
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					GUILayout.Space(10);
				}
			}

			GUILayout.Space(10);

			if(myTarget.blendSystem == null){
				EditorGUILayout.HelpBox("Select a BlendSystem to enable blinking." , MessageType.Warning);
			}else if(!myTarget.blendSystem.isReady){
				EditorGUILayout.HelpBox("BlendSystem not set up." , MessageType.Warning);
			}else if(blendables == null) {
				GetBlendShapes();
			}else{
				EditorGUILayout.IntPopup(leftEyeBlinkBlendshape , blendables , blendShapeNumbers , new GUIContent("Left Blink "+myTarget.blendSystem.blendableDisplayName));
				EditorGUILayout.IntPopup(rightEyeBlinkBlendshape , blendables , blendShapeNumbers , new GUIContent("Right Blink "+myTarget.blendSystem.blendableDisplayName));
			}
			GUILayout.Space(10);

			float minGap = minimumBlinkGap.floatValue;
			float maxGap = maximumBlinkGap.floatValue;

			MinMaxSliderWithNumbers(new GUIContent("Blink Gap" , "Time, in seconds, between blinks.") , ref minGap , ref maxGap , 0.1f , 20);

			minimumBlinkGap.floatValue = minGap;
			maximumBlinkGap.floatValue = maxGap;

			EditorGUILayout.PropertyField(blinkSpeed , new GUIContent("Blink Duration" , "How long each blink takes."));
			GUILayout.Space(10);
		}
		FixedEndFadeGroup(showBlinking.faded);
		EditorGUI.indentLevel--;

		// Random Look Direction
		lineRect = EditorGUILayout.BeginHorizontal();
		GUI.Box(lineRect , "" , (GUIStyle)"flow node 0");
		GUILayout.Space(10);
		myTarget.randomLookingEnabled = EditorGUILayout.ToggleLeft("Random Looking" , myTarget.randomLookingEnabled , EditorStyles.largeLabel , GUILayout.ExpandWidth(true) , GUILayout.Height(20));
		showRandomLook.target = myTarget.randomLookingEnabled;
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
		EditorGUI.indentLevel++;
		if(EditorGUILayout.BeginFadeGroup(showRandomLook.faded)){
			GUILayout.Space(5);

			float minGap = minimumChangeDirectionGap.floatValue;
			float maxGap = maximumChangeDirectionGap.floatValue;

			MinMaxSliderWithNumbers(new GUIContent("Change Direction Gap" , "Time, in seconds, between the eyes turning to a new direction.") , ref minGap , ref maxGap , 1f , 30);

			minimumChangeDirectionGap.floatValue = minGap;
			maximumChangeDirectionGap.floatValue = maxGap;

			float minX = eyeRotationRangeY.vector2Value.x;
			float maxX = eyeRotationRangeY.vector2Value.y;
			float minY = eyeRotationRangeX.vector2Value.x;
			float maxY = eyeRotationRangeX.vector2Value.y;

			GUILayout.Space(10);

			MinMaxSliderWithNumbers(new GUIContent("Horizontal Look Range" , "The minimum and maximum horizontal angles random look directions will be between") , ref minX , ref maxX , -90 , 90);
			MinMaxSliderWithNumbers(new GUIContent("Vertical Look Range" , "The minimum and maximum vertical angles random look directions will be between") , ref minY , ref maxY , -90 , 90);

			eyeRotationRangeY.vector2Value = new Vector2(minX , maxX);
			eyeRotationRangeX.vector2Value = new Vector2(minY , maxY);

			GUILayout.Space(10);
		}
		FixedEndFadeGroup(showRandomLook.faded);
		EditorGUI.indentLevel--;

		// Look Targets
		lineRect = EditorGUILayout.BeginHorizontal();
		GUI.Box(lineRect , "" , (GUIStyle)"flow node 0");
		GUILayout.Space(10);
		myTarget.targetEnabled = EditorGUILayout.ToggleLeft("Look At Target" , myTarget.targetEnabled , EditorStyles.largeLabel , GUILayout.ExpandWidth(true) , GUILayout.Height(20));
		showLookTarget.target = myTarget.targetEnabled;
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
		EditorGUI.indentLevel++;
		if(EditorGUILayout.BeginFadeGroup(showLookTarget.faded)){
			GUILayout.Space(5);
			if(EditorGUILayout.BeginFadeGroup(1-showAutoTarget.faded)){
				EditorGUILayout.PropertyField(viewTarget , new GUIContent("Target" , "Transform to look at."));
				GUILayout.Space(10);
			}
			FixedEndFadeGroup(1-showAutoTarget.faded);
			EditorGUILayout.PropertyField(autoTarget , new GUIContent("Use Auto Target"));
			showAutoTarget.target = myTarget.autoTarget;
			if(EditorGUILayout.BeginFadeGroup(showAutoTarget.faded)){
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(autoTargetTag , new GUIContent("Auto Target Tag" , "Tag to use when searching for targets."));
				EditorGUILayout.PropertyField(autoTargetDistance , new GUIContent("Auto Target Distance" , "The maximum distance between a target and the character for it to be targeted."));
				EditorGUI.indentLevel--;
				GUILayout.Space(10);
			}
			FixedEndFadeGroup(showAutoTarget.faded);
			EditorGUILayout.PropertyField(eyeLookOffset , new GUIContent("Rotation Offset" , "Offset applied to eye rotations. (Used if eyes don't look down the Z axis.)"));
			EditorGUILayout.Slider(targetWeight , 0 , 1 , new GUIContent("Look At Amount"));
			GUILayout.Space(10);
		}
		FixedEndFadeGroup(showLookTarget.faded);
		EditorGUI.indentLevel--;

		// Shared Look Controls
		GUILayout.Space(-2);
		lineRect = EditorGUILayout.BeginHorizontal();
		GUI.Box(lineRect , "" , (GUIStyle)"flow node 0");
		GUILayout.Space(24);
		GUILayout.Label("Looking (Shared)" , EditorStyles.largeLabel , GUILayout.ExpandWidth(true) , GUILayout.Height(20));
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
		showLookShared.target = myTarget.targetEnabled||myTarget.randomLookingEnabled;
		EditorGUI.indentLevel++;
		if(EditorGUILayout.BeginFadeGroup(showLookShared.faded)){
			GUILayout.Space(5);
			EditorGUILayout.PropertyField(lefteye , new GUIContent("Left Eye Transform" , "Either a bone, or the eye mesh itself depending on how your model is set up."));
			EditorGUILayout.PropertyField(righteye , new GUIContent("Right Eye Transform" , "Either a bone, or the eye mesh itself depending on how your model is set up."));

			GUILayout.Space(10);

			EditorGUILayout.PropertyField(eyeTurnSpeed , new GUIContent("Eye Turn Speed" , "The speed at which eyes rotate."));
			GUILayout.Space(10);
		}
		FixedEndFadeGroup(showLookShared.faded);
		EditorGUI.indentLevel--;
		GUILayout.Space(10);

		serializedTarget.ApplyModifiedProperties();
	}

	void MinMaxSliderWithNumbers (GUIContent label , ref float minValue , ref float maxValue , float minLimit , float maxLimit) {
		GUILayout.BeginHorizontal();
		EditorGUILayout.PrefixLabel(label);
		minValue = EditorGUILayout.FloatField(minValue , GUILayout.Width(65));
		EditorGUILayout.MinMaxSlider(ref minValue , ref maxValue , minLimit , maxLimit);
		maxValue = EditorGUILayout.FloatField(maxValue , GUILayout.Width(65));
		GUILayout.EndHorizontal();

		minValue = Mathf.Clamp(minValue , minLimit , maxValue);
		maxValue = Mathf.Clamp(maxValue , minValue , maxLimit);
	}

	void GetBlendShapes () {
		if(myTarget.blendSystem.isReady){
			string[] blendableNames = myTarget.blendSystem.GetBlendables();
			blendables = new GUIContent[blendableNames.Length];
			blendShapeNumbers = new int[blendableNames.Length];

			for(int b = 0 ; b < blendableNames.Length ; b++) {
				blendables[b] = new GUIContent(blendableNames[b]);
				blendShapeNumbers[b] = b;
			}
		}
	}

	void FindBlendSystems () {
		blendSystems = new List<System.Type>();
		blendSystemNames = new List<string>();
		foreach(System.Type t in typeof(BlendSystem).Assembly.GetTypes()){
			if(t.IsSubclassOf(typeof(BlendSystem))){
				blendSystems.Add(t);
				blendSystemNames.Add(AddSpaces(t.Name));
			}
		}

		if(myTarget.blendSystem != null){
			blendSystemNumber = blendSystems.IndexOf(myTarget.blendSystem.GetType());
		}
	}

	void CreateBlendSystemEditor () {
		if(myTarget.blendSystem != null){
			myTarget.blendSystem = myTarget.GetComponent<BlendSystem>();
			if(myTarget.blendSystem != null){
				blendSystemEditor = (BlendSystemEditor)Editor.CreateEditor(myTarget.blendSystem);
			}
		}
	}

	public static void FixedEndFadeGroup (float value) {
		if(value == 0f || value == 1f) {
			return;
		}
		EditorGUILayout.EndFadeGroup();
	}

	static string AddSpaces (string input){
		if (string.IsNullOrEmpty(input))
			return "";

		StringBuilder newText = new StringBuilder(input.Length * 2);
		newText.Append(input[0]);
		for (int i = 1; i < input.Length; i++)
		{
			if (char.IsUpper(input[i]) && input[i-1] != ' '){
				if(i+1 < input.Length){
					if(!char.IsUpper(input[i-1]) || !char.IsUpper(input[i+1])){
						newText.Append(' ');
					}
				}else{
					newText.Append(' ');
				}
			}
			newText.Append(input[i]);
		}
		return newText.ToString();
	}
}

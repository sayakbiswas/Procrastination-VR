using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Reflection;
using System.Collections;
using RogoDigital.Lipsync;
using RogoDigital;

[CustomEditor(typeof(BlendSystem) , true)]
public class BlendSystemEditor : Editor {

	private SerializedObject serializedTarget;
	private SerializedProperty[] properties;
	private BlendSystem myTarget;

	private BlendSystemUser[] users;

	void Init () {
		Type sysType = target.GetType();
		MemberInfo[] propInfo = sysType.GetMembers(BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly);

		myTarget = (BlendSystem)target;

		serializedTarget = new SerializedObject(myTarget);
		properties = new SerializedProperty[propInfo.Length];

		for(int a = 0 ; a < properties.Length ; a++){
			properties[a] = serializedTarget.FindProperty(propInfo[a].Name);
		}

		users = myTarget.GetComponents<BlendSystemUser>();
	}

	public override void OnInspectorGUI () {
		if(properties == null){
			Init();
		}
		if(users != null){
			if(users.Length > 1){
				EditorGUILayout.HelpBox("There are multiple components using this BlendSystem. The BlendSystem and its settings will be shared." , MessageType.Info);
			}
		}

		if(serializedTarget != null){
			serializedTarget.Update();
			EditorGUI.BeginChangeCheck();
			foreach(SerializedProperty property in properties){
				if(property != null){
					EditorGUILayout.PropertyField(property , true);
				}
			}
			if(EditorGUI.EndChangeCheck()){
				myTarget.SendMessage("OnVariableChanged" , SendMessageOptions.DontRequireReceiver);
			}
			serializedTarget.ApplyModifiedProperties();
		}
	}
		
	[MenuItem("Assets/Create/Empty BlendSystem")]
	public static void CreateNewBlendSystem () {
		string path = AssetDatabase.GetAssetPath (Selection.activeObject);

		if (path == ""){
			path = "Assets";
		}else if (Path.GetExtension(path) != ""){
			path = path.Replace(Path.GetFileName (AssetDatabase.GetAssetPath (Selection.activeObject)), "");
		}else{
			path += "/";
		}

		StreamWriter writer = File.CreateText(Path.GetFullPath(path)+"MyNewBlendSystem.cs");
		StreamReader reader = File.OpenText(Path.GetFullPath("Assets/Rogo Digital/LipSync/BlendSystems/NewBlendSystemTemplate.cs.txt"));

		string line;
		while((line = reader.ReadLine()) != null) {
			writer.WriteLine(line);
		}

		writer.Close();
		reader.Close();

		AssetDatabase.Refresh();
		Selection.activeObject = AssetDatabase.LoadAssetAtPath(path+"MyNewBlendSystem.cs" , typeof(object));
	}
}

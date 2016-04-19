using RogoDigital;
using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

using System.Media;

public class RDExtensionWindow : EditorWindow {
	public bool downloading = false;
	public string currentExtension = "";

	private int currentFilter = -1;
	private string currentCategory;

	private string baseAddress = "http://updates.rogodigital.com/AssetStore/extensions/";
	private string listFilename = "list-new.xml";

	private List<string> headerLinks = new List<string>();
	private List<string> categories = new List<string>();
	private List<ItemListing> items = new List<ItemListing>();

	private bool gotListing = false;
	private bool connectionFailed = false;

	private Vector2 headerScroll;
	private Vector2 bodyScroll;

	private WWW downloadConnection;
	private ContinuationManager.Job downloadJob;

	private GUIStyle headerLink;
	private GUIStyle headerLinkActive;

	private GUIStyle productTitle;
	private GUIStyle productDescription;

	private GUIStyle headerText;
	private GUIStyle headerTextActive;

	//Images
	Texture2D headerLogo;
	Texture2D headerBG;
	Texture2D headerButtonActive;
	Texture2D defaultIcon;

	Dictionary<string, Texture2D> productIcons = new Dictionary<string, Texture2D>();
	
	void OnEnable() {
		headerLogo = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Shared/RogoDigital_header_left.png");
		headerBG = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Shared/RogoDigital_header_bg.png");
		headerButtonActive = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Shared/RogoDigital_header_button.png");
		defaultIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Shared/default_icon.png");
		ConnectToServer();
	}

	void ConnectToServer() {
		WWW listConnection = new WWW(baseAddress + listFilename);

		ContinuationManager.Add(() => listConnection.isDone, () => {
			if (string.IsNullOrEmpty(listConnection.error)) {
				XmlReader reader = XmlReader.Create(new StringReader(listConnection.text));
				headerLinks = new List<string>();
				items = new List<ItemListing>();

				try {
					while (reader.Read()) {
						if (reader.Name == "product") {
							if (reader.HasAttributes) {
								string name = reader.GetAttribute("name").Replace(" ", "_");
								headerLinks.Add(name);

								//Get Icon
								WWW iconConnection = new WWW(baseAddress + "icons/" + name + ".png");
								ContinuationManager.Add(() => iconConnection.isDone, () => {
									if (string.IsNullOrEmpty(iconConnection.error)) {

										Texture2D icon = iconConnection.texture;
										icon.hideFlags = HideFlags.DontSave;

										if (icon != null) {
											productIcons.Add(name, icon);
											Repaint();
										}

									} else {
										Debug.Log("Attempt to download icon reported error: " + iconConnection.error + " at " + iconConnection.url);
									}
								});
							}
						} else if (reader.Name == "item") {
							if (reader.HasAttributes) {
								//Get Icon
								WWW iconDownload = new WWW(baseAddress + reader.GetAttribute("icon"));
								string itemName = reader.GetAttribute("name");
								string itemCategory = reader.GetAttribute("category");
								string[] itemProducts = reader.GetAttribute("products").Replace(" ", "_").Split(',');
								float itemMinVersion = float.Parse(reader.GetAttribute("minVersion"));
								string itemURL = reader.GetAttribute("url");
								string itemDescription = reader.GetAttribute("description");

								if (!categories.Contains(itemCategory)) {
									categories.Add(itemCategory);
								}

								ContinuationManager.Add(() => iconDownload.isDone, () => {
									if (string.IsNullOrEmpty(iconDownload.error)) {

										Texture2D icon = iconDownload.texture;

										icon.hideFlags = HideFlags.DontSave;

										if (icon != null) {
											items.Add(new ItemListing(
												itemName,
												itemCategory,
												itemProducts,
												itemMinVersion,
												itemURL,
												itemDescription,
												icon
											));
										} else {
											items.Add(new ItemListing(
												itemName,
												itemCategory,
												itemProducts,
												itemMinVersion,
												itemURL,
												itemDescription,
												null
											));
										}

										Repaint();
									}
								});
							}
						}
					}
					gotListing = true;

				} catch (System.Exception exception) {
					Debug.Log("Error loading extension list. Error: " + exception.Message);
					connectionFailed = true;
				}
			} else {
				Debug.Log("Could not connect to extension server. Error: " + listConnection.error);
				connectionFailed = true;
			}

			Repaint();
		});
	}

	// Editor GUI
	public static void ShowWindowGeneric(object startProduct) {
		ShowWindow((string)startProduct);
	}

	[MenuItem("Window/Rogo Digital/Get Extensions", false, 0)]
	public static RDExtensionWindow ShowWindow() {
		return ShowWindow("");
	}

	public static RDExtensionWindow ShowWindow(string startProduct) {
		RDExtensionWindow window;

		window = EditorWindow.GetWindow<RDExtensionWindow>();
		Texture2D icon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Shared/RogoDigital_Icon.png");

		window.titleContent = new GUIContent("Extensions", icon);

		ContinuationManager.Add(() => window.gotListing, () => {
			if (window.headerLinks.Contains(startProduct)) {
				window.currentFilter = window.headerLinks.IndexOf(startProduct);
			}
		});

		return window;
	}

	void OnGUI() {
		//Initialize GUIStyles if needed
		if (headerLink == null) {
			headerLink = new GUIStyle((GUIStyle)"TL Selection H2");
			headerLink.alignment = TextAnchor.MiddleCenter;
			headerLink.fontStyle = FontStyle.Normal;

			headerLink.normal.textColor = new Color(0.2f, 0.2f, 0.2f);
			headerLink.margin = new RectOffset(5, 5, 25, 25);
			headerLink.padding = new RectOffset(0, 0, 6, 12);

			headerLinkActive = new GUIStyle(headerLink);
			headerLinkActive.normal.background = headerButtonActive;
			headerLinkActive.onNormal.background = headerButtonActive;
			headerLinkActive.normal.textColor = Color.white;
		}
		if (headerText == null) {
			headerText = new GUIStyle((GUIStyle)"ControlLabel");
			headerText.alignment = TextAnchor.MiddleCenter;
			headerText.fontStyle = FontStyle.Normal;
			headerText.normal.textColor = new Color(0.2f, 0.2f, 0.2f);
			headerText.margin = new RectOffset(5, 5, 30, 25);

			headerTextActive = new GUIStyle(headerText);
			headerTextActive.normal.textColor = Color.white;
		}
		if (productTitle == null) {
			productTitle = new GUIStyle((GUIStyle)"TL Selection H2");
			productTitle.alignment = TextAnchor.MiddleLeft;
			productTitle.fontStyle = FontStyle.Normal;
			productTitle.fontSize = 16;
			productTitle.margin = new RectOffset(0, 0, 15, 0);
			if (EditorGUIUtility.isProSkin) {
				productTitle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
			} else {
				productTitle.normal.textColor = new Color(0.1f, 0.1f, 0.1f);
			}

			productDescription = new GUIStyle(headerText);
			productDescription.margin = new RectOffset(0, 0, 5, 0);
			productDescription.alignment = TextAnchor.MiddleLeft;
			if (EditorGUIUtility.isProSkin) {
				productDescription.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
			} else {
				productDescription.normal.textColor = new Color(0.3f, 0.3f, 0.3f);
			}
		}
		GUILayout.BeginHorizontal();
		GUI.DrawTexture(new Rect(0, 0, this.position.width, headerBG.height), headerBG);
		GUILayout.Box(headerLogo, GUIStyle.none);

		if (gotListing) {
			GUILayout.Space(-170);
			GUILayout.Box("Products", headerText);

			headerScroll = GUILayout.BeginScrollView(headerScroll, false, false, GUILayout.MaxHeight(headerBG.height+12));
			GUILayout.Space(0);
			int linkCount = 0;
			GUILayout.BeginHorizontal();
			foreach (string product in headerLinks) {
				Rect buttonRect = EditorGUILayout.BeginHorizontal();
				if (productIcons.ContainsKey(product)) {
					if (GUILayout.Button(new GUIContent(productIcons[product], product.Replace("_", " ")), (currentFilter == linkCount ? headerLinkActive : headerLink), GUILayout.MaxHeight(75), GUILayout.MaxWidth(70))) {
						if (currentFilter == linkCount) {
							currentFilter = -1;
						} else {
							currentFilter = linkCount;
						}
					}
				} else {
					if (GUILayout.Button(new GUIContent(product), (currentFilter == linkCount ? headerLinkActive : headerLink), GUILayout.MaxHeight(50))) {
						if (currentFilter == linkCount) {
							currentFilter = -1;
						} else {
							currentFilter = linkCount;
						}
					}
				}
				EditorGUILayout.EndHorizontal();
				EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);
				linkCount++;
				GUILayout.Space(10);
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.EndScrollView();
		} else {
			GUILayout.FlexibleSpace();
			if (connectionFailed) {
				GUILayout.Box("Connection Failed", headerText);
			} else {
				GUILayout.Box("Connecting...", headerText);
			}
			GUILayout.Space(30);
		}
		GUILayout.EndHorizontal();
		GUILayout.Space(-86);
		GUILayout.BeginHorizontal();
		GUILayout.Space(220);
		GUILayout.Box("Categories", headerText);
		GUILayout.Space(30);
		foreach (string category in categories) {
			Rect buttonRect = EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(new GUIContent(category), category != currentCategory?headerText:headerTextActive)) {
				if (currentCategory == category) {
					currentCategory = null;
				} else {
					currentCategory = category;
				}
			}
			EditorGUILayout.EndHorizontal();
			EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		if (connectionFailed) {
			GUILayout.FlexibleSpace();
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Box("Could not connect to server.", headerText);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Retry")) {
				connectionFailed = false;
				ConnectToServer();
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.FlexibleSpace();
		} else if (!gotListing) {
			GUILayout.FlexibleSpace();
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Box("Connecting", headerLink);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.FlexibleSpace();
		} else {
			EditorGUI.BeginDisabledGroup(downloading);
			bodyScroll = GUILayout.BeginScrollView(bodyScroll, false, false);
			foreach (ItemListing listing in items) {
				bool show = false;

				if (currentFilter >= 0) {
					foreach (string product in listing.products) {
						if (headerLinks[currentFilter] == product) {
							show = true;
						}
					}
				} else {
					show = true;
				}
				
				if (!string.IsNullOrEmpty(currentCategory)) {
					if (listing.category != currentCategory) {
						show = false;
					}
				}

				if (show) {
					GUILayout.BeginHorizontal();
					GUILayout.Space(20);
					if (listing.icon != null) {
						GUILayout.Box(listing.icon, GUIStyle.none, GUILayout.Width(90), GUILayout.Height(90));
					} else {
						GUILayout.Box(defaultIcon, GUIStyle.none, GUILayout.Width(90), GUILayout.Height(90));
					}

					GUILayout.Space(30);
					GUILayout.BeginVertical();
					//Hack: Prevents random vertical offset.
					GUILayout.Space(0);
					GUILayout.Box(new GUIContent(listing.name), productTitle, GUILayout.Height(12));
					GUILayout.BeginHorizontal();
					foreach (string product in listing.products) {
						if (productIcons.ContainsKey(product)) {
							GUILayout.Box(new GUIContent(productIcons[product], product.Replace("_", " ")), productTitle, GUILayout.Height(32), GUILayout.Width(38));
						}
					}
					GUILayout.EndHorizontal();
					GUILayout.Box(listing.description, productDescription);
					GUILayout.EndVertical();
					GUILayout.FlexibleSpace();
					GUILayout.BeginVertical();
					GUILayout.Space(30);
					if (GUILayout.Button("Download", GUILayout.Height(30), GUILayout.MaxWidth(100))) {
						downloading = true;
						downloadConnection = new WWW(baseAddress + listing.url);
						currentExtension = listing.name;

						downloadJob = ContinuationManager.Add(() => downloadConnection.isDone, () => {
							downloading = false;
							File.WriteAllBytes(Application.dataPath + "/Rogo Digital/" + currentExtension + ".unitypackage", downloadConnection.bytes);
							ShowNotification(new GUIContent(currentExtension + " Downloaded"));
							AssetDatabase.ImportPackage(Application.dataPath + "/Rogo Digital/" + currentExtension + ".unitypackage", true);
							File.Delete(Application.dataPath + "/Rogo Digital/" + currentExtension + ".unitypackage");
						});
					}
					GUILayout.EndVertical();
					GUILayout.Space(20);
					GUILayout.EndHorizontal();
					GUILayout.Space(20);
				}
			}

			GUILayout.Space(40);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Box("More extensions coming soon. To request support for another asset, post in the forum thread, or send us an email.", productTitle);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.Space(10);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Forum Thread")) {
				Application.OpenURL("http://forum.unity3d.com/threads/alpha-lipsync-a-phoneme-based-lipsyncing-system-for-unity.309324/");
			}
			if (GUILayout.Button("Email Support")) {
				Application.OpenURL("mailto:contact@rogodigital.com");
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.EndScrollView();
			EditorGUI.EndDisabledGroup();

			if (downloading) {
				EditorGUI.ProgressBar(new Rect(10, position.height - 30, position.width - 110, 20), downloadConnection.progress, "Downloading " + currentExtension + " - " + Mathf.Round(downloadConnection.progress * 100).ToString() + "%");
				if (GUI.Button(new Rect(position.width - 90, position.height - 30, 80, 20), "Cancel")) {
					ContinuationManager.Cancel(downloadJob);
					downloadJob = null;
					downloadConnection.Dispose();
					downloading = false;
					ShowNotification(new GUIContent("Download of " + currentExtension + " cancelled."));
				}
			}
		}

	}

	void Update() {
		if (downloadConnection != null) {
			if (!downloadConnection.isDone) {
				Repaint();
			}
		}
	}

	public class ItemListing {
		public string name;
		public string category;
		public string[] products;
		public float minVersion;
		public string url;
		public string description;
		public Texture2D icon;

		public ItemListing(string name, string category, string[] products, float minVersion, string url, string description, Texture2D icon) {
			this.name = name;
			this.category = category;
			this.products = products;
			this.minVersion = minVersion;
			this.url = url;
			this.description = description;
			this.icon = icon;
		}
	}
}

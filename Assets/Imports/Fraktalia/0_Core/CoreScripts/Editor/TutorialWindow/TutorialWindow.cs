using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Video;

#if UNITY_EDITOR
namespace Fraktalia.Core.FraktaliaAttributes
{
	public class TutorialWindow : EditorWindow
	{
		Vector2 scrollPos;
		RenderTexture rendertexture;
		public VideoPlayer player;
		public InfoContent tutorialcontent;

		public static void Init(InfoContent tutorialcontent)
		{
		
			TutorialWindow window = (TutorialWindow)EditorWindow.GetWindow(typeof(TutorialWindow));
			window.tutorialcontent = tutorialcontent;
			window.Show();
		}

		void OnGUI()
		{
			if (this.tutorialcontent == null) return;

			GUIStyle titleH1 = new GUIStyle();
			titleH1.fontSize = 18;
			titleH1.padding = new RectOffset(3, 3, 3, 3);
			titleH1.margin = new RectOffset(5, 5, 5, 5);
			titleH1.fontStyle = FontStyle.Bold;
			titleH1.border = new RectOffset(3, 3, 3, 3);
	

			GUIStyle titleH2 = new GUIStyle();
			titleH2.fontSize = 15;
			titleH2.padding = new RectOffset(3, 3, 3, 3);
			titleH2.margin = new RectOffset(5, 5, 5, 5);
			titleH2.fontStyle = FontStyle.Bold;
			titleH2.border = new RectOffset(3, 3, 3, 3);


			GUIStyle text = new GUIStyle();
			text.fontSize = 12;
			text.padding = new RectOffset(3, 3, 3, 3);
			text.margin = new RectOffset(5, 5, 5, 5);
			text.wordWrap = true;			

			GUIContent content = new GUIContent();

			GUIStyle link = new GUIStyle(GUI.skin.button);
			link.fontSize = 12;
			link.wordWrap = true;
			link.alignment = TextAnchor.MiddleCenter;
			link.richText = true;

			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			InfoSection infotitle = this.tutorialcontent.Title;
			if (infotitle != null)
			{
				EditorGUILayout.LabelField("<color=green>" + infotitle.Title + "</color>", titleH1);
				EditorGUILayout.LabelField(infotitle.Text, text);
			}

			List<InfoSection> tutorialcontent = new List<InfoSection>(this.tutorialcontent.content);
			if (tutorialcontent == null)
			{
				EditorGUILayout.EndScrollView();
				return;
			}

			for (int i = 0; i < tutorialcontent.Count; i++)
			{
				if (tutorialcontent[i] == null) continue;
				EditorGUILayout.LabelField(tutorialcontent[i].Title,titleH2);

				EditorGUILayout.LabelField(tutorialcontent[i].Text,text);

				
				EditorGUILayout.Space();
			}

			Rect controllrect;
			if (this.tutorialcontent.videoURL != "")
			{
				EditorGUILayout.LabelField("<color=green>Related Tutorial Video:</color>", titleH2);
				EditorGUILayout.Space();
				controllrect = EditorGUILayout.GetControlRect(false, 26);

				

				if (GUI.Button(controllrect, "<color=blue>" + "See Youtube Video" + "</color>", link))
				{
					Application.OpenURL(this.tutorialcontent.videoURL);
				}

				if (this.tutorialcontent.showvideo)
				{
					if (rendertexture == null)
					{
						rendertexture = ((RenderTexture)Resources.Load("__EditorVideoScreen"));
					}

					if (player == null)
					{
						player = GameObject.Instantiate(((GameObject)Resources.Load("__EditorVideoPlayer"))).GetComponent<VideoPlayer>();
						player.url = "";
						player.targetTexture = rendertexture;
					}
					
					player.url = this.tutorialcontent.videoURL;

					if (GUI.Button(EditorGUILayout.GetControlRect(false, 30), "Play Video"))
					{
						player.Play();
					}

					if (GUI.Button(EditorGUILayout.GetControlRect(false, 30), "Stop Video"))
					{
						player.Stop();
					}

					Rect rect = EditorGUILayout.GetControlRect(false, 400);
					EditorGUI.DrawPreviewTexture(rect, rendertexture);
				}
				
					
					
				
			}

			if (this.tutorialcontent.videoURL2 != "")
			{
				EditorGUILayout.LabelField("<color=green>Second Tutorial Video:</color>", titleH2);
				EditorGUILayout.Space();
				controllrect = EditorGUILayout.GetControlRect(false, 26);

			

				if (GUI.Button(controllrect, "<color=blue>" + this.tutorialcontent.videoURL2Text + "</color>", link))
				{
					Application.OpenURL(this.tutorialcontent.videoURL2);
				}
			}

			
			EditorGUILayout.LabelField("<color=green>Complete Tutorial Playlist:</color>", titleH2);
			EditorGUILayout.Space();
			controllrect = EditorGUILayout.GetControlRect(false, 26);
		
			if (GUI.Button(controllrect, "<color=blue>" + "Open Playlist" + "</color>", link))
			{
				Application.OpenURL("https://www.youtube.com/playlist?list=PLAiH3Q5-qXIcXCz1AQohOtyZOeFiO5NRU");
			}
		

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			EditorGUILayout.EndScrollView();
			Repaint();
		}

		private void OnDestroy()
		{
			if(player)
			{
				DestroyImmediate(player.gameObject);
			}
		}
	}

	
}
#endif

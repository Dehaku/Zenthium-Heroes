using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;

#endif

namespace Fraktalia.Core.FraktaliaAttributes
{
	
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
	public class InfoPanelAttribute : PropertyAttribute
	{
		public string titletext;
		public Color titleColor;

		public string panelText;
		public Color panelColor;


		public InfoPanelAttribute(string title, string text, string titlecolor, string panelcolor)
		{

			ColorUtility.TryParseHtmlString(titlecolor, out titleColor);
			ColorUtility.TryParseHtmlString(panelcolor, out panelColor);


			titletext = title;
			panelText = text;

			
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(InfoPanelAttribute))]
	public class InfoPanelDrawer : DecoratorDrawer
	{
		

		public override float GetHeight()
		{
			InfoPanelAttribute title = attribute as InfoPanelAttribute;

			GUIContent content = new GUIContent(title.panelText);
			GUIStyle normal = new GUIStyle();
			normal.fontStyle = FontStyle.Normal;
			normal.fontSize = 10;
			normal.richText = true;
			normal.wordWrap = true;
			normal.margin = new RectOffset(5, 5, 5, 5);
			normal.padding = new RectOffset(5, 5, 5, 5);

			float contentSize = normal.CalcHeight(content, EditorGUIUtility.currentViewWidth - 50);

			return 16 + contentSize + 10 + 10;
		}

		public override void OnGUI(Rect position)
		{
			
			
			InfoPanelAttribute title = attribute as InfoPanelAttribute;

			GUIStyle bold = new GUIStyle();
			bold.fontStyle = FontStyle.Bold;
			bold.fontSize = 14;
			bold.richText = true;
			bold.normal.textColor = title.titleColor;
			bold.padding = new RectOffset(5, 5, 5, 5);

			GUIContent content = new GUIContent(title.panelText);
			GUIStyle normal = new GUIStyle();
			normal.fontStyle = FontStyle.Normal;
			normal.fontSize = 10;
			normal.richText = true;
			normal.wordWrap = true;

			normal.margin = new RectOffset(5, 5, 5, 5);
			normal.padding = new RectOffset(5, 50, 5, 5);



			float contentSize = normal.CalcHeight(content, position.width);

			Rect titlerect = position;
			Rect contentrect = position;
			Rect background = position;

			contentrect.y += 18;
			contentrect.height = contentSize + 3;

			background.height = contentSize + 3 + 18;
		
			GUIContent panel = new GUIContent();
			panel.image = MakeTex((int)background.width, (int)background.height, title.panelColor);
			
			EditorGUI.LabelField(background, panel );
			EditorGUI.LabelField(titlerect, title.titletext, bold);
			EditorGUI.LabelField(contentrect, content, normal);


		}

		private Texture2D MakeTex(int width, int height, Color col)
		{
			Color[] pix = new Color[width * height];
			for (int i = 0; i < pix.Length; ++i)
			{
				pix[i] = col;
			}
			Texture2D result = new Texture2D(width, height);
			result.SetPixels(pix);
			result.Apply();
			return result;
		}
	}
#endif

}

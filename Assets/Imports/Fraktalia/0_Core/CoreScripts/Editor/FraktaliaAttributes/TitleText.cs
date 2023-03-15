using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;

#endif

namespace Fraktalia.Core.FraktaliaAttributes
{
	public enum TitleTextType
	{
		Title,
		H1,
		H2,
		H3
	}

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
	public class TitleTextAttribute : PropertyAttribute
	{
		public string titletext;
		public Color titlecolor;
		public int titlesize;
		public FontStyle fontstyle;


		public TitleTextAttribute(string text, TitleTextType titletype)
		{
			titletext = text;
			switch (titletype)
			{
				case TitleTextType.Title:
					fontstyle = FontStyle.Bold;
					titlecolor = Color.white;
					ColorUtility.TryParseHtmlString("#008000ff", out titlecolor);
					titlesize = 14;
					break;
				case TitleTextType.H1:
					
					titlecolor = Color.black;
					fontstyle = FontStyle.Bold;
					titlesize = 12;
					break;
				case TitleTextType.H2:
					break;
				case TitleTextType.H3:
					fontstyle = FontStyle.Bold;
					titlecolor = Color.black;
					ColorUtility.TryParseHtmlString("#008000ff", out titlecolor);
					titlesize = 12;
					break;
				default:
					break;
			}

			
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(TitleTextAttribute))]
	public class TitleTextDrawer : DecoratorDrawer
	{
		public override float GetHeight()
		{
			TitleTextAttribute title = attribute as TitleTextAttribute;
			return title.titlesize+5+10;
		}
		
		public override void OnGUI(Rect position)
		{
			TitleTextAttribute title = attribute as TitleTextAttribute;

			GUIStyle bold = new GUIStyle();
			bold.fontStyle = title.fontstyle;
			bold.fontSize = title.titlesize;
			bold.richText = true;
			bold.normal.textColor = title.titlecolor;

			position.yMin += 10;

			EditorGUI.LabelField(position, title.titletext, bold);					
		}
	}
#endif

}

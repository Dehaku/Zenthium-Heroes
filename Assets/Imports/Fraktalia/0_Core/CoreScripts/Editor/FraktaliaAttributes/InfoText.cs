using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fraktalia.Core.FraktaliaAttributes
{
	public class InfoContent
	{
		public InfoSection Title;
		public InfoSection[] content;
		public InfoSection section;
		public string videoURL;
		public string videoURL2;
		public string videoURL2Text;
		public string videoURL3;
		public string videoURL3Text;
		public bool showvideo;
	}


	public class BeginInfoAttribute : PropertyAttribute
	{
		public static Dictionary<string, InfoContent> InfoContentDictionary = new Dictionary<string, InfoContent>();
		
		public BeginInfoAttribute(string Key)
		{
			if (InfoContentDictionary == null) InfoContentDictionary = new Dictionary<string, InfoContent>();

			if(!InfoContentDictionary.ContainsKey(Key))
			{
				InfoContentDictionary[Key] = new InfoContent();
				var CurrentInfoContent = InfoContentDictionary[Key];
				CurrentInfoContent.content = new InfoSection[5];
				CurrentInfoContent.videoURL = "";
				CurrentInfoContent.videoURL2 = "";
				CurrentInfoContent.videoURL3 = "";
				CurrentInfoContent.showvideo = false;
			}	

		}

		public static void CreateEntry(string key)
        {
			if (!InfoContentDictionary.ContainsKey(key))
			{
				InfoContentDictionary[key] = new InfoContent();
				var CurrentInfoContent = InfoContentDictionary[key];
				CurrentInfoContent.content = new InfoSection[5];
				CurrentInfoContent.videoURL = "";
				CurrentInfoContent.videoURL2 = "";
				CurrentInfoContent.videoURL3 = "";
				CurrentInfoContent.showvideo = false;
			}
		}
		
	}

	public class InfoTitleAttribute : PropertyAttribute
	{
		

		public InfoTitleAttribute(string title, string text, string key)
		{
			BeginInfoAttribute.CreateEntry(key);

			if (BeginInfoAttribute.InfoContentDictionary.ContainsKey(key))
			{
				var CurrentInfoContent = BeginInfoAttribute.InfoContentDictionary[key];
				if (CurrentInfoContent == null) return;
				CurrentInfoContent.Title = new InfoSection();
				CurrentInfoContent.Title.Title = title;
				CurrentInfoContent.Title.Text = text;
			}
		}
	}

	public class InfoSection1Attribute : PropertyAttribute
	{
		public InfoSection1Attribute(string title, string text, string key)
		{
			BeginInfoAttribute.CreateEntry(key);
			if (BeginInfoAttribute.InfoContentDictionary.ContainsKey(key))
			{
				var CurrentInfoContent = BeginInfoAttribute.InfoContentDictionary[key];
				if (CurrentInfoContent == null) return;

				InfoSection section = new InfoSection();
				section.Title = title;
				section.Text = text;
				CurrentInfoContent.content[0] = (section);
			}
		}
	}
	public class InfoSection2Attribute : PropertyAttribute
	{
		public InfoSection2Attribute(string title, string text, string key)
		{
			BeginInfoAttribute.CreateEntry(key);
			if (BeginInfoAttribute.InfoContentDictionary.ContainsKey(key))
			{
				var CurrentInfoContent = BeginInfoAttribute.InfoContentDictionary[key];
				if (CurrentInfoContent == null) return;
				InfoSection section = new InfoSection();
				section.Title = title;
				section.Text = text;
				CurrentInfoContent.content[1] = (section);
			}
		}
	}
	public class InfoSection3Attribute : PropertyAttribute
	{
		public InfoSection3Attribute(string title, string text, string key)
		{
			BeginInfoAttribute.CreateEntry(key);
			if (BeginInfoAttribute.InfoContentDictionary.ContainsKey(key))
			{
				var CurrentInfoContent = BeginInfoAttribute.InfoContentDictionary[key];
				if (CurrentInfoContent == null) return;
				InfoSection section = new InfoSection();
				section.Title = title;
				section.Text = text;
				CurrentInfoContent.content[2] = (section);
			}
		}
	}
	public class InfoSection4Attribute : PropertyAttribute
	{
		public InfoSection4Attribute(string title, string text, string key)
		{
			BeginInfoAttribute.CreateEntry(key);
			if (BeginInfoAttribute.InfoContentDictionary.ContainsKey(key))
			{
				var CurrentInfoContent = BeginInfoAttribute.InfoContentDictionary[key];
				if (CurrentInfoContent == null) return;
				InfoSection section = new InfoSection();
				section.Title = title;
				section.Text = text;
				CurrentInfoContent.content[3] = (section);
			}
		}
	}
	public class InfoSection5Attribute : PropertyAttribute
	{
		public InfoSection5Attribute(string title, string text, string key)
		{
			BeginInfoAttribute.CreateEntry(key);
			if (BeginInfoAttribute.InfoContentDictionary.ContainsKey(key))
			{
				var CurrentInfoContent = BeginInfoAttribute.InfoContentDictionary[key];
				if (CurrentInfoContent == null) return;
				InfoSection section = new InfoSection();
				section.Title = title;
				section.Text = text;
				CurrentInfoContent.content[4] = (section);
			}
		}
	}

	public class InfoVideoAttribute : PropertyAttribute
	{
		public InfoVideoAttribute(string url, bool showvideo, string key)
		{
			BeginInfoAttribute.CreateEntry(key);
			if (BeginInfoAttribute.InfoContentDictionary.ContainsKey(key))
			{
				var CurrentInfoContent = BeginInfoAttribute.InfoContentDictionary[key];
				if (CurrentInfoContent == null) return;
				CurrentInfoContent.videoURL = url;
				CurrentInfoContent.showvideo = showvideo;
			}
		}
	}

	public class InfoVideo2Attribute : PropertyAttribute
	{
		public InfoVideo2Attribute(string url, string text, string key)
		{
			BeginInfoAttribute.CreateEntry(key);
			if (BeginInfoAttribute.InfoContentDictionary.ContainsKey(key))
			{
				var CurrentInfoContent = BeginInfoAttribute.InfoContentDictionary[key];
				if (CurrentInfoContent == null) return;		
				CurrentInfoContent.videoURL2 = url;
				CurrentInfoContent.videoURL2Text = text;
			}
		}
	}

	public class InfoVideo3Attribute : PropertyAttribute
	{
		public InfoVideo3Attribute(string url, string text, string key)
		{
			BeginInfoAttribute.CreateEntry(key);
			if (BeginInfoAttribute.InfoContentDictionary.ContainsKey(key))
			{
				var CurrentInfoContent = BeginInfoAttribute.InfoContentDictionary[key];
				if (CurrentInfoContent == null) return;			
				CurrentInfoContent.videoURL3 = url;
				CurrentInfoContent.videoURL3Text = text;
			}
		}
	}

	public class InfoTextAttribute : PropertyAttribute
	{
		public string labeltext;
		public string Key;

		public InfoTextAttribute(string labeltext, string key)
		{
			this.labeltext = labeltext;
			this.Key = key;
		}
	}

	public class InfoTextKeyAttribute : PropertyAttribute
	{
		public string labeltext;
		public string Key;

		public InfoTextKeyAttribute(string labeltext, string key)
		{
			this.labeltext = labeltext;
			this.Key = key;
		}
	}

	public class InfoSection
	{
		public string Title;
		public string Text;
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(InfoTextAttribute))]
	public class InfoTextDrawer : DecoratorDrawer
	{


		public override float GetHeight()
		{
			
			return base.GetHeight() + 20;

		}

		// Draw the property inside the given rect
		public override void OnGUI(Rect position)
		{
			// First get the attribute since it contains the range for the slider
			InfoTextAttribute labeltext = attribute as InfoTextAttribute;
			
			GUIStyle style = new GUIStyle();
			style.alignment = TextAnchor.MiddleLeft;
			style.fontStyle = FontStyle.Bold;
			style.fontSize = 14;

			Rect pos = position;

			pos.xMax = position.xMax;
			pos.xMin = position.xMax - 65;
			pos.yMin = position.yMin + 5;
			pos.yMax = position.yMax - 5;

			if (BeginInfoAttribute.InfoContentDictionary == null) return;
			if (!BeginInfoAttribute.InfoContentDictionary.ContainsKey(labeltext.Key)) return;
			if (GUI.Button(pos, "Info/Help"))
			{
				InfoContent tutorial = BeginInfoAttribute.InfoContentDictionary[labeltext.Key];
				TutorialWindow.Init(tutorial);
			}

		
			EditorGUI.TextField(position, labeltext.labeltext, style);

			

			

		}
	}

#endif


}


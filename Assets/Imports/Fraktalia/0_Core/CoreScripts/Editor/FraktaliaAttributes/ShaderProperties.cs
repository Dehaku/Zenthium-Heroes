#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace Fraktalia.Core.FraktaliaAttributes
{
	public class TitleDecorator : MaterialPropertyDrawer
	{
		string Title = "";
		public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
		{
			return 25;
		}

		public TitleDecorator(string title)
		{
			this.Title = title;
		}

		public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
		{
			GUIStyle bold = new GUIStyle();
			bold.fontStyle = FontStyle.Bold; 
			bold.fontSize = 18;
			bold.richText = true;
			Color titlecolor = new Color();
			ColorUtility.TryParseHtmlString("#008000ff", out titlecolor);
			bold.normal.textColor = titlecolor;

			

			EditorGUI.LabelField(position, Title, bold);
			
		}
	}

	public class SingleLineDrawer : MaterialPropertyDrawer
	{
		string extraproperty;
		string KeyWord;

		public SingleLineDrawer()
		{
			
			extraproperty = "";
			
		}

		public SingleLineDrawer(string extraproperty)
		{
			this.extraproperty = extraproperty;	
		}

		public SingleLineDrawer(string extraproperty, string keyword)
		{
			this.extraproperty = extraproperty;
			this.KeyWord = keyword;
		}


		public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
		{
			Material mat = (Material)prop.targets[0];
			if (KeyWord != null)
			{
				bool state = mat.IsKeywordEnabled(KeyWord);
				if (!state)
				{
					return 0;
				}
			}
			return base.GetPropertyHeight(prop, label, editor);
		}


		public override void OnGUI(Rect position, MaterialProperty prop, String label, MaterialEditor editor)
		{
			Dictionary<string, int> properties = new Dictionary<string, int>();
			Dictionary<string, ShaderUtil.ShaderPropertyType> types = new Dictionary<string, ShaderUtil.ShaderPropertyType>();


			Material mat = (Material)prop.targets[0];
			if (KeyWord != null)
			{
				bool state = mat.IsKeywordEnabled(KeyWord);
				if (!state)
				{
					return;
				}
			}

			Shader shader = mat.shader;

			int count = ShaderUtil.GetPropertyCount(shader);

			for (int i = 0; i < count; i++)
			{				
				string name = ShaderUtil.GetPropertyName(shader, i);
				ShaderUtil.ShaderPropertyType type = ShaderUtil.GetPropertyType(shader, i);

				types[name] = type;
				properties[name] = i;
			}


			editor.TexturePropertyMiniThumbnail(position, prop, label, "");
			
			if (properties.ContainsKey(extraproperty))
			{
				ShaderUtil.ShaderPropertyType typ = types[extraproperty];

				Rect pos;
				float value;
				switch (typ)
				{
					case ShaderUtil.ShaderPropertyType.Color:
						var color = mat.GetColor(extraproperty);

						pos = position;

						pos.xMax = position.xMax;
						pos.xMin = pos.xMax - 65;
						color = EditorGUI.ColorField(pos, color);

						mat.SetColor(extraproperty, color);

						break;
					case ShaderUtil.ShaderPropertyType.Vector:
						break;
					case ShaderUtil.ShaderPropertyType.Float:
						value = mat.GetFloat(extraproperty);
						
						pos = position;

						
						
						value = EditorGUI.FloatField(pos," ", value);

						mat.SetFloat(extraproperty, value);
						

						break;
					case ShaderUtil.ShaderPropertyType.Range:
						value = mat.GetFloat(extraproperty);
						float min = ShaderUtil.GetRangeLimits(shader, properties[extraproperty], 1);
						float max = ShaderUtil.GetRangeLimits(shader, properties[extraproperty], 2);
						pos = position;

						pos.xMax = position.xMax;
						pos.xMin = pos.xMax - pos.width * 0.52f;
						value = EditorGUI.Slider(pos, value, min, max);

						mat.SetFloat(extraproperty, value);

						

						break;
					case ShaderUtil.ShaderPropertyType.TexEnv:
						break;
					default:
						break;
				}



			
			}
			
			
			

			
		}
	}

	public class MultiCompileOptionDrawer : MaterialPropertyDrawer
	{
		string KeyWord;
		string Activeonkeyword = "";
		
		public MultiCompileOptionDrawer(string keyWord)
		{
			this.KeyWord = keyWord;
		}

		public MultiCompileOptionDrawer(string keyWord, string activeonkeyword)
		{
			this.KeyWord = keyWord;
			Activeonkeyword = activeonkeyword;
		}


		public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
		{
			if(Activeonkeyword != "")
			{
				Material mat = (Material)prop.targets[0];
				Shader shader = mat.shader;			
				bool state = mat.IsKeywordEnabled(Activeonkeyword);
				if (!state) return 0;
			}

			return base.GetPropertyHeight(prop, label, editor) + 20;
		}

		public override void OnGUI(Rect position, MaterialProperty prop, String label, MaterialEditor editor)
		{


			Material mat = (Material)prop.targets[0];
			Shader shader = mat.shader;

			if (Activeonkeyword != "")
			{
				if (!mat.IsKeywordEnabled(Activeonkeyword)) return;
			}


			string labeltext = KeyWord.Replace("_", " ");
			bool state = mat.IsKeywordEnabled(KeyWord);
			GUIStyle style = new GUIStyle();
			TextAnchor anchor = style.alignment;
			anchor = TextAnchor.MiddleLeft;
			style.alignment = anchor;
			style.fontSize = 14;
			style.fontStyle = FontStyle.Bold;
			GUIContent content = new GUIContent(labeltext);

			position.yMin += 8;
			
			EditorGUI.LabelField(position, content, style);
			Rect pos = position;

			pos.xMax = position.xMax;
			pos.xMin = pos.xMax - 65;
			pos.yMin += 2;
			pos.yMax -= 2;



			var color = GUI.backgroundColor;
			if (state)
			{
				GUI.backgroundColor = Color.yellow;
			}
			if (GUI.Button(pos, "ON"))
			{
				mat.EnableKeyword(KeyWord);
			}

			GUI.backgroundColor = color;
			pos.x -= pos.width;
			if (!state)
			{
				GUI.backgroundColor = Color.yellow;
			}
			if (GUI.Button(pos, "OFF"))
			{
				mat.DisableKeyword(KeyWord);
			}

			GUI.backgroundColor = color;
		}
	}

	public class MultiCompileToggleDrawer : MaterialPropertyDrawer
	{
		string KeyWord;
		string Activeonkeyword = "";

		public MultiCompileToggleDrawer(string keyWord)
		{
			this.KeyWord = keyWord;
		}

		public MultiCompileToggleDrawer(string keyWord, string activeonkeyword)
		{
			this.KeyWord = keyWord;
			Activeonkeyword = activeonkeyword;
		}


		public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
		{
			if (Activeonkeyword != "")
			{
				Material mat = (Material)prop.targets[0];
				Shader shader = mat.shader;
				bool state = mat.IsKeywordEnabled(Activeonkeyword);
				if (!state) return 0;
			}

			return base.GetPropertyHeight(prop, label, editor);
		}

		public override void OnGUI(Rect position, MaterialProperty prop, String label, MaterialEditor editor)
		{


			Material mat = (Material)prop.targets[0];
			Shader shader = mat.shader;

			if (Activeonkeyword != "")
			{
				if (!mat.IsKeywordEnabled(Activeonkeyword)) return;
			}


			string labeltext = KeyWord.Replace("_", " ");
			bool state = mat.IsKeywordEnabled(KeyWord);
			GUIStyle style = new GUIStyle();
			TextAnchor anchor = style.alignment;
			anchor = TextAnchor.MiddleLeft;
			style.alignment = anchor;
			style.fontSize = 14;

			state = EditorGUI.Toggle(position, labeltext, state);
			GUIContent content = new GUIContent(labeltext);
				
			if (state)
			{ 
				mat.EnableKeyword(KeyWord);
			}
	
			if (!state)
			{		
				mat.DisableKeyword(KeyWord);
			}		
		}
	}


	public class KeywordDependentDrawer : MaterialPropertyDrawer
	{
		string KeyWord;
		

		public KeywordDependentDrawer(string keyWord)
		{
			this.KeyWord = keyWord;		
		}

		public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
		{
			Material mat = (Material)prop.targets[0];
			Shader shader = mat.shader;

			string labeltext = KeyWord.Replace("_", " ");
			bool state = mat.IsKeywordEnabled(KeyWord);

			if (state)
			{
                if (prop.type == MaterialProperty.PropType.Texture)
                {
					return base.GetPropertyHeight(prop, label, editor) + 50;
                }
				
				return base.GetPropertyHeight(prop, label, editor);
			
			
			}
			return 0;
		}

		public override void OnGUI(Rect position, MaterialProperty prop, String label, MaterialEditor editor)
		{


			Material mat = (Material)prop.targets[0];
			Shader shader = mat.shader;

			string labeltext = KeyWord.Replace("_", " ");
			bool state = mat.IsKeywordEnabled(KeyWord);
			if(state)
			{
				editor.DefaultShaderProperty(position, prop, label);
			}

		}
	}

}
#endif

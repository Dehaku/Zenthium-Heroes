using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;



namespace Fraktalia.Core.FraktaliaAttributes
{
	public static class FraktaliaEditorStyles
    {
        private static GUIStyle foldout;
        public static GUIStyle FoldOut
        {
            get
            {
                if (foldout != null) return foldout;

               

                foldout = new GUIStyle(EditorStyles.toggle);
                foldout.fontStyle = FontStyle.Bold;
                foldout.fontSize = 16;

               
        
                int slicing = 20;
                foldout.border.left = slicing;
                foldout.border.right = slicing;
                foldout.border.top = 5;
                foldout.border.bottom = 5;

                foldout.overflow = new RectOffset(-10, 0, 3, 0);
                foldout.padding = new RectOffset(25, -150, 0, 0);

                foldout.onNormal.textColor = new Color32(0,0,0,255);
                foldout.normal.textColor = new Color32(0, 0, 0, 255);
               



                return foldout;
            }
        }

        private static GUIStyle box;
        public static GUIStyle Box
        {
            get
            {
                if (box != null) return box;
                Texture2D texture = Resources.Load<Texture2D>("button");
                box = new GUIStyle(GUI.skin.box);
                box.padding = new RectOffset(20, 0, 5, 5);
                box.normal.background = texture;

                int slicing = 20;
                box.border.left = slicing;
                box.border.right = slicing;
                box.border.top = 5;
                box.border.bottom = 5;

                return box;
            }
        }

        private static GUIStyle button;
        public static GUIStyle Button
        {
            get
            {
                if (button != null) return button;
                Texture2D texture = Resources.Load<Texture2D>("button");
                button = new GUIStyle(GUI.skin.box);
                button.padding = new RectOffset(20, 0, 5, 5);
                button.normal.background = texture;

                int slicing = 20;
                button.border.left = slicing;
                button.border.right = slicing;
                button.border.top = 5;
                button.border.bottom = 5;

                return button;
            }
        }

        private static GUIStyle boxContent;
        public static GUIStyle BoxContent
        {
            get
            {
                if (boxContent != null) return boxContent;              
                boxContent = new GUIStyle(GUI.skin.box);
                if (EditorGUIUtility.isProSkin)
                {
                    Texture2D texture = Resources.Load<Texture2D>("panel_dark");
                    boxContent.normal.background = texture;
                }
                else
                {
                    Texture2D texture = Resources.Load<Texture2D>("panel");
                    boxContent.normal.background = texture;
                }

                
                int slicing = 20;
                boxContent.border.left = slicing;
                boxContent.border.right = slicing;
                boxContent.border.top = slicing;
                boxContent.border.bottom = slicing;

                return boxContent;
            }
        }


        private static GUIStyle box_disabled;
        public static GUIStyle Box_Disabled
        {
            get
            {
                if (box_disabled != null) return box_disabled;
                Texture2D texture = Resources.Load<Texture2D>("button_off");
                box_disabled = new GUIStyle(GUI.skin.box);
                box_disabled.padding = new RectOffset(20, 0, 5, 5);
                box_disabled.normal.background = texture;

                int slicing = 20;
                box_disabled.border.left = slicing;
                box_disabled.border.right = slicing;
                box_disabled.border.top = 5;
                box_disabled.border.bottom = 5;

                return box_disabled;
            }
        }

        public static GUIStyle GetFoldoutBox(bool state)
        {
            if (state)
            {
                return Box;
            }
            else return Box_Disabled;
        }

        public static GUIStyle Title
        {
            get
            {
                GUIStyle title = new GUIStyle();
                title.fontStyle = FontStyle.Bold;
                title.fontSize = 16;
                title.richText = true;
                title.alignment = TextAnchor.MiddleLeft;
                return title;
            }
        }

        public static bool InfoTitle(Rect position, string infotext)
        {
            Rect labelRect = position;
            labelRect.xMin += 10;

            EditorGUI.LabelField(labelRect, infotext, Title);

            Rect infobuttonRect = position;
            infobuttonRect.xMax = position.xMax - 10;
            infobuttonRect.xMin = infobuttonRect.xMax - 64;
            infobuttonRect.height = infobuttonRect.height - 20;
            infobuttonRect.y = position.y + position.height / 2 - infobuttonRect.height / 2;

            return (GUI.Button(infobuttonRect, "Info")); 
        }

    }
}
#endif
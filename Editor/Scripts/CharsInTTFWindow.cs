#if UNITY_2018_3_OR_NEWER && !UNITY_2019_1_OR_NEWER
#define TMP_1_4_0_OR_NEWER
#endif

#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace KyubEditor.EmojiSearch
{
    public class CharsInTTFWindow : EditorWindow
    {
        #region Private Variables

        [SerializeField]
        string m_charsRange = "";
        [SerializeField]
        Font m_font = null;

        Vector2 _scroll = Vector2.zero;

        #endregion

        #region Static Init

        [MenuItem("Window/TextMeshPro/Chars in Font")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            CharsInTTFWindow window = (CharsInTTFWindow)EditorWindow.GetWindow(typeof(CharsInTTFWindow));
            window.titleContent = new GUIContent("Chars in Font");
            window.ShowUtility();
        }

        #endregion

        #region Unity Functions

        protected virtual void OnGUI()
        {
            if (m_font != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Recalculate", GUILayout.Width(150)))
                    {
                        m_charsRange = PickAllCharsRangeFromFont(m_font);
                    }
                }
            }

            EditorGUILayout.Space();
            var newFont = EditorGUILayout.ObjectField("Source Font File", m_font, typeof(Font), false) as Font;
            if (m_font != newFont)
            {
                m_font = newFont;
                m_charsRange = PickAllCharsRangeFromFont(m_font);
            }
            EditorGUILayout.Space();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("Character Sequence (Decimal)", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(m_charsRange);
            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Helper Static Functions

        public static string PickAllCharsRangeFromFont(Font font)
        {
            string charsRange = "";
            if (font != null)
            {
                TrueTypeFontImporter fontReimporter = null;

                //A GLITCH: Unity's Font.CharacterInfo doesn't work
                //properly on dynamic mode, we need to change it to Unicode first
                if (font.dynamic)
                {
                    var assetPath = AssetDatabase.GetAssetPath(font);
                    fontReimporter = (TrueTypeFontImporter)AssetImporter.GetAtPath(assetPath);

                    fontReimporter.fontTextureCase = FontTextureCase.Unicode;
                    fontReimporter.SaveAndReimport();
                }

                //Only Non-Dynamic Fonts define the characterInfo array
                Vector2Int minMaxRange = new Vector2Int(-1, -1);
                for (int i = 0; i < font.characterInfo.Length; i++)
                {
                    var charInfo = font.characterInfo[i];
                    var apply = true;
                    if (minMaxRange.x < 0 || minMaxRange.y < 0)
                    {
                        apply = false;
                        minMaxRange = new Vector2Int(charInfo.index, charInfo.index);
                    }
                    else if (charInfo.index == minMaxRange.y + 1)
                    {
                        apply = false;
                        minMaxRange.y = charInfo.index;
                    }

                    if (apply || i == font.characterInfo.Length - 1)
                    {
                        if (!string.IsNullOrEmpty(charsRange))
                            charsRange += "\n,";
                        charsRange += minMaxRange.x + "-" + minMaxRange.y;

                        if (i == font.characterInfo.Length - 1)
                        {
                            if (charInfo.index >= 0 && (charInfo.index  < minMaxRange.x || charInfo.index > minMaxRange.y))
                                charsRange += "\n," + charInfo.index + "-" + charInfo.index;
                        }
                        else
                            minMaxRange = new Vector2Int(charInfo.index, charInfo.index);

                    }
                }

                // Change back to dynamic font
                if (fontReimporter != null)
                {
                    fontReimporter.fontTextureCase = FontTextureCase.Dynamic;
                    fontReimporter.SaveAndReimport();
                }
            }
            return charsRange;
        }

        #endregion
    }
}

#endif

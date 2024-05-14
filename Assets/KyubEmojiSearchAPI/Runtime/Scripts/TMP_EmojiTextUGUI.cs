#if (TMP_2_1_4_OR_NEWER && !TMP_3_0_0_OR_NEWER) || TMP_3_0_4_OR_NEWER
#define TMP_NEW_PREPROCESSOR
#endif

#if UNITY_2018_3_OR_NEWER && !UNITY_2019_1_OR_NEWER
#define TMP_1_4_0_OR_NEWER
#endif


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Kyub.EmojiSearch.Utilities;

namespace Kyub.EmojiSearch.UI
{
#if TMP_NEW_PREPROCESSOR
    public class TMP_EmojiTextUGUI : TextMeshProUGUI, ITextPreprocessor
#else
    public class TMP_EmojiTextUGUI : TextMeshProUGUI
#endif
    {
        #region Private Fields

        [SerializeField, Tooltip("Force monospace character with (value)em.\nRequire RichText active")]
        protected float m_monospaceDistEm = 0;

        protected bool _emojiParsingRequired = true;

#if TMP_NEW_PREPROCESSOR
        [SerializeField]
        protected ITextPreprocessor m_SecondaryPreprocessor;
#endif

        #endregion

        #region Properties

#if TMP_NEW_PREPROCESSOR
        public new ITextPreprocessor textPreprocessor
        {
            get { return m_SecondaryPreprocessor; }
            set
            {
                m_SecondaryPreprocessor = value;
                InitTextPreProcessor();
            }
        }
#endif

        public float MonospaceDistEm
        {
            get
            {
                return m_monospaceDistEm;
            }
            set
            {
                if (m_monospaceDistEm == value)
                    return;
                m_monospaceDistEm = value;
                SetVerticesDirty();
            }
        }

#if TMP_1_4_0_OR_NEWER
        protected System.Reflection.FieldInfo _isInputParsingRequired_Field = null;
#endif
        protected internal bool IsInputParsingRequired_Internal
        {
            get
            {
#if TMP_1_4_0_OR_NEWER
                if (_isInputParsingRequired_Field == null)
                    _isInputParsingRequired_Field = typeof(TMP_Text).GetField("m_isInputParsingRequired", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (_isInputParsingRequired_Field != null)
                    return (bool)_isInputParsingRequired_Field.GetValue(this);
                else
                    return false;
#else
                return m_isInputParsingRequired;
#endif
            }
            protected set
            {
#if TMP_1_4_0_OR_NEWER
                if (_isInputParsingRequired_Field == null)
                    _isInputParsingRequired_Field = typeof(TMP_Text).GetField("m_isInputParsingRequired", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (_isInputParsingRequired_Field != null)
                    _isInputParsingRequired_Field.SetValue(this, value);
#else
                m_isInputParsingRequired = value;
#endif
            }
        }

#if TMP_1_4_0_OR_NEWER
        protected enum TextInputSources { Text = 0, SetText = 1, SetCharArray = 2, String = 3 };
        protected System.Reflection.FieldInfo _inputSource_Field = null;
        protected System.Type _textInputSources_Type = null;
#endif
        protected TextInputSources InputSource_Internal
        {
            get
            {
#if TMP_1_4_0_OR_NEWER
                if (_inputSource_Field == null)
                    _inputSource_Field = typeof(TMP_Text).GetField("m_inputSource", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (_inputSource_Field != null)
                    return (TextInputSources)System.Enum.ToObject(typeof(TextInputSources), (int)_inputSource_Field.GetValue(this));
                else
                    return TextInputSources.Text;
#else
                return m_inputSource;
#endif
            }
            set
            {
#if TMP_1_4_0_OR_NEWER

                if (_inputSource_Field == null)
                    _inputSource_Field = typeof(TMP_Text).GetField("m_inputSource", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (_inputSource_Field != null)
                {
                    //Pick the Type of internal enum to set back in TMP_Text
                    if (_textInputSources_Type == null)
                        _textInputSources_Type = typeof(TMP_Text).GetNestedType("TextInputSources", System.Reflection.BindingFlags.NonPublic);
                    if (_textInputSources_Type != null)
                    {
                        _inputSource_Field.SetValue(this, System.Enum.ToObject(_textInputSources_Type, (int)value));
                    }
                }

#else
                m_inputSource = value;
#endif
            }
        }

        #endregion

        #region Unity Functions

        protected override void Awake()
        {
            InitTextPreProcessor();
            base.Awake();
        }

        protected override void OnEnable()
        {
            InitTextPreProcessor();
            base.OnEnable();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            InitTextPreProcessor();
            base.OnValidate();
        }
#endif

        #endregion

        #region Emoji Parser Functions

        protected virtual bool ParseInputTextAndEmojiCharSequence()
        {
            _emojiParsingRequired = false;

            if (IsUsingNewPreprocessor())
                return false;

            //Only parse when richtext active (we need the <sprite=index> tag)
            if (m_isRichText)
            {
                var oldText = m_text;

                var parsedEmoji = PreprocessText(m_text, out m_text, true);
                _emojiParsingRequired = false;
                IsInputParsingRequired_Internal = false;
                InputSource_Internal = TextInputSources.Text;

                ParseInputText();

                _emojiParsingRequired = false;
                IsInputParsingRequired_Internal = false;

                //Debug.Log("ParseInputTextAndEmojiCharSequence End");
                //We must revert the original text because we dont want to permanently change the text
                m_text = oldText;

#if !TMP_2_1_0_PREVIEW_10_OR_NEWER
                m_isCalculateSizeRequired = true;
#endif

                return parsedEmoji;
            }

            return false;
        }

        protected virtual bool ApplyMonoSpacingValues(string text, out string outText)
        {
            bool sucess = false;

            if (m_monospaceDistEm != 0)
            {
                outText = "<mspace=" + m_monospaceDistEm.ToString(System.Globalization.CultureInfo.InvariantCulture) + "em>" + text;
                sucess = true;
            }
            else
                outText = text;

            return sucess;
        }

        #endregion

        #region New TMP PreProcessor Functions

        protected bool IsUsingNewPreprocessor()
        {
#if TMP_NEW_PREPROCESSOR
            InitTextPreProcessor();
            //Only Apply this function when new preprocessor is not active
            if (m_TextPreprocessor as Object == this)
                return true;
#endif
            return false;
        }

        protected virtual void InitTextPreProcessor()
        {
#if TMP_NEW_PREPROCESSOR
            if (m_SecondaryPreprocessor as Object == this)
                m_SecondaryPreprocessor = null;

            if (m_TextPreprocessor as Object != this)
            {
                if (m_TextPreprocessor != null)
                    m_SecondaryPreprocessor = m_TextPreprocessor;
                m_TextPreprocessor = this;
            }
#endif
        }

        //New PreProcessor Implementation
        public virtual string PreprocessText(string text)
        {
            PreprocessText(text, out text, false);
            return text;
        }

        public virtual bool PreprocessText(string text, out string parsedString, bool forceApply)
        {
            bool success = false;
            if (forceApply || m_isRichText)
            {
                success = TMP_EmojiSearchEngine.ParseEmojiCharSequence(spriteAsset, ref text);

                if (m_monospaceDistEm != 0)
                    ApplyMonoSpacingValues(text, out text);
            }

#if TMP_NEW_PREPROCESSOR
            if (m_SecondaryPreprocessor != null)
                text = m_SecondaryPreprocessor.PreprocessText(text);

            //TODO: Find a better option
            //TMP drop support to automatic convert \\n into \n
            if (m_parseCtrlCharacters)
            {
                text = text.Replace("\\n", "\n").Replace("\\t", "\t");
            }
#endif
            parsedString = text;

            return success;
        }

        #endregion

        #region Text Overriden Functions

        public override void SetVerticesDirty()
        {
            //In textmeshpro 1.4 the parameter "m_isInputParsingRequired" changed to internal, so, to dont use reflection i changed to "m_havePropertiesChanged" parameter
            if (IsInputParsingRequired_Internal)
            {
                _emojiParsingRequired = m_isRichText;
            }
            base.SetVerticesDirty();
        }

        public override void Rebuild(CanvasUpdate update)
        {
            if (this == null && enabled && gameObject.activeInHierarchy) return;

            if (_emojiParsingRequired)
                ParseInputTextAndEmojiCharSequence();

            base.Rebuild(update);
        }

        public override string GetParsedText()
        {
            if (_emojiParsingRequired)
                ParseInputTextAndEmojiCharSequence();

            return base.GetParsedText();
        }

        public override TMP_TextInfo GetTextInfo(string text)
        {
            TMP_EmojiSearchEngine.ParseEmojiCharSequence(spriteAsset, ref text);
            return base.GetTextInfo(text);
        }

#if (TMP_2_2_0_PREVIEW_1_OR_NEWER && !TMP_3_0_0_OR_NEWER) || TMP_3_2_0_PREVIEW_1_OR_NEWER
        protected override Vector2 CalculatePreferredValues(ref float fontSize, Vector2 marginSize, bool isTextAutoSizingEnabled, TextWrappingModes textWrapMode)
        {
            if (_emojiParsingRequired)
                ParseInputTextAndEmojiCharSequence();

            return base.CalculatePreferredValues(ref fontSize, marginSize, isTextAutoSizingEnabled, textWrapMode);
        }

#elif TMP_2_1_0_PREVIEW_8_OR_NEWER
        protected override Vector2 CalculatePreferredValues(ref float defaultFontSize, Vector2 marginSize, bool ignoreTextAutoSizing, bool isWordWrappingEnabled)
        {
            if (_emojiParsingRequired)
                ParseInputTextAndEmojiCharSequence();

            return base.CalculatePreferredValues(ref defaultFontSize, marginSize, ignoreTextAutoSizing, isWordWrappingEnabled);
        }
#elif TMP_2_1_0_PREVIEW_3_OR_NEWER
        protected override Vector2 CalculatePreferredValues(float defaultFontSize, Vector2 marginSize, bool ignoreTextAutoSizing, bool isWordWrappingEnabled)
        {
            if (_emojiParsingRequired)
                ParseInputTextAndEmojiCharSequence();

            return base.CalculatePreferredValues(defaultFontSize, marginSize, ignoreTextAutoSizing, isWordWrappingEnabled);
        }
#else
        protected override Vector2 CalculatePreferredValues(float defaultFontSize, Vector2 marginSize, bool ignoreTextAutoSizing)
        {
            if (_emojiParsingRequired)
                ParseInputTextAndEmojiCharSequence();

            return base.CalculatePreferredValues(defaultFontSize, marginSize, ignoreTextAutoSizing);
        }
#endif

        #endregion

        #region Layout Overriden Functions

#if (TMP_2_1_3_OR_NEWER && !TMP_3_0_0_OR_NEWER) || TMP_3_0_3_OR_NEWER
        public override float preferredWidth { get { m_preferredWidth = GetPreferredWidth(); return m_preferredWidth; } }
        public override float preferredHeight { get { m_preferredHeight = GetPreferredHeight(); return m_preferredHeight; } }

        //NOTE: In Version 2.1.3 or newer we detected an incorrect behaviour in GetPreferredWidth() and GetPreferredHeight() Logic.
        //Fixed this logic creating a "new" version of this funcion that override old behaviour to the correct one
        protected new float GetPreferredWidth()
        {
            if (IsUsingNewPreprocessor())
                return base.GetPreferredWidth();

            if (TMP_Settings.instance == null) return 0;

            // Return cached preferred height if already computed
            if (!m_isPreferredWidthDirty)
                return m_preferredWidth;

            float fontSize = m_enableAutoSizing ? m_fontSizeMax : m_fontSize;

            // Reset auto sizing point size bounds
            m_minFontSize = m_fontSizeMin;
            m_maxFontSize = m_fontSizeMax;
            m_charWidthAdjDelta = 0;

            // Set Margins to Infinity
            Vector2 margin = k_LargePositiveVector2;

            //if (IsInputParsingRequired_Internal || m_isTextTruncated)
            //{
            m_isCalculatingPreferredValues = true;
            ParseInputText();

            _emojiParsingRequired = m_isRichText;
            if (_emojiParsingRequired)
                ParseInputTextAndEmojiCharSequence();
            //}

            m_AutoSizeIterationCount = 0;
#if (TMP_2_2_0_PREVIEW_1_OR_NEWER && !TMP_3_0_0_OR_NEWER) || TMP_3_2_0_PREVIEW_1_OR_NEWER
            TextWrappingModes wrapMode = m_TextWrappingMode == TextWrappingModes.Normal || m_TextWrappingMode == TextWrappingModes.NoWrap ? TextWrappingModes.NoWrap : TextWrappingModes.PreserveWhitespaceNoWrap;
            float preferredWidth = CalculatePreferredValues(ref fontSize, margin, false, wrapMode).x;
#else
            float preferredWidth = CalculatePreferredValues(ref fontSize, margin, false, false).x;
#endif


            m_isPreferredWidthDirty = false;

            //Debug.Log("GetPreferredWidth() called on Object ID: " + GetInstanceID() + " on frame: " + Time.frameCount + ". Returning width of " + preferredWidth);

            return preferredWidth;
        }

        protected new float GetPreferredHeight()
        {
            if (IsUsingNewPreprocessor())
                return base.GetPreferredHeight();

            if (TMP_Settings.instance == null) return 0;

            // Return cached preferred height if already computed
            if (!m_isPreferredHeightDirty)
                return m_preferredHeight;

            float fontSize = m_enableAutoSizing ? m_fontSizeMax : m_fontSize;

            // Reset auto sizing point size bounds
            m_minFontSize = m_fontSizeMin;
            m_maxFontSize = m_fontSizeMax;
            m_charWidthAdjDelta = 0;

            Vector2 margin = new Vector2(m_marginWidth != 0 ? m_marginWidth : k_LargePositiveFloat, k_LargePositiveFloat);

            //if (IsInputParsingRequired_Internal || m_isTextTruncated)
            //{
            m_isCalculatingPreferredValues = true;
            ParseInputText();

            _emojiParsingRequired = m_isRichText;
            if (_emojiParsingRequired)
                ParseInputTextAndEmojiCharSequence();
            //}

            // Reset Text Auto Size iteration tracking.
            m_IsAutoSizePointSizeSet = false;
            m_AutoSizeIterationCount = 0;

            // The CalculatePreferredValues function is potentially called repeatedly when text auto size is enabled.
            // This is a revised implementation to remove the use of recursion which could potentially result in stack overflow issues.
            float preferredHeight = 0;

            while (m_IsAutoSizePointSizeSet == false)
            {
#if (TMP_2_2_0_PREVIEW_1_OR_NEWER && !TMP_3_0_0_OR_NEWER) || TMP_3_2_0_PREVIEW_1_OR_NEWER
                preferredHeight = CalculatePreferredValues(ref fontSize, margin, m_enableAutoSizing, m_TextWrappingMode).y;
#else
                preferredHeight = CalculatePreferredValues(ref fontSize, margin, m_enableAutoSizing, m_enableWordWrapping).y;
#endif
                m_AutoSizeIterationCount += 1;
            }

            m_isPreferredHeightDirty = false;

            //Debug.Log("GetPreferredHeight() called on Object ID: " + GetInstanceID() + " on frame: " + Time.frameCount +". Returning height of " + preferredHeight);

            return preferredHeight;
        }
#endif

        #endregion
    }
}

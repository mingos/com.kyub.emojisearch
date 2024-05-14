#if UNITY_2018_3_OR_NEWER
#define TMP_1_4_0_OR_NEWER
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Kyub.EmojiSearch.Utilities
{
    public static class TMP_EmojiSearchEngine
    {
        #region Static Fields (Lookup Tables per SpriteAsset)

        //Dictionary<string, string> is TableSquence to Sprite Name
        static Dictionary<TMP_SpriteAsset, Dictionary<string, string>> s_lookupTableSequences = new Dictionary<TMP_SpriteAsset, Dictionary<string, string>>();
        static Dictionary<TMP_SpriteAsset, HashSet<string>> s_fastLookupPath = new Dictionary<TMP_SpriteAsset, HashSet<string>>(); //this will cache will save the path of each character in sequence, so for every iteration we can check if we need to continue

        #endregion

        #region Emoji Search Engine Functions

        /// <summary>
        /// Try parse text converting to supported EmojiSequence format (all char sequences will be replaced to <sprite=index>)
        /// </summary>
        /// <param name="spriteAsset"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool ParseEmojiCharSequence(TMP_SpriteAsset spriteAsset, ref string text)
        {
            bool changed = false;
            TryUpdateSequenceLookupTable(spriteAsset);
            if (!string.IsNullOrEmpty(text))
            {
                var mainSpriteAsset = spriteAsset == null ? TMPro.TMP_Settings.defaultSpriteAsset : spriteAsset;
                var fastLookupPath = mainSpriteAsset != null && s_fastLookupPath.ContainsKey(mainSpriteAsset) ? s_fastLookupPath[mainSpriteAsset] : new HashSet<string>();
                var lookupTableSequences = mainSpriteAsset != null && s_lookupTableSequences.ContainsKey(mainSpriteAsset) ? s_lookupTableSequences[mainSpriteAsset] : new Dictionary<string, string>();

                if (lookupTableSequences == null || lookupTableSequences.Count == 0)
                    return false;

                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                //Eficient way to check characters
                for (int i = 0; i < text.Length; i++)
                {
                    int endCounter = i;
                    System.Text.StringBuilder auxSequence = new System.Text.StringBuilder();

                    //Look for sequences in fastLookupPath
                    while (text.Length > endCounter &&
                           (endCounter == i ||
                           fastLookupPath.Contains(auxSequence.ToString()))
                          )
                    {
                        //Try Split \U and \u string and convert to UTF16 or UTF32 character
                        string unicode;
                        var unicodeEndIndex = GetUnicodeChar(ref text, endCounter, out unicode);
                        if (unicodeEndIndex >= 0 && !string.IsNullOrEmpty(unicode))
                        {
                            var failed = false;
                            for (int j = 0; j < unicode.Length; j++)
                            {
                                auxSequence.Append(unicode[j]);
                                //Cancel Append (When we found an UTF32 Character, we can add multiple inputs in same loop, so we cancel it to keep internal logic)
                                if (!fastLookupPath.Contains(auxSequence.ToString()))
                                {
                                    failed = true;
                                    break;
                                }
                            }
                            if(!failed)
                                endCounter = unicodeEndIndex;
                        }
                        else
                        {
                            //We must skip variant selectors (if found it)
                            auxSequence.Append(text[endCounter]);
                        }
                        endCounter++;
                    }

                    //Remove last added guy (the previous one is the correct)
                    if (auxSequence.Length > 0 && !fastLookupPath.Contains(auxSequence.ToString()))
                    {
                        auxSequence.Remove(auxSequence.Length - 1, 1);
                        endCounter--;
                    }

                    var sequence = auxSequence.Length > 0 ? auxSequence.ToString() : "";
                    //Found a sequence, add it instead add the character
                    if (sequence.Length > 0 && lookupTableSequences.ContainsKey(sequence))
                    {
                        changed = true;
                        //Changed Index to Sprite Name to prevent erros when looking at fallbacks
                        sb.Append(string.Format("<sprite name=\"{0}\">", lookupTableSequences[sequence]));

                        int deltaChecked = (endCounter - i - 1); //jump checked characters
                        if (deltaChecked > 0)
                            i += deltaChecked;
                    }
                    //add the char (normal character)
                    else
                    {
                        sb.Append(text[i]);
                    }
                }

                if (changed)
                    text = sb.ToString();
            }

            return changed;
        }

        /// <summary>
        /// Return final index of Unicode in String and output the Unicode String
        /// </summary>
        /// <param name="text"></param>
        /// <param name="index"></param>
        /// <param name="unicode"></param>
        /// <returns></returns>
        public static int GetUnicodeChar(ref string text, int index, out string unicode)
        {
            if (text == null || index >= text.Length || index < 0)
            {
                unicode = null;
                return -1;
            }

            var length = text.Length;
            var c = text[index];
            if (c == '\\' && index + 1 < length)
            {
                switch (text[index + 1])
                {
                    case '\\':
                        index += 1;
                        break;
                    case 'u':
                        // UTF16 format is "\uFF00" or u + 2 hex pairs.
                        if (index + 5 < length)
                        {
                            var utf16Value = GetUTF16(text, index + 2);
                            unicode = ((char)utf16Value).ToString();

                            index += 5;
                            return index;
                        }
                        break;
                    case 'U':
                        // UTF32 format is "\UFF00FF00" or U + 4 hex pairs.
                        if (index + 9 < length)
                        {
                            var utf32Value = GetUTF32(text, index + 2);
                            unicode = UTF32ToString(utf32Value);

                            index += 9;
                            return index;
                        }
                        break;
                }
            }

            unicode = null;
            return -1;
        }

        static string UTF32ToString(int intValue)
        {
            //Not a surrogate and is valid UTF32 (conditions to use char.ConvertFromUtf32 function)
            if (intValue > 0x000000 && intValue < 0x10ffff &&
                (intValue < 0x00d800 || intValue > 0x00dfff))
            {
                var UTF16Surrogate = char.ConvertFromUtf32(intValue);
                return UTF16Surrogate;
            }
            return null;
        }

        /// <summary>
        /// Cache all sequences in a lookuptable (and in a fastpath) found in SpriteAsset
        /// This Lookuptable will return the Sprite Index of the Emoji in SpriteAsset (the key will be the char sequence that will be used as replacement of old unicode format)
        /// 
        /// The sequence will be the name of the TMP_Sprite in UTF32 or UTF16 HEX format separeted by '-' for each character (see below the example)
        /// Ex: 0023-fe0f-20e3.png
        /// </summary>
        /// <param name="spriteAsset"> The sprite asset used to cache the sequences</param>
        /// <param name="forceUpdate"> force update the lookup table of this SpriteAsset</param>
        /// <returns>true if lookup table changed</returns>
        public static bool TryUpdateSequenceLookupTable(TMP_SpriteAsset spriteAsset, bool forceUpdate = false)
        {
            var mainSpriteAsset = spriteAsset == null ? TMPro.TMP_Settings.defaultSpriteAsset : spriteAsset;

            if (mainSpriteAsset != null && (!s_lookupTableSequences.ContainsKey(mainSpriteAsset) || s_lookupTableSequences[mainSpriteAsset] == null || forceUpdate))
            {
                //Init FastlookupPath
                if (mainSpriteAsset != null && (!s_fastLookupPath.ContainsKey(mainSpriteAsset) || s_fastLookupPath[mainSpriteAsset] == null))
                    s_fastLookupPath[mainSpriteAsset] = new HashSet<string>();
                var fastLookupPath = mainSpriteAsset != null && s_fastLookupPath.ContainsKey(mainSpriteAsset) ? s_fastLookupPath[mainSpriteAsset] : new HashSet<string>();
                fastLookupPath.Clear();

                //Init Lookup Table
                if (mainSpriteAsset != null && (!s_lookupTableSequences.ContainsKey(mainSpriteAsset) || s_lookupTableSequences[mainSpriteAsset] == null))
                    s_lookupTableSequences[mainSpriteAsset] = new Dictionary<string, string>();
                var lookupTableSequences = mainSpriteAsset != null && s_lookupTableSequences.ContainsKey(mainSpriteAsset) ? s_lookupTableSequences[mainSpriteAsset] : new Dictionary<string, string>();
                lookupTableSequences.Clear();

                List<TMPro.TMP_SpriteAsset> spriteAssetsChecked = new List<TMPro.TMP_SpriteAsset>();
                spriteAssetsChecked.Add(mainSpriteAsset);
                //Add the main sprite asset
                if (TMPro.TMP_Settings.defaultSpriteAsset != null && !spriteAssetsChecked.Contains(TMPro.TMP_Settings.defaultSpriteAsset))
                    spriteAssetsChecked.Add(TMPro.TMP_Settings.defaultSpriteAsset);

                //Check in all spriteassets (and fallbacks)
                for (int i = 0; i < spriteAssetsChecked.Count; i++)
                {
                    spriteAsset = spriteAssetsChecked[i];
                    if (spriteAsset != null)
                    {
                        //Check all sprites in this sprite asset
                        for (int j = 0; j < spriteAsset.spriteInfoList.Count; j++)
                        {
                            var element = spriteAsset.spriteInfoList[j];

                            if (element == null || string.IsNullOrEmpty(element.name) || !element.name.Contains("-"))
                                continue;

                            var elementName = BuildNameInEmojiSurrogateFormat(element.name);
                            var unicodeX8 = element.unicode.ToString("X8");

                            //Check for elements that Unicode is different from Name
                            if (!string.IsNullOrEmpty(elementName) &&
                                !string.Equals(elementName, unicodeX8, System.StringComparison.InvariantCultureIgnoreCase))
                            {
                                var tableStringBuilder = new System.Text.StringBuilder();
                                for (int k = 0; k < elementName.Length; k += 8)
                                {
                                    var hexUTF32 = elementName.Substring(k, Mathf.Min(elementName.Length - k, 8));
#if UNITY_2018_3_OR_NEWER
                                    var intValue = TMPro.TMP_TextUtilities.StringHexToInt(hexUTF32);
#else
                                    var intValue = TMPro.TMP_TextUtilities.StringToInt(hexUTF32);
#endif

                                    //Not a surrogate and is valid UTF32 (conditions to use char.ConvertFromUtf32 function)
                                    if (intValue > 0x000000 && intValue < 0x10ffff &&
                                        (intValue < 0x00d800 || intValue > 0x00dfff))
                                    {
                                        var UTF16Surrogate = char.ConvertFromUtf32(intValue);
                                        if (!string.IsNullOrEmpty(UTF16Surrogate))
                                        {
                                            //Add chars into cache (we must include the both char paths in fastLookupPath)
                                            foreach (var surrogateChar in UTF16Surrogate)
                                            {
                                                tableStringBuilder.Append(surrogateChar);
                                                //Add current path to lookup fast path
                                                fastLookupPath.Add(tableStringBuilder.ToString());
                                            }
                                        }
                                    }
                                    //Split into two chars (we failed to match conditions of char.ConvertFromUtf32 so we must split into two UTF16 chars)
                                    else
                                    {
                                        for (int l = 0; l < hexUTF32.Length; l += 4)
                                        {
                                            var hexUTF16 = hexUTF32.Substring(l, Mathf.Min(hexUTF32.Length - l, 4));
#if UNITY_2018_3_OR_NEWER
                                            var charValue = (char)TMPro.TMP_TextUtilities.StringHexToInt(hexUTF16);
#else
                                            var charValue = (char)TMPro.TMP_TextUtilities.StringToInt(hexUTF16);
#endif
                                            tableStringBuilder.Append(charValue);

                                            //Add current path to lookup fast path
                                            fastLookupPath.Add(tableStringBuilder.ToString());
                                        }
                                    }

                                }
                                var tableKey = tableStringBuilder.ToString();
                                //Add key as sequence in lookupTable
                                if (!string.IsNullOrEmpty(tableKey) && !lookupTableSequences.ContainsKey(tableKey))
                                {
                                    lookupTableSequences[tableKey] = element.name; //j;
                                }
                            }
                        }

                        //Add Fallbacks (before the next sprite asset and after this sprite asset)
                        for (int k = spriteAsset.fallbackSpriteAssets.Count - 1; k >= 0; k--)
                        {
                            var fallback = spriteAsset.fallbackSpriteAssets[k];
                            if (fallback != null && !spriteAssetsChecked.Contains(fallback))
                                spriteAssetsChecked.Insert(i + 1, fallback);
                        }
                    }
                }
                return true;
            }

            return false;
        }

        #endregion

        #region Other Helper Functions

        /*public static int GetSpriteIndexFromCharSequence(TMP_SpriteAsset spriteAsset, string charSequence)
        {
            var mainSpriteAsset = spriteAsset == null ? TMPro.TMP_Settings.defaultSpriteAsset : spriteAsset;
            TryUpdateSequenceLookupTable(mainSpriteAsset);

            Dictionary<string, int> lookupTable = null;
            s_lookupTableSequences.TryGetValue(mainSpriteAsset, out lookupTable);

            int index;
            if (lookupTable == null || !lookupTable.TryGetValue(charSequence, out index))
                index = -1;

            return index;
        }*/

        public static string GetSpriteNameFromCharSequence(TMP_SpriteAsset spriteAsset, string charSequence)
        {
            var mainSpriteAsset = spriteAsset == null ? TMPro.TMP_Settings.defaultSpriteAsset : spriteAsset;
            TryUpdateSequenceLookupTable(mainSpriteAsset);

            Dictionary<string, string> lookupTable = null;
            s_lookupTableSequences.TryGetValue(mainSpriteAsset, out lookupTable);

            string name;
            if (lookupTable == null || !lookupTable.TryGetValue(charSequence, out name))
                name = null;

            return name;
        }

        public static Dictionary<string, string> GetAllCharSequences(TMP_SpriteAsset spriteAsset)
        {
            var mainSpriteAsset = spriteAsset == null ? TMPro.TMP_Settings.defaultSpriteAsset : spriteAsset;
            TryUpdateSequenceLookupTable(mainSpriteAsset);

            Dictionary<string, string> lookupTable = null;
            if (!s_lookupTableSequences.TryGetValue(mainSpriteAsset, out lookupTable) && lookupTable == null)
                lookupTable = new Dictionary<string, string>();

            return lookupTable;
        }

        public static void ClearCache()
        {
            s_lookupTableSequences.Clear();
            s_fastLookupPath.Clear();
        }

        public static void ClearCache(TMP_SpriteAsset spriteAsset)
        {
            var mainSpriteAsset = spriteAsset == null ? TMPro.TMP_Settings.defaultSpriteAsset : spriteAsset;

            s_lookupTableSequences.Remove(mainSpriteAsset);
            s_lookupTableSequences.Remove(mainSpriteAsset);
        }

        #endregion

        #region Name Pattern Functions

        public static string BuildNameInEmojiSurrogateFormat(string name)
        {
            if (name == null)
                name = "";

            //Remove variant selectors (FE0F and FE0E)
            //ex: 2665-1F0FF5-FE0F.png will be converted to 2665-1f0f5 (remove variant selectors, cast to lower and remove file extension)
            var fileName = System.IO.Path.GetFileNameWithoutExtension(name).ToLower();

            //Split Surrogates and change to UTF16 or UTF32 (based in length of each string splitted)
            //ex: 2665-1f0f5 will be converted to [2665, 0001f0f5] and after that converted to 26650001f0f5
            if (fileName.Contains("-"))
            {
                var splitArray = fileName.Split(new char[] { '-' }, System.StringSplitOptions.RemoveEmptyEntries);
                fileName = "";
                for (int i = 0; i < splitArray.Length; i++)
                {
                    var split = splitArray[i];
                    while (/*split.Length > 4 && */split.Length < 8)
                    {
                        split = "0" + split;
                    }
                    fileName += split;
                }
            }
            return fileName;
        }

        #endregion

        #region Internal UTF16/UTF32 Parser Functions

        /// <summary>
        /// Convert UTF-16 Hex to Char
        /// </summary>
        /// <returns>The Unicode hex.</returns>
        /// <param name="i">The index.</param>
        static int GetUTF16(string text, int i)
        {
            int unicode = 0;
            unicode += CharHexToInt(text[i]) << 12;
            unicode += CharHexToInt(text[i + 1]) << 8;
            unicode += CharHexToInt(text[i + 2]) << 4;
            unicode += CharHexToInt(text[i + 3]);
            return unicode;
        }

        /// <summary>
        /// Convert UTF-32 Hex to Char
        /// </summary>
        /// <returns>The Unicode hex.</returns>
        /// <param name="i">The index.</param>
        static int GetUTF32(string text, int i)
        {
            int unicode = 0;
            unicode += CharHexToInt(text[i]) << 28;
            unicode += CharHexToInt(text[i + 1]) << 24;
            unicode += CharHexToInt(text[i + 2]) << 20;
            unicode += CharHexToInt(text[i + 3]) << 16;
            unicode += CharHexToInt(text[i + 4]) << 12;
            unicode += CharHexToInt(text[i + 5]) << 8;
            unicode += CharHexToInt(text[i + 6]) << 4;
            unicode += CharHexToInt(text[i + 7]);
            return unicode;
        }

        static int CharHexToInt(char hex)
        {
            switch (hex)
            {
                case '0': return 0;
                case '1': return 1;
                case '2': return 2;
                case '3': return 3;
                case '4': return 4;
                case '5': return 5;
                case '6': return 6;
                case '7': return 7;
                case '8': return 8;
                case '9': return 9;
                case 'A': return 10;
                case 'B': return 11;
                case 'C': return 12;
                case 'D': return 13;
                case 'E': return 14;
                case 'F': return 15;
                case 'a': return 10;
                case 'b': return 11;
                case 'c': return 12;
                case 'd': return 13;
                case 'e': return 14;
                case 'f': return 15;
            }
            return 15;
        }

        #endregion
    }
}

#if UNITY_2018_3_OR_NEWER && !UNITY_2019_1_OR_NEWER
#define TMP_1_4_0_OR_NEWER
#endif

#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro.EditorUtilities;
using TMPro.SpriteAssetUtilities;

namespace KyubEditor.EmojiSearch
{
    public static class EmojiDataConversorUtility
    {
        #region Helper Functions

        public static string ConvertToTexturePackerFormat(string json, Vector2Int gridSize, Vector2Int padding, Vector2Int spacing)
        {
            try
            {
                //Unity cannot deserialize Dictionary, so we converted the dictionary to List using MiniJson
                json = ConvertToUnityJsonFormat(json);
                PreConvertedSpritesheetData preData = JsonUtility.FromJson<PreConvertedSpritesheetData>(json);
                TexturePackerData.SpriteDataObject postData = preData.ToTexturePacketDataObject(gridSize, padding, spacing);

                return JsonUtility.ToJson(postData);
            }
            catch (System.Exception exception)
            {
                Debug.Log("Failed to convert to EmojiOne\n: " + exception);
            }

            return "";
        }

        static string ConvertToUnityJsonFormat(string json)
        {
            json = "{\"frames\":" + json + "}";

            var changed = false;
            var jObject = MiniJsonEditor.Deserialize(json) as Dictionary<string, object>;
            if (jObject != null)
            {
                var array = jObject.ContainsKey("frames") ? jObject["frames"] as IList : null;
                if (array != null)
                {
                    foreach (var jPreDataNonCasted in array)
                    {
                        var jPredataObject = jPreDataNonCasted as Dictionary<string, object>;
                        if (jPredataObject != null)
                        {
                            var skin_variation_dict = jPredataObject.ContainsKey("skin_variations") ? jPredataObject["skin_variations"] as Dictionary<string, object> : null;

                            if (skin_variation_dict != null)
                            {
                                changed = true;
                                List<object> skin_variation_array = new List<object>();

                                foreach (var skinVariationObject in skin_variation_dict.Values)
                                {
                                    
                                    skin_variation_array.Add(skinVariationObject);
                                }
                                jPredataObject["skin_variations"] = skin_variation_array;
                            }
                        }
                    }
                }
            }
            return jObject != null && changed ? MiniJsonEditor.Serialize(jObject) : json;
        }

        #endregion

        #region Helper Classes

        public class TexturePackerData
        {
            [System.Serializable]
            public struct SpriteFrame
            {
                public float x;
                public float y;
                public float w;
                public float h;

                public override string ToString()
                {
                    string s = "x: " + x.ToString("f2") + " y: " + y.ToString("f2") + " h: " + h.ToString("f2") + " w: " + w.ToString("f2");
                    return s;
                }
            }

            [System.Serializable]
            public struct SpriteSize
            {
                public float w;
                public float h;

                public override string ToString()
                {
                    string s = "w: " + w.ToString("f2") + " h: " + h.ToString("f2");
                    return s;
                }
            }

            [System.Serializable]
            public struct Frame
            {
                public string filename;
                public SpriteFrame frame;
                public bool rotated;
                public bool trimmed;
                public SpriteFrame spriteSourceSize;
                public SpriteSize sourceSize;
                public Vector2 pivot;
            }

            [System.Serializable]
            public class SpriteDataObject
            {
                public List<Frame> frames;
            }
        }

        [System.Serializable]
        public class PreConvertedSpritesheetData
        {
            public List<PreConvertedImgDataWithVariants> frames = new List<PreConvertedImgDataWithVariants>();

            public virtual TexturePackerData.SpriteDataObject ToTexturePacketDataObject(Vector2Int gridSize, Vector2 padding, Vector2 spacing)
            {
                TexturePackerData.SpriteDataObject postData = new TexturePackerData.SpriteDataObject();
                postData.frames = new List<TexturePackerData.Frame>();

                if (frames != null)
                {
                    var framesToCheck = new List<PreConvertedImgData>();
                    if (frames != null)
                    {
                        foreach (var frameToCheck in frames)
                        {
                            framesToCheck.Add(frameToCheck);
                        }
                    }

                    for(int i=0; i< framesToCheck.Count; i++)
                    {
                        var preFrame = framesToCheck[i];

                        //Add all variations in list to check (after the current PreFrame)
                        var preFrameWithVariants = framesToCheck[i] as PreConvertedImgDataWithVariants;
                        if (preFrameWithVariants != null && preFrameWithVariants.skin_variations != null && preFrameWithVariants.skin_variations.Count > 0)
                        {
                            for (int j = preFrameWithVariants.skin_variations.Count-1; j >=0; j--)
                            {
                                var skinVariantFrame = preFrameWithVariants.skin_variations[j];
                                if (skinVariantFrame != null)
                                    framesToCheck.Insert(i+1, skinVariantFrame);
                            }
                        }

                        //Create TexturePacker SpriteData
                        var postFrame = new TexturePackerData.Frame();

                        postFrame.filename = preFrame.image;
                        postFrame.rotated = false;
                        postFrame.trimmed = false;
                        postFrame.sourceSize = new TexturePackerData.SpriteSize() { w = gridSize.x, h = gridSize.y };
                        postFrame.spriteSourceSize = new TexturePackerData.SpriteFrame() { x = 0, y = 0, w = gridSize.x, h = gridSize.y };
                        postFrame.frame = new TexturePackerData.SpriteFrame()
                        {
                            x = (preFrame.sheet_x * (gridSize.x + spacing.x)) + padding.x,
                            y = (preFrame.sheet_y * (gridSize.y + spacing.y)) + padding.y,
                            w = gridSize.x,
                            h = gridSize.y
                        };
                        postFrame.pivot = new Vector2(0f, 0f);

                        postData.frames.Add(postFrame);
                    }
                }

                return postData;
            }
        }

        [System.Serializable]
        public class PreConvertedImgData
        {
            public string name;
            public string unified;
            public string non_qualified;
            public string docomo;
            public string au;
            public string softbank;
            public string google;
            public string image;
            public int sheet_x;
            public int sheet_y;
            public string short_name;
            public string[] short_names;
            public object text;
            public object texts;
            public string category;
            public int sort_order;
            public string added_in;
            public bool has_img_apple;
            public bool has_img_google;
            public bool has_img_twitter;
            public bool has_img_facebook;
            public bool has_img_messenger;
        }

        [System.Serializable]
        public class PreConvertedImgDataWithVariants : PreConvertedImgData
        {
            public List<PreConvertedImgData> skin_variations = new List<PreConvertedImgData>();
        }

        #endregion
    }
}

#endif

using TMPro;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UdonSharpEditor;

public static class MetaReaManyLayout
{
    public static void Apply(MetaReaWallGuide g)
    {
            var template = g.topButtons[0].transform;
            string photo = AnimationUtility.CalculateTransformPath(g.topPictures[0].transform, template);
            string caption = AnimationUtility.CalculateTransformPath(g.topCaptions[0].transform, template);
            string title = AnimationUtility.CalculateTransformPath(g.topTitles[0].transform, template);
            string author = AnimationUtility.CalculateTransformPath(g.topAuthors[0].transform, template);
            string summary = AnimationUtility.CalculateTransformPath(g.topSummaries[0].transform, template);
            var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(g);
            g.manyVersion = true;
            g.manyButtons = new Button[4]; g.manyPictures = new RawImage[4];
            g.manyCaptions = new TMP_Text[4]; g.manyTitles = new TMP_Text[4]; g.manyAuthors = new TMP_Text[4];
            g.manySummaries = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                var existing = g.topPanel.transform.Find("Many " + i);
                var card = existing != null ? existing.gameObject : UnityEngine.Object.Instantiate(template.gameObject, g.topPanel.transform);
                card.name = "Many " + i;
                g.manyButtons[i] = card.GetComponent<Button>();
                g.manyButtons[i].onClick = new Button.ButtonClickedEvent();
                UnityEventTools.AddStringPersistentListener(g.manyButtons[i].onClick, udon.SendCustomEvent, "_Many" + i);
                g.manyPictures[i] = card.transform.Find(photo).GetComponent<RawImage>();
                g.manyCaptions[i] = card.transform.Find(caption).GetComponent<TMP_Text>();
                g.manyTitles[i] = card.transform.Find(title).GetComponent<TMP_Text>();
                g.manyAuthors[i] = card.transform.Find(author).GetComponent<TMP_Text>();
                g.manySummaries[i] = card.transform.Find(summary).GetComponent<TMP_Text>();
                g.manySummaries[i].gameObject.SetActive(true);
                g.manySummaries[i].fontSize = 16;
                g.manySummaries[i].enableAutoSizing = false;
                g.manySummaries[i].lineSpacing = 0f;
                g.manySummaries[i].alignment = TextAlignmentOptions.TopLeft;
                g.manySummaries[i].enableWordWrapping = true;
                g.manySummaries[i].overflowMode = TextOverflowModes.Truncate;
                g.manySummaries[i].richText = false;
                g.manyCaptions[i].fontSize = 18;
                g.manyCaptions[i].enableAutoSizing = true; g.manyCaptions[i].fontSizeMin = 15; g.manyCaptions[i].fontSizeMax = 18;
                g.manyTitles[i].fontSize = 24;
                g.manyTitles[i].enableAutoSizing = true; g.manyTitles[i].fontSizeMin = 19; g.manyTitles[i].fontSizeMax = 24;
                g.manyTitles[i].enableWordWrapping = true;
                g.manyAuthors[i].fontSize = 16;
                g.manyAuthors[i].enableAutoSizing = true; g.manyAuthors[i].fontSizeMin = 13; g.manyAuthors[i].fontSizeMax = 16;
            }
            g.headingTexts = g.headingTexts.Concat(g.manyTitles).Distinct().ToArray();
            g.accentTexts = g.accentTexts.Concat(g.manyCaptions).Distinct().ToArray();
            g.secondaryTexts = g.secondaryTexts.Concat(g.manyAuthors).Distinct().ToArray();
            g.bodyTexts = g.bodyTexts.Concat(g.manySummaries).Distinct().ToArray();
            g.cardImages = g.cardImages.Concat(g.manyButtons.Select(b => b.GetComponent<Image>())).Distinct().ToArray();
            g.borderOutlines = g.GetComponentsInChildren<Outline>(true);
            foreach (var button in g.topButtons) button.gameObject.SetActive(false);
            MetaReaPhotoOverlay.Apply(g);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(MetaReaWallGuide).GetMethod("RenderMany", flags).Invoke(g, null);
            g._ApplyAppearance();
            MetaReaPlaceholderLayout.Apply(g);
            UdonSharpEditorUtility.CopyProxyToUdon(g);
    }
}


﻿using RimValiCore.Windows.GUIUtils;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimValiCore.QLine
{
    public class QL_Window : MainTabWindow
    {
        /// <summary>
        ///     Stores what quests are expanded
        /// </summary>
        private static readonly HashSet<QLine> expandedQuests = new HashSet<QLine>();

        private readonly Rect rectFull = new Rect(0f, 0f, 240f, 440f);
        private readonly Rect rectMain;

        //Quest Item stuff
        private readonly Rect rectContentPartOuter;

        private Rect rectContentPartInner;
        private Rect rectQuestBase;
        private Rect rectQuestStageBase;

        //Title stuff
        private readonly Rect rectTopPart;
        private readonly Rect rectTitle;

        //Variables
        private const float CommonMargin = 5f;
        private const float ItemHeight = 30f;
        private const float ExpandCollapseIconSize = 18f;
        private int savedQuestCount;

        //
        private Vector2 listScroll;

        private List<QLine> Quests => Find.World.GetComponent<Quest_Tracker>().Quests;

        public override Vector2 InitialSize => rectFull.size;

        protected override float Margin => 0f;

        public float RequiredHeightForInnerScrollRect => (ItemHeight + CommonMargin) * Quests.Sum(quest => 1 + (expandedQuests.Contains(quest) ? quest.Worker.Stages.Sum(stage => quest.Worker.IsStageCompletedOrCurrent(stage) ? 1 : 0) : 0));

        public QL_Window()
        {
            def = DefDatabase<MainButtonDef>.GetNamed("QuestQUI");

            rectMain = rectFull.ContractedBy(CommonMargin * 2f);
            rectTopPart = rectMain.TopPartPixels(30f);
            rectTitle = new Rect(rectTopPart.x, rectTopPart.y, rectTopPart.width, rectTopPart.height - 5f);

            rectContentPartOuter = new Rect(rectMain.x, rectMain.y + rectTopPart.height, rectMain.width, rectMain.height - rectTopPart.height);
            savedQuestCount = Quests.Count;
            RefreshScrollRects();
        }

        public override void DoWindowContents(Rect inRect)
        {
            RefreshScrollRectsIfNeeded();

            DrawTitleBar();
            DrawQuestList();
        }

        /// <summary>
        ///     Refreshes the scroll rects if the amount of quests has changed
        /// </summary>
        private void RefreshScrollRectsIfNeeded()
        {
            if (savedQuestCount == Quests.Count) return;

            savedQuestCount = Quests.Count;
            RefreshScrollRects();
        }

        /// <summary>
        ///     This function refreshes the height of <see cref="rectContentPartInner"/>, so that the scrollbar doesn't end up too short/long
        /// </summary>
        public void RefreshScrollRects()
        {
            rectContentPartInner = rectContentPartOuter.GetInnerScrollRect(RequiredHeightForInnerScrollRect);
            rectQuestBase = new Rect(rectContentPartInner.x, rectContentPartInner.y, rectContentPartInner.width, ItemHeight);
            rectQuestStageBase = new Rect(rectContentPartInner.x + CommonMargin * 2f, rectContentPartInner.y, rectContentPartInner.width - CommonMargin * 2f, ItemHeight);
        }

        /// <summary>
        ///     Lists all quests
        /// </summary>
        private void DrawQuestList()
        {
            Widgets.BeginScrollView(rectContentPartOuter, ref listScroll, rectContentPartInner);

            int displayedButtons = 0;
            for (int i = 0; i < Quests.Count; i++)
            {
                QLine quest = Quests[i];
                Vector2 baseVector = new Vector2(0f, (rectQuestBase.height + CommonMargin) * (i + displayedButtons));
                Rect rectQuestItem = new Rect(rectQuestBase).MoveRect(baseVector);

                DrawExpandAndUnexpandQuestButton(quest, rectQuestItem, (i + displayedButtons) % 2 == 1);
                DrawStageListing(ref displayedButtons, i, quest, baseVector);
            }

            Widgets.EndScrollView();

            Text.Anchor = TextAnchor.UpperLeft;
        }

        /// <summary>
        ///     Lists every <see cref="QuestStage"/> of the given <paramref name="quest"/>
        /// </summary>
        /// <param name="displayedButtons">The number of buttons displayed, used for determining the correct highlighting</param>
        /// <param name="i">the current quest iterator, used for determining the correct highlighting</param>
        /// <param name="quest">the current quest</param>
        /// <param name="baseVector">the vector by which the base button is moved by</param>
        private void DrawStageListing(ref int displayedButtons, int i, QLine quest, Vector2 baseVector)
        {
            if (!expandedQuests.Contains(quest)) return;

            int displayedStages = 0;
            for (int j = 0; j < quest.Worker.Stages.Count; j++)
            {
                QuestStage questStage = quest.Worker.Stages[j];
                if (!quest.Worker.IsStageCompletedOrCurrent(questStage)) continue;

                displayedButtons++;

                Rect rectQuestStage = new Rect(rectQuestStageBase).MoveRect(baseVector + new Vector2(0f, rectQuestBase.height + CommonMargin + (rectQuestBase.height + CommonMargin) * displayedStages));
                Widgets.DrawBox(rectQuestStage);
                rectQuestStage.DoRectHighlight((i + displayedButtons) % 2 == 1);
                Widgets.Label(rectQuestStage.MoveRect(new Vector2(CommonMargin, 0f)), questStage.LabelCap);
                Widgets.DrawHighlightIfMouseover(rectQuestStage);

                if (Widgets.ButtonInvisible(rectQuestStage))
                {
                    Find.WindowStack.Add(new QL_DecisionWindow(quest, questStage, j, quest.Worker.CurrentStage));
                    SoundDefOf.TabOpen.PlayOneShotOnCamera();
                }

                displayedStages++;
            }
        }

        /// <summary>
        ///     Draws a button that expands and unexpands a <see cref="QLine"/> <paramref name="quest"/> inside a <see cref="Rect"/> <paramref name="rectQuestItem"/>
        /// </summary>
        /// <param name="quest">the <see cref="QLine"/> to be expanded or unexpanded</param>
        /// <param name="rectQuestItem">the <see cref="Rect"/> the button is drawn in</param>
        /// <param name="doLightHighlight">determines if the button should have a light or normal highlight</param>
        private void DrawExpandAndUnexpandQuestButton(QLine quest, Rect rectQuestItem, bool doLightHighlight)
        {
            Rect rectExpandCollapseIcon = rectQuestItem.RightPartPixels(ExpandCollapseIconSize).BottomPartPixels(ExpandCollapseIconSize).MoveRect(new Vector2(-CommonMargin, -CommonMargin));

            Widgets.DrawBox(rectQuestItem);
            rectQuestItem.DoRectHighlight(doLightHighlight);
            Widgets.Label(rectQuestItem.MoveRect(new Vector2(CommonMargin, 0f)), quest.Quest.LabelCap);
            Widgets.DrawTextureFitted(rectExpandCollapseIcon, expandedQuests.Contains(quest) ? TexButton.Collapse : TexButton.Reveal, 1f);
            Widgets.DrawHighlightIfMouseover(rectQuestItem);

            if (Widgets.ButtonInvisible(rectQuestItem))
            {
                if (expandedQuests.Add(quest))
                {
                    SoundDefOf.TabOpen.PlayOneShotOnCamera();
                }
                else
                {
                    expandedQuests.Remove(quest);
                    SoundDefOf.TabClose.PlayOneShotOnCamera();
                }

                RefreshScrollRects();
            }
        }

        /// <summary>
        ///     Draws the title and the line beneath it
        /// </summary>
        private void DrawTitleBar()
        {
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Medium;

            Widgets.Label(rectTitle, "##Quests:");
            Widgets.DrawLineHorizontal(rectTitle.x, rectTitle.yMax, rectTitle.width);

            Text.Font = GameFont.Small;
        }
    }

    public class QL_DecisionWindow : Window
    {
        private readonly Rect rectFull = new Rect(0f, 0f, 660f, 440f);
        private readonly Rect rectMain;

        //Title
        private readonly Rect rectTop;
        private readonly Rect rectLabel;

        //Description Box
        private Rect rectBackgroundImage = new Rect (0f, 0f, 223f, 223f);
        private Rect rectDecisionButtonBase;
        private Rect rectDescriptionBox;

        private Texture2D questTexture;

        //Decision Button Space
        private Rect rectBottom;

        //Variables
        private readonly QLine quest;
        private readonly QuestStage stage;
        private readonly int stageIndex;
        private readonly int currentStage;

        private const float CommonMargin = 5f;
        private const float DecisionButtonHeight = 25f;
        private const float DecisionButtonSpace = DecisionButtonHeight + CommonMargin;

        private string debugStageSelectorBuffer;
        private int debugStageSelector;
        private Vector2 labelScroll;

        private bool DoButtons => stageIndex == currentStage;

        public override Vector2 InitialSize => rectFull.size;

        protected override float Margin => 0f;

        public QL_DecisionWindow(QLine quest, QuestStage stage, int stageIndex, int currentStage)
        {
            this.quest = quest;
            this.stage = stage;
            this.stageIndex = stageIndex;
            this.currentStage = currentStage;
            questTexture = QuestBackgroundHandler.GetTexture(quest.Quest.texPath);

            doCloseX = true;
            forcePause = DoButtons;
            onlyOneOfTypeAllowed = true;
            preventCameraMotion = false;

            rectMain = rectFull.ContractedBy(25f);
            rectTop = rectMain.TopPartPixels(35f);
            rectLabel = rectTop.TopPartPixels(30f);

            if (DoButtons)
            {
                rectBottom = rectMain.BottomPartPixels(stage.buttons.Count * (DecisionButtonHeight + CommonMargin) - CommonMargin);
            }

            rectDescriptionBox = new Rect(rectMain.x, rectMain.y + rectTop.height, rectMain.width, rectMain.height - rectTop.height - rectBottom.height - (DoButtons ? CommonMargin : 0f));

            IncreaseSpaceForDebugButton();
            rectDecisionButtonBase = rectBottom.TopPartPixels(DecisionButtonHeight);
        }

        public override void DoWindowContents(Rect inRect)
        {
            KillSelfIfQLWindowMissing();

            DrawTitleBar();
            DrawDescription();
            DrawDecisionButtons();
        }

        /// <summary>
        ///     Places this window next to the <see cref="QL_Window"/>
        /// </summary>
        protected override void SetInitialSizeAndPosition()
        {
            base.SetInitialSizeAndPosition();
            
            if (Find.WindowStack.WindowOfType<QL_Window>() is QL_Window mainTabWindow)
            {
                windowRect.x = mainTabWindow.InitialSize.x + CommonMargin;
                windowRect.y = UI.screenHeight - 35f - windowRect.height;
            }
        }

        /// <summary>
        ///     Increases the space so that a debug button can fit
        /// </summary>
        /// <returns>true if space was increased, false otherwise</returns>
        private bool IncreaseSpaceForDebugButton()
        {
            bool increaseSpace = RimValiCoreMod.Settings.QL_DecisionWindow_ShowDebug && DoButtons;
            
            if (increaseSpace)
            {
                rectDescriptionBox.yMax -= DecisionButtonSpace;
                rectBottom.y -= DecisionButtonSpace;
            }

            rectBackgroundImage.center = rectDescriptionBox.center;
            return increaseSpace;
        }

        /// <summary>
        ///     Closes this window if <see cref="QL_Window"/> is closed
        /// </summary>
        private void KillSelfIfQLWindowMissing()
        {
            if (Find.WindowStack.WindowOfType<QL_Window>() is null)
            {
                Close();
            }
        }

        /// <summary>
        ///     Draws the buttons that are used to make decisions
        /// </summary>
        private void DrawDecisionButtons()
        {
            for (int i = 0; i < stage.buttons.Count; i++)
            {
                Rect rectButton = rectDecisionButtonBase.MoveRect(new Vector2(0f, (rectDecisionButtonBase.height + CommonMargin) * i));
                Rect rectIcon = rectButton.LeftPartPixels(rectButton.height).ContractedBy(4f);
                QuestStageButtonDecision button = stage[i];
                bool buttonDisabled = button.Disabled;

                if (buttonDisabled)
                {
                    GUI.color = new Color(0.6f, 0.6f, 0.6f);
                }

                rectButton.DrawButtonText(button.ButtonText, () =>
                {
                    button.ButtonAction();
                    Close();
                }, buttonDisabled);

                GUI.color = Color.white;
                rectButton.MakeToolTip(windowRect.position, button.DisableReasons);

                GUI.DrawTexture(rectIcon, buttonDisabled ? Widgets.CheckboxOffTex : Widgets.CheckboxOnTex);
            }

            DrawDebugButton();
        }

        /// <summary>
        ///     Draws debug buttons
        /// </summary>
        private void DrawDebugButton()
        {
            if (RimValiCoreMod.Settings.QL_DecisionWindow_ShowDebug)
            {
                Rect rectButton = rectDecisionButtonBase.MoveRect(new Vector2(0f, (rectDecisionButtonBase.height + CommonMargin) * stage.buttons.Count));

                if (currentStage == quest.Worker.Stages.Count - 1)
                {
                    rectButton.DrawButtonText("DEBUG Finish Quest", () =>
                    {
                        quest.Worker.FinishQuest();
                        Close();
                    });

                    return;
                }

                Widgets.TextFieldNumeric(rectButton.RightHalf(), ref debugStageSelector, ref debugStageSelectorBuffer, currentStage + 1, quest.Worker.Stages.Count - 1);

                rectButton.LeftHalf().DrawButtonText("DEBUG Skip Stage", () =>
                {
                    quest.Worker.ChangeStage(debugStageSelector - currentStage);
                    Close();
                });
            }
        }

        /// <returns>A stage's debug string</returns>
        private string GetStageDebugString()
        {
            return $"\n\nstage: {stage}\nstageIndex: {stageIndex}\ncurrentStage: {currentStage}\namount of Stages: {quest.Worker.Stages.Count}\nisCompleted: {quest.Worker.IsStageCompleted(stage)}\nDoButtons: {DoButtons}";
        }

        /// <summary>
        ///     Draws the stage description and background image
        /// </summary>
        private void DrawDescription()
        {
            string debugString = RimValiCoreMod.Settings.QL_DecisionWindow_ShowDebug ? GetStageDebugString() : string.Empty;
            string descriptionText = $"{stage.description}{debugString}";

            if (questTexture != null)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.1f);
                GUI.DrawTexture(rectBackgroundImage, questTexture);
                GUI.color = Color.white;
            }

            Widgets.DrawBox(rectDescriptionBox);
            Widgets.DrawLightHighlight(rectDescriptionBox);
            Widgets.LabelScrollable(rectDescriptionBox.ContractedBy(CommonMargin), descriptionText, ref labelScroll);
        }

        /// <summary>
        ///     Draws the title, and the horizontal line seperating it from the description
        /// </summary>
        private void DrawTitleBar()
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(rectLabel, stage.LabelCap);
            Text.Font = GameFont.Small;

            Widgets.DrawLineHorizontal(rectLabel.x, rectLabel.yMax, rectLabel.width);

            DrawDebugCheckbox();
        }

        /// <summary>
        ///     Adds a debug checkbox to the window, inside the top part
        /// </summary>
        private void DrawDebugCheckbox()
        {
            if (!Prefs.DevMode) return;

            bool previous = RimValiCoreMod.Settings.QL_DecisionWindow_ShowDebug;
            Widgets.CheckboxLabeled(rectLabel.RightPartPixels(150f), "Show Debug", ref RimValiCoreMod.Settings.QL_DecisionWindow_ShowDebug);

            if (previous != RimValiCoreMod.Settings.QL_DecisionWindow_ShowDebug)
            {
                if (!IncreaseSpaceForDebugButton() && DoButtons)
                {
                    rectDescriptionBox.yMax += DecisionButtonSpace;
                    rectBottom.y += DecisionButtonSpace;
                }

                rectBackgroundImage.center = rectDescriptionBox.center;
                rectDecisionButtonBase = rectBottom.TopPartPixels(DecisionButtonHeight);
            }
        }

        public override void Close(bool doCloseSound = true)
        {
            if (Find.WindowStack.WindowOfType<QL_Window>() is QL_Window mainTabWindow) mainTabWindow.RefreshScrollRects();
            RimValiCoreMod.Settings.Write();
            base.Close(doCloseSound);
        }
    }
}

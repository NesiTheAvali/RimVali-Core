﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimValiCore.Windows.GUIUtils;
using Verse.Sound;

namespace RimValiCore.QLine
{
    public class QL_Window : MainTabWindow
    {
        /// <summary>
        ///     Stores what quests are expanded
        /// </summary>
        private static readonly HashSet<QL_Quest> expandedQuests = new HashSet<QL_Quest>();

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

        //consts
        private const float CommonMargin = 5f;
        private const float ItemHeight = 30f;
        private const float ExpandCollapseIconSize = 18f;

        //
        private Vector2 listScroll;

        public override Vector2 InitialSize => rectFull.size;

        protected override float Margin => 0f;

        public float RequiredHeightForInnerScrollRect => (ItemHeight + CommonMargin) * DefDatabase<QL_Quest>.AllDefsListForReading.Sum(def => 1 + (expandedQuests.Contains(def) ? def.QuestWorker.Stages.Count : 0));

        public QL_Window()
        {
            def = DefDatabase<MainButtonDef>.GetNamed("QuestQUI");

            rectMain = rectFull.ContractedBy(CommonMargin * 2f);
            rectTopPart = rectMain.TopPartPixels(30f);
            rectTitle = new Rect(rectTopPart.x, rectTopPart.y, rectTopPart.width, rectTopPart.height - 5f);

            rectContentPartOuter = new Rect(rectMain.x, rectMain.y + rectTopPart.height, rectMain.width, rectMain.height - rectTopPart.height);
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

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;

            Widgets.Label(rectTitle, "##Quests:");
            Widgets.DrawLineHorizontal(rectTitle.x, rectTitle.yMax, rectTitle.width);

            Text.Font = GameFont.Small;

            //Quest Listing
            Widgets.BeginScrollView(rectContentPartOuter, ref listScroll, rectContentPartInner);

            int count = 0;
            for (int i = 0; i < DefDatabase<QL_Quest>.AllDefsListForReading.Count; i++)
            {
                QL_Quest quest = DefDatabase<QL_Quest>.AllDefsListForReading[i];
                Vector2 baseVector = new Vector2(0f, (rectQuestBase.height + CommonMargin) * (i + count));
                Rect rectQuestItem = new Rect(rectQuestBase).MoveRect(baseVector);
                Rect rectExpandCollapseIcon = rectQuestItem.RightPartPixels(ExpandCollapseIconSize).BottomPartPixels(ExpandCollapseIconSize).MoveRect(new Vector2(- CommonMargin, - CommonMargin));

                Widgets.DrawBox(rectQuestItem);
                rectQuestItem.DoRectHighlight((i + count) % 2 == 1);
                Widgets.Label(rectQuestItem.ContractedBy(CommonMargin), quest.LabelCap);
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

                //Quest stage Listing
                if (expandedQuests.Contains(quest))
                {
                    for (int j = 0; j < quest.QuestWorker.Stages.Count; j++)
                    {
                        QuestStage questStage = quest.QuestWorker.Stages[j];
                        count++;

                        Rect rectQuestStage = new Rect(rectQuestStageBase).MoveRect(baseVector + new Vector2(0f, rectQuestBase.height + CommonMargin + (rectQuestBase.height + CommonMargin) * j));
                        Widgets.DrawBox(rectQuestStage);
                        rectQuestStage.DoRectHighlight((i + count) % 2 == 1);
                        Widgets.Label(rectQuestStage, questStage.LabelCap);
                        Widgets.DrawHighlightIfMouseover(rectQuestStage);

                        if (Widgets.ButtonInvisible(rectQuestStage))
                        {
                            Find.WindowStack.Add(new QL_DecisionWindow(quest, questStage, j, quest.QuestWorker.CurrentStage));
                            SoundDefOf.TabOpen.PlayOneShotOnCamera();
                        }
                    }
                }
            }

            Widgets.EndScrollView();
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
        private Rect rectDecisionButtonBase;
        private Rect rectDescriptionBox;

        //Decision Button Space
        private Rect rectBottom;

        //Variables
        private readonly QL_Quest quest;
        private readonly QuestStage stage;
        private readonly int stageIndex;
        private readonly int currentStage;

        private const float CommonMargin = 5f;
        private const float DecisionButtonHeight = 25f;
        private const float DecisionButtonSpace = DecisionButtonHeight + CommonMargin;

        private Vector2 labelScroll;

        private bool DoButtons => stageIndex == currentStage;

        public override Vector2 InitialSize => rectFull.size;

        protected override float Margin => 0f;

        public QL_DecisionWindow(QL_Quest quest, QuestStage stage, int stageIndex, int currentStage)
        {
            this.quest = quest;
            this.stage = stage;
            this.stageIndex = stageIndex;
            this.currentStage = currentStage;

            doCloseX = true;
            forcePause = DoButtons;
            onlyOneOfTypeAllowed = true;

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

            return increaseSpace;
        }

        public override void DoWindowContents(Rect inRect)
        {
            DrawTopPart();
            DrawDescription();
            DrawDecisionButtons();
        }

        /// <summary>
        ///     Draws the buttons that are used to make decisions
        /// </summary>
        private void DrawDecisionButtons()
        {
            for (int i = 0; i < stage.buttons.Count; i++)
            {
                QuestStageButtonDecision decision = stage[i];
                Rect rectButton = rectDecisionButtonBase.MoveRect(new Vector2(0f, (rectDecisionButtonBase.height + CommonMargin) * i));
                rectButton.DrawButtonText(stage[i].ButtonText, () =>
                {
                    stage[i].Action();
                    Close();
                });
            }

            if (RimValiCoreMod.Settings.QL_DecisionWindow_ShowDebug)
            {
                Rect rectButton = rectDecisionButtonBase.MoveRect(new Vector2(0f, (rectDecisionButtonBase.height + CommonMargin) * stage.buttons.Count));
                rectButton.DrawButtonText("DEBUG Skip Stage", () =>
                {
                    quest.QuestWorker.IncrementStage();
                    Close();
                });
            }
        }

        /// <returns>A stage's debug string</returns>
        private string GetStageDebugString()
        {
            return $"\n\nstage: {stage}\nstageIndex: {stageIndex}\ncurrentStage: {currentStage}\nisCompleted: {quest.QuestWorker.IsStageCompleted(stage)}\nDoButtons: {DoButtons}";
        }

        /// <summary>
        ///     Draws the stage description
        /// </summary>
        private void DrawDescription()
        {
            string debugString = RimValiCoreMod.Settings.QL_DecisionWindow_ShowDebug ? GetStageDebugString() : string.Empty;
            string descriptionText = $"{stage.description}{debugString}";

            Widgets.DrawBox(rectDescriptionBox);
            Widgets.DrawLightHighlight(rectDescriptionBox);
            Widgets.LabelScrollable(rectDescriptionBox.ContractedBy(CommonMargin), descriptionText, ref labelScroll);
        }

        /// <summary>
        ///     Draws the title, and the horizontal line seperating it from the description
        /// </summary>
        private void DrawTopPart()
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(rectLabel, stage.LabelCap);
            Text.Font = GameFont.Small;

            Widgets.DrawLineHorizontal(rectLabel.x, rectLabel.yMax, rectLabel.width);

            DrawDebugOptions();
        }

        /// <summary>
        ///     Adds a debug checkbox to the window, inside the top part
        /// </summary>
        private void DrawDebugOptions()
        {
            if (!Prefs.DevMode) return;

            bool previous = RimValiCoreMod.Settings.QL_DecisionWindow_ShowDebug;
            Widgets.CheckboxLabeled(rectLabel.RightPartPixels(150f), "##Show Debug", ref RimValiCoreMod.Settings.QL_DecisionWindow_ShowDebug);

            if (previous != RimValiCoreMod.Settings.QL_DecisionWindow_ShowDebug)
            {
                if (!IncreaseSpaceForDebugButton() && DoButtons)
                {
                    rectDescriptionBox.yMax += DecisionButtonSpace;
                    rectBottom.y += DecisionButtonSpace;
                }

                rectDecisionButtonBase = rectBottom.TopPartPixels(DecisionButtonHeight);
            }
        }

        public override void Close(bool doCloseSound = true)
        {
            RimValiCoreMod.Settings.Write();
            base.Close(doCloseSound);
        }
    }
}

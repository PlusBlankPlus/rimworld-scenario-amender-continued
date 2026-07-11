using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace ScenarioAmender
{
    public class Page_MidGameScenarioEditor : Page_ScenarioEditor
    {
        private Predicate<Scenario> CheckAllPartsCompatible { get; }

        public Page_MidGameScenarioEditor()
            : base(Find.Scenario.CopyForEditing())
        {
            MethodInfo method = typeof(Page_ScenarioEditor).GetMethod("CheckAllPartsCompatible", BindingFlags.Static | BindingFlags.NonPublic);
            CheckAllPartsCompatible = Delegate.CreateDelegate(typeof(Predicate<Scenario>), method) as Predicate<Scenario>;
            nextAct = SaveAndClose;
        }

        public override void DoWindowContents(Rect rect)
        {
            base.DoWindowContents(rect);
            DoBottomButtons(rect, "Save".Translate());
        }

        public void SaveAndClose()
        {
            List<GameCondition> list = new List<GameCondition>();
            Find.CurrentMap.GameConditionManager.GetAllGameConditionsAffectingMap(Find.CurrentMap, list);
            foreach (GameCondition item in list)
            {
                item.Permanent = false;
            }
            (typeof(GameRules).GetField("disallowedDesignatorTypes", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(Current.Game.Rules) as HashSet<Type>)?.Clear();
            (typeof(GameRules).GetField("disallowedBuildings", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(Current.Game.Rules) as HashSet<ThingDef>)?.Clear();
            Current.Game.Scenario = base.EditingScenario;

            foreach (ScenPart allPart in Current.Game.Scenario.AllParts)
            {
                if (allPart is ScenPart_PlayerFaction)
                {
                    typeof(ScenPart_PlayerFaction).GetField("factionDef", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(allPart, Find.FactionManager.OfPlayer.def);
                }
                else if ((!(allPart is ScenPart_GameStartDialog) || Find.GameInitData != null) && !(allPart is ScenPart_ConfigPage_ConfigureStartingPawns))
                {
                    try
                    {
                        allPart.PostGameStart();
                        if (Find.GameInitData != null)
                        {
                            allPart.PostWorldGenerate();
                        }

                        allPart.GenerateIntoMap(Find.CurrentMap);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[ScenarioAmender] Failed to initialize scenario part {allPart.def?.defName}: {ex}");
                    }
                }
            }
            try
            {
                foreach (StatDef statDef in DefDatabase<StatDef>.AllDefs)
                {
                    if (statDef.Worker != null)
                    {
                        statDef.Worker.TryClearCache(); // Оставляем только его!
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[ScenarioAmender] Failed to clear game stat cache: {ex}");
            }

            Close();
        }

        protected override bool CanDoNext()
        {
            return CheckAllPartsCompatible(base.EditingScenario);
        }
    }
}

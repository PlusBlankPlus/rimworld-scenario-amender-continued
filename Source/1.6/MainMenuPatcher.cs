using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ScenarioAmender
{
    class MainMenuPatcher
    {
        public static void ReplaceReviewScenarioButton(List<Verse.ListableOption> list)
        {
            string targetString = "ReviewScenario".Translate().ToString();
            foreach (var entry in list)
            {
                if (entry.label == targetString)
                {
                    entry.action = new Action(ReviewScenarioValue);
                }
            }
        }

        static void ReviewScenarioValue()
        {
            Find.WindowStack.Add(new Page_MidGameScenarioEditor());
        }
    }
}

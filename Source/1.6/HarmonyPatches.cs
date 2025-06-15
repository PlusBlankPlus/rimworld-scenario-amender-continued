using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using RimWorld;
using HarmonyLib;
using Verse;
using System.Linq;

namespace ScenarioAmender
{
    [StaticConstructorOnStartup]
    internal class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("katana.scenarioamender");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.Message("[katana.scenarioamender] Loaded");
        }

        [HarmonyPatch(typeof(RimWorld.MainMenuDrawer), "DoMainMenuControls")]
        public static class MainMenuDrawer_DoMainMenuControls_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo drawOptionListingMethod = AccessTools.Method(typeof(Verse.OptionListingUtility), nameof(Verse.OptionListingUtility.DrawOptionListing));
                MethodInfo patcherMethod = AccessTools.Method(typeof(ScenarioAmender.MainMenuPatcher), nameof(ScenarioAmender.MainMenuPatcher.ReplaceReviewScenarioButton));
                List<CodeInstruction> instructionList = instructions.ToList();

                bool done = false;
                for (int i = 0; i < instructionList.Count; i++)
                {
                    CodeInstruction instruction = instructionList[i];

                    // safety in case we manage to jump past the actual targeted IL
                    if (!done && (i + 2) < instructionList.Count)
                    {
                        CodeInstruction lookahead = instructionList[i + 2];
                        if (lookahead.Calls(drawOptionListingMethod))
                        {
                            CodeInstruction arg =  new CodeInstruction(OpCodes.Ldloc_2);
                            CodeInstruction call = new CodeInstruction(OpCodes.Call, patcherMethod);

                            instruction.MoveLabelsTo(arg);

                            yield return arg;
                            yield return call;
                            done = true;
                        }
                    }

                    yield return instruction;
                }
            }
        }
    }
}

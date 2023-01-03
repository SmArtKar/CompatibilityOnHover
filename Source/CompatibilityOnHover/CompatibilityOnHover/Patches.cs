using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Noise;

namespace CompatibilityOnHover
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            Harmony harmony = new Harmony(id: "rimworld.smartkar.compatibilityonhover.main");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(Page_ConfigureStartingPawns), "DrawPawnList")]
        public class Page_ConfigureStartingPawns_DrawPawnList_Transpiler
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
            {
                var code = new List<CodeInstruction>(instructions);
                LocalBuilder floatLocal = ilg.DeclareLocal(typeof(float));

                int insertionIndex = -1;
                for (int i = 0; i < code.Count - 1; i++) // -1 since we will be checking i + 1
                {
                    if (code[i].opcode == OpCodes.Ldstr && (string)code[i].operand == "DragToReorder")
                    {
                        insertionIndex = i;
                        break;
                    }
                }

                code.RemoveAt(insertionIndex);

                List<CodeInstruction> instructionsToInsert = new List<CodeInstruction>();

                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldstr, "DragToReorderCOH"));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldloc_2));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Verse.Pawn), nameof(Pawn.relations))));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldarg_0));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(RimWorld.Page_ConfigureStartingPawns), "curPawn")));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(RimWorld.Pawn_RelationsTracker), "CompatibilityWith", new Type[] { typeof(Verse.Pawn) })));

                instructionsToInsert.Add(new CodeInstruction(OpCodes.Stloc_S, floatLocal.LocalIndex));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldloca_S, floatLocal.LocalIndex));

                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldstr, "F2"));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(System.Single), "ToString", new Type[] { typeof(string) })));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Verse.NamedArgument), "op_Implicit", new Type[] { typeof(string) })));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Verse.TranslatorFormattedStringExtensions), "Translate", new Type[] { typeof(string), typeof(Verse.NamedArgument) })));

                if (insertionIndex != -1)
                {
                    code.RemoveAt(insertionIndex);
                    code.InsertRange(insertionIndex, instructionsToInsert);
                }

                return code;
            }
        }
    }
}

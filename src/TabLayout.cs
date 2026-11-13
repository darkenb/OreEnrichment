using System;
using System.Collections.Generic;
using System.Linq;
using CommonAPI.Systems;
using HarmonyLib;
using UnityEngine;

namespace OreEnrichment
{
    // This mod's tabs show only where they have something: in the replicator once one of their recipes is unlocked, in the
    // item filter (GenesisBook adds a button per tab there) only when they hold items. Later tabs close up the gaps.
    public static class TabLayout
    {
        internal static Func<IEnumerable<int>> Own = () => new int[0];

        // Slot of a visible tab once the hidden tabs before it are taken out.
        public static int Slot(int index, ICollection<int> hidden) => index - hidden.Count(h => h < index);

        static void Arrange(IEnumerable<UITabButton> buttons, HashSet<int> hidden, Func<int, Vector2> at)
        {
            foreach (var b in buttons)
            {
                bool show = !hidden.Contains(b.tabIndex);
                if (!show) b.gameObject.SetActive(false);
                ((RectTransform)b.transform).anchoredPosition = at(Slot(b.tabIndex, hidden));
            }
        }

        static HashSet<int> itemless;

        [HarmonyPostfix, HarmonyPatch(typeof(UILootFilter), "RefreshWindow")]
        [HarmonyAfter("org.LoShin.GenesisBook", "aaa.bug.dsp.gbcompat")]
        static void LootFilter(UILootFilter __instance)
        {
            try
            {
                if (Traverse.Create(__instance).Field<bool>("showDropOnly").Value) return;
                var buttons = __instance.filterTrans.GetComponentsInChildren<UITabButton>(true);
                if (buttons.Length == 0) return;
                if (itemless == null)
                {
                    var used = new HashSet<int>(LDB.items.dataArray.Where(i => i != null && i.GridIndex >= 1101).Select(i => i.GridIndex / 1000));
                    itemless = new HashSet<int>(Own().Where(t => !used.Contains(t)));
                }
                // GenesisBook's places: x = slot * 70 - 54, the enemy drop button in the slot after the last tab.
                Arrange(buttons, itemless, slot => new Vector2(slot * 70 - 54, -72f));
                int last = buttons.Where(b => !itemless.Contains(b.tabIndex)).Select(b => Slot(b.tabIndex, itemless)).DefaultIfEmpty(2).Max();
                __instance.typeButton3.transform.localPosition = new Vector3((last + 1) * 70 - 54, -40f, 0f);
            }
            catch (Exception e) { Plugin.Log.LogWarning("Item filter tabs left as they are: " + e.Message); }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIReplicatorWindow), "_OnOpen")]
        [HarmonyAfter("dsp.common-api.CommonAPI")]
        static void Replicator(UIReplicatorWindow __instance)
        {
            try
            {
                var buttons = __instance.GetComponentsInChildren<UITabButton>(true);
                if (buttons.Length == 0) return;
                var history = GameMain.history;
                var open = new HashSet<int>(LDB.recipes.dataArray.Where(r => r != null && r.GridIndex >= 1101 && history.RecipeUnlocked(r.ID)).Select(r => r.GridIndex / 1000));
                var hidden = new HashSet<int>(Own().Where(t => !open.Contains(t)));
                foreach (var b in buttons) if (!hidden.Contains(b.tabIndex)) b.gameObject.SetActive(true);
                // CommonAPI's places in the replicator: x = 70 * tab - 95.
                Arrange(buttons, hidden, slot => new Vector2(70 * slot - 95, 50f));
                var t = Traverse.Create(__instance);
                if (hidden.Contains(t.Field<int>("currentType").Value)) AccessTools.Method(typeof(UIReplicatorWindow), "OnTypeButtonClick").Invoke(__instance, new object[] { 1 });
            }
            catch (Exception e) { Plugin.Log.LogWarning("Replicator tabs left as they are: " + e.Message); }
        }
    }
}

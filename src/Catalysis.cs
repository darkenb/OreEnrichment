using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace OreEnrichment
{
    // A catalysed copy of every other productive recipe the loaded game has, vanilla or modded: three batches of the
    // inputs and one catalyst give four batches of the products. The copy runs in the same building as its original.
    // Buildings, recipes the game bars from extra products, and anything that would close a production loop get none.
    public static class Catalysis
    {
        public const int IdBase = 10000, MaxSourceId = 1999;  // copy id = IdBase + original id; the game's icon table ends at 12000
        public const int TechBase = 1975, Batches = 3, Gain = 4, CellsPerTab = 14 * 7;
        public static readonly int[] Catalysts = { 1114, 1120, 1123, 1124, 1127 };  // refined oil, hydrogen, graphene, carbon nanotube, strange matter

        static readonly string[] TechNames = { "Catalysis I", "Catalysis II", "Catalysis III", "Catalysis IV", "Catalysis V" };
        static readonly Dictionary<int, RecipeProto> sources = new Dictionary<int, RecipeProto>();  // original id -> original
        static readonly Dictionary<int, Sprite> icons = new Dictionary<int, Sprite>();

        // inputs x3 plus one catalyst, outputs x4
        public static void Scale(int[] items, int[] counts, int[] resultCounts, int catalyst, out int[] items2, out int[] counts2, out int[] resultCounts2)
        {
            items2 = new int[items.Length + 1];
            counts2 = new int[items.Length + 1];
            for (int i = 0; i < items.Length; i++) { items2[i] = items[i]; counts2[i] = counts[i] * Batches; }
            items2[items.Length] = catalyst;
            counts2[items.Length] = 1;
            resultCounts2 = new int[resultCounts.Length];
            for (int i = 0; i < resultCounts.Length; i++) resultCounts2[i] = resultCounts[i] * Gain;
        }

        // True when something made (directly or through any chain of recipes) from `from` is one of `targets`.
        public static bool Reaches(Dictionary<int, HashSet<int>> makes, IEnumerable<int> from, ICollection<int> targets)
        {
            var seen = new HashSet<int>();
            var todo = new Stack<int>(from);
            while (todo.Count > 0)
            {
                int item = todo.Pop();
                if (!seen.Add(item)) continue;
                if (targets.Contains(item)) return true;
                if (makes.TryGetValue(item, out var next)) foreach (int n in next) todo.Push(n);
            }
            return false;
        }

        // Stage 0-4 from how deep in the tech tree the recipe's research sits, in fifths of the deepest recipe research.
        // Matrix kinds would not do: content mods use their own matrices, in their own combinations.
        public static int Stage(int depth, int deepest) => Math.Max(0, Math.Min(Catalysts.Length - 1, depth * Catalysts.Length / (deepest + 1)));

        // ---- Icons ----
        // The game draws grid icons from one atlas of 625 cells, sized in its shaders, and GenesisBook nearly fills it.
        // So a copy never takes a cell: while the atlas is built it carries its original's picture (same cell), and
        // the flask badge is laid over the cell as a separate image. Outside the grids (tooltip, building window) the
        // copy shows a picture with the badge drawn in.

        internal static Sprite IconFor(int sourceId)
        {
            if (icons.TryGetValue(sourceId, out var sprite)) return sprite;
            sources.TryGetValue(sourceId, out var source);
            return icons[sourceId] = source?.iconSprite ?? LDB.items.Select(source?.Results[0] ?? 0)?.iconSprite;
        }

        // Recipes drawn as another picture with a badge: the catalysed copies here, the gold and silver recipes (Precious).
        static readonly Dictionary<int, string> badgeOf = new Dictionary<int, string>();
        static readonly List<KeyValuePair<RecipeProto, Func<Sprite>>> marked = new List<KeyValuePair<RecipeProto, Func<Sprite>>>();
        static readonly Dictionary<int, Sprite> badged = new Dictionary<int, Sprite>();

        internal static void Mark(RecipeProto r, Func<Sprite> picture, string badge)
        {
            badgeOf[r.ID] = badge;
            marked.Add(new KeyValuePair<RecipeProto, Func<Sprite>>(r, picture));
        }

        static void SetSprites(bool withBadge)
        {
            foreach (var kv in marked)
            {
                var r = kv.Key;
                var sprite = kv.Value();
                if (withBadge && sprite != null)
                {
                    if (!badged.TryGetValue(r.ID, out var b)) badged[r.ID] = b = Plugin.Badged(sprite, badgeOf[r.ID]);
                    sprite = b;
                }
                Traverse.Create(r).Field("_iconSprite").SetValue(sprite);
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(IconSet), nameof(IconSet.Create))]
        static void BeforeAtlas() => SetSprites(false);

        [HarmonyPostfix, HarmonyPatch(typeof(IconSet), nameof(IconSet.Create))]
        static void AfterAtlas()
        {
            try { SetSprites(true); }
            catch (Exception e) { Plugin.Log.LogWarning("Badged recipes keep plain pictures: " + e.Message); }
        }

        static readonly Dictionary<RectTransform, List<Image>> overlays = new Dictionary<RectTransform, List<Image>>();

        // One badge image per shown copy, placed by the recipe's own grid cell (46 wide, icons inset by 3).
        static void DrawBadges(RectTransform grid, RecipeProto[] shown)
        {
            if (grid == null || shown == null) return;
            if (!overlays.TryGetValue(grid, out var pool)) overlays[grid] = pool = new List<Image>();
            int used = 0;
            foreach (var p in shown)
            {
                if (p == null || !badgeOf.TryGetValue(p.ID, out string badge)) continue;
                if (used == pool.Count)
                {
                    var img = new GameObject("catalysis-badge", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    img.transform.SetParent(grid, false);
                    img.rectTransform.anchorMin = img.rectTransform.anchorMax = img.rectTransform.pivot = new Vector2(0f, 1f);
                    img.rectTransform.sizeDelta = new Vector2(40f, 40f);
                    img.raycastTarget = false;
                    pool.Add(img);
                }
                int col = p.GridIndex % 100 - 1, row = p.GridIndex % 1000 / 100 - 1;
                pool[used].sprite = Plugin.Icon(badge);
                pool[used].rectTransform.anchoredPosition = new Vector2(col * 46 + 3, -row * 46 - 3);
                pool[used].gameObject.SetActive(true);
                used++;
            }
            for (int i = used; i < pool.Count; i++) pool[i].gameObject.SetActive(false);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIRecipePicker), "RefreshIcons")]
        static void PickerBadges(UIRecipePicker __instance, RecipeProto[] ___protoArray) => DrawBadges(__instance.iconImage?.rectTransform, ___protoArray);

        [HarmonyPostfix, HarmonyPatch(typeof(UIReplicatorWindow), "RefreshRecipeIcons")]
        static void ReplicatorBadges(UIReplicatorWindow __instance, RecipeProto[] ___recipeProtoArray) =>
            DrawBadges(Above(__instance.recipeBg?.rectTransform, __instance.recipeIcons?.transform), ___recipeProtoArray);

        static readonly Dictionary<RectTransform, RectTransform> layers = new Dictionary<RectTransform, RectTransform>();

        // The replicator draws its icons in a separate image after the cell background, so badges parented to the
        // background end up behind the items. They go into a layer right after the icon image, on the background's rect.
        static RectTransform Above(RectTransform bg, Transform icons)
        {
            if (bg == null || icons == null || icons.parent == null) return bg;
            if (!layers.TryGetValue(bg, out var layer) || layer == null)
            {
                layer = (RectTransform)new GameObject("catalysis-badges", typeof(RectTransform)).transform;
                layer.SetParent(icons.parent, false);
                layers[bg] = layer;
            }
            layer.SetSiblingIndex(icons.GetSiblingIndex() + 1);
            layer.localScale = Vector3.one;
            layer.localRotation = Quaternion.identity;
            if (bg.parent == layer.parent)
            {
                layer.anchorMin = bg.anchorMin; layer.anchorMax = bg.anchorMax; layer.pivot = bg.pivot;
                layer.sizeDelta = bg.sizeDelta; layer.anchoredPosition = bg.anchoredPosition;
            }
            else
            {
                layer.anchorMin = layer.anchorMax = new Vector2(0.5f, 0.5f);
                layer.pivot = bg.pivot;
                layer.sizeDelta = bg.rect.size;
                layer.position = bg.position;
            }
            var p = layer.localPosition;
            layer.localPosition = new Vector3(p.x, p.y, 0f);  // the UI camera is perspective: stay on the canvas plane
            return layer;
        }

        internal static void Generate(HashSet<int> skip, int[] tabs, float x0, float dx, float y)
        {
            var techDepth = new Dictionary<int, int>();
            int Depth(TechProto t)
            {
                if (t == null) return 0;
                if (techDepth.TryGetValue(t.ID, out int known)) return known;
                techDepth[t.ID] = 0;  // guards against a cycle
                int d = 0;
                foreach (int[] pres in new[] { t.PreTechs, t.PreTechsImplicit })
                    if (pres != null) foreach (int pre in pres) d = Math.Max(d, Depth(LDB.techs.Select(pre)) + 1);
                return techDepth[t.ID] = d;
            }
            var depth = new Dictionary<int, int>();  // recipe id -> depth of its earliest research
            int deepest = 0;
            foreach (var t in LDB.techs.dataArray)
            {
                if (t?.UnlockRecipes == null || t.UnlockRecipes.Length == 0 || t.page != 0) continue;
                int d = Depth(t);
                deepest = Math.Max(deepest, d);
                foreach (int id in t.UnlockRecipes) depth[id] = depth.TryGetValue(id, out int had) ? Math.Min(had, d) : d;
            }
            var makes = new Dictionary<int, HashSet<int>>();
            void Edge(int from, int[] to)
            {
                if (!makes.TryGetValue(from, out var set)) makes[from] = set = new HashSet<int>();
                foreach (int r in to) set.Add(r);
            }
            foreach (var r in LDB.recipes.dataArray)
                if (r?.Items != null && r.Results != null) foreach (int item in r.Items) Edge(item, r.Results);

            var originals = new List<RecipeProto>();
            foreach (var r in LDB.recipes.dataArray)
            {
                if (r == null || skip.Contains(r.ID) || r.ID > MaxSourceId || r.GridIndex < 1101 || r.NonProductive || !r.productive) continue;
                if (r.Items == null || r.Items.Length == 0 || r.Results == null || r.Results.Length == 0) continue;
                if (r.Items.Length + r.Results.Length + 1 > Precious.MaxIcons) continue;   // the catalyst would not show in recipe lists
                if (r.Type == ERecipeType.Fractionate || r.Type == ERecipeType.Exchange || r.Type == ERecipeType.PhotonStore || r.Type == ERecipeType.None) continue;
                bool building = false;
                foreach (int id in r.Results) { var item = LDB.items.Select(id); building |= item == null || item.CanBuild; }
                if (!building) originals.Add(r);
            }
            originals.Sort((a, b) => a.GridIndex != b.GridIndex ? a.GridIndex.CompareTo(b.GridIndex) : a.ID.CompareTo(b.ID));

            var unlocks = new List<int>[Catalysts.Length];
            for (int i = 0; i < unlocks.Length; i++) unlocks[i] = new List<int>();
            var copies = new List<RecipeProto>();
            int added = 0, loops = 0, noCatalyst = 0, noRoom = 0;
            foreach (var r in originals)
            {
                int stage = Stage(depth.TryGetValue(r.ID, out int d) ? d : 0, deepest), catalyst = 0;
                for (int k = stage; k >= 0 && catalyst == 0; k--)
                {
                    var item = LDB.items.Select(Catalysts[k]);
                    if (item != null && item.GridIndex > 0 && Array.IndexOf(r.Items, item.ID) < 0 && Array.IndexOf(r.Results, item.ID) < 0) catalyst = item.ID;
                }
                if (catalyst == 0) { noCatalyst++; continue; }
                Scale(r.Items, r.ItemCounts, r.ResultCounts, catalyst, out int[] items, out int[] counts, out int[] resultCounts);
                if (Reaches(makes, r.Results, items)) { loops++; continue; }
                if (LDB.recipes.Exist(IdBase + r.ID)) { noRoom++; continue; }
                if (added >= tabs.Length * CellsPerTab) { noRoom++; continue; }

                string product = LDB.items.Select(r.Results[0])?.name ?? "", catalystName = LDB.items.Select(catalyst).name;
                string name = (string.IsNullOrEmpty(r.name) ? product : r.name) + Lang.T(" (Catalysed)");
                var copy = new RecipeProto
                {
                    ID = IdBase + r.ID,
                    Name = name,
                    Description = Lang.F("{0} {1} from three batches of the inputs and 1 {2}. The normal recipe makes {3} from the same inputs.\nRuns in the same building as the normal recipe.",
                        resultCounts[0], product, catalystName, r.ResultCounts[0] * Batches),
                    Type = r.Type,
                    Handcraft = false,
                    Explicit = true,
                    TimeSpend = r.TimeSpend * Gain,  // the building keeps its pace per product
                    Items = items,
                    ItemCounts = counts,
                    Results = (int[])r.Results.Clone(),
                    ResultCounts = resultCounts,
                    GridIndex = tabs[added / CellsPerTab] * 1000 + (added % CellsPerTab / 14 + 1) * 100 + added % 14 + 1,
                    IconPath = Plugin.IconRoot + "cat/" + r.ID,
                    IconTag = "",
                    NonProductive = false,
                };
                sources[r.ID] = r;
                copies.Add(copy);
                Edge(catalyst, r.Results);
                unlocks[stage].Add(copy.ID);
                added++;
            }

            var techs = new TechProto[Catalysts.Length];
            for (int i = 0; i < techs.Length; i++)
            {
                string catalystName = LDB.items.Select(Catalysts[i])?.name ?? "a catalyst";
                techs[i] = Plugin.NewTech(TechBase + i, Lang.T(TechNames[i]),
                    Lang.F("Catalysed copies of recipes: three batches of the inputs and one catalyst give four batches of the products, in the same building. This stage covers recipes of its research level and uses {0} where the recipe does not already contain it.", catalystName),
                    Plugin.IconRoot + "tab-cat", i == 0 ? 0 : techs[i - 1].ID, Plugin.TechUnlocking(Catalysts[i]), i + 1, unlocks[i].ToArray(), new Vector2(x0 + i * dx, y));
            }
            // Recipes first, unloaded: the techs' own load looks their recipes up, and a recipe's load looks its tech up.
            int first = Plugin.Append(LDB.recipes, copies);
            Plugin.Insert(techs);
            int noIcon = 0;
            for (int i = 0; i < copies.Count; i++)
            {
                copies[i].Preload(first + i);
                if (copies[i].iconSprite == null) noIcon++;
            }
            RecipeProto.InitRecipeItems();  // the game's per-recipe table, built before these existed
            foreach (var copy in copies)
            {
                int source = copy.ID - IdBase;
                Mark(copy, () => IconFor(source), "badge-cat");
            }
            Plugin.Log.LogInfo("Catalysed copies: " + added + " of " + originals.Count + " recipes (" + loops + " would close a loop, " + noCatalyst + " have no free catalyst, " + noRoom
                + " no free id or cell); per stage " + unlocks[0].Count + "/" + unlocks[1].Count + "/" + unlocks[2].Count + "/" + unlocks[3].Count + "/" + unlocks[4].Count + "; " + noIcon + " copies without icon");
        }
    }
}

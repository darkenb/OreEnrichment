using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace OreEnrichment
{
    // A recipe made from the normal recipe of a product, as the loaded game has it (content mods change those):
    // every normal input x BatchesIn (an ore may be swapped for its dust), plus extra items, gives every product x BatchesOut.
    public sealed class Variant
    {
        public int Id, Product, Ore, Dust;        // Ore/Dust: the normal recipe is the one-input recipe from Ore; Dust goes in instead
        public int BatchesIn, BatchesOut, Type;   // Type 0: the building of the normal recipe
        public int[] Extra, ExtraN;               // ExtraN 0: computed from the size of the normal recipe (Precious.Amount)
        public int Per;                           // input units one extra unit goes with, when ExtraN is 0
        public int Tab, Row, Col, Tech;           // Tab 0: precious tab, 1: enrichment tab; Tech: index into Precious techs
        public string Badge, Suffix;
    }

    // Gold and silver from copper, as on Earth: electrolytic refining of copper leaves anode slime, leaching the slime
    // gives gold and silver dust, smelted into ingots. They go into extra-yield copies of electronics and power buildings.
    // With GenesisBook its fluids add more: nitric acid and hydrochloric acid for the metals, and fluid smelting of dust.
    public static class Precious
    {
        public const int Slime = 9457, GoldDust = 9458, SilverDust = 9459, GoldIngot = 9460, SilverIngot = 9461;
        public static readonly int[] Items = { Slime, GoldDust, SilverDust, GoldIngot, SilverIngot };
        public static readonly string[] ItemNames = { "Anode Slime", "Gold Dust", "Silver Dust", "Gold Ingot", "Silver Ingot" };
        public static readonly string[] ItemIcons = { "anode-slime", "gold-dust", "silver-dust", "gold-ingot", "silver-ingot" };
        public const int ChainBase = 9601, VariantBase = 9620, TechBase = 1980;
        public const int RuleBase = 7000;  // rule variant id = RuleBase + id of the recipe it copies (<= 1999), so saves keep it

        // GenesisBook items; a recipe with one of them exists only while that item does.
        public const int Oxygen = 7019, CO2 = 6206, HCl = 7014, Ammonia = 7002, NaOH = 7020, Seawater = 7018, Nitric = 7017, Salt = 7101;

        // Techs: 0 recovery (chain), 1 electronics, 2 power buildings, 3 fluid chemistry (GenesisBook only)
        public static readonly string[] TechNames = { "Precious Metal Recovery", "Gold and Silver Electronics", "Gold and Silver Construction", "Fluid Chemistry" };

        // The fixed chain. Row/Col on the precious tab (Tech 3 washing: enrichment tab row 7).
        public static List<Alt> Chain()
        {
            int cu = Tables.Dust(Tables.Copper);
            var all = new List<Alt>
            {
                new Alt { Id = ChainBase, Type = Tables.Chemical, Time = 240, Row = 1, Col = 1, Name = "Electrolytic Copper Refining",
                    In = new[] { cu, Tables.Acid }, InN = new[] { 4, 1 }, Out = new[] { Slime, 1104 }, OutN = new[] { 2, 4 } },
                new Alt { Id = ChainBase + 1, Type = Tables.Chemical, Time = 180, Row = 1, Col = 2, Name = "Anode Slime Leaching",
                    In = new[] { Slime, Tables.Acid }, InN = new[] { 2, 1 }, Out = new[] { GoldDust, SilverDust }, OutN = new[] { 1, 2 } },
                new Alt { Id = ChainBase + 2, Type = Tables.Smelt, Time = 90, Row = 1, Col = 3, Name = "Gold Smelting",
                    In = new[] { GoldDust }, InN = new[] { 1 }, Out = new[] { GoldIngot }, OutN = new[] { 1 } },
                new Alt { Id = ChainBase + 3, Type = Tables.Smelt, Time = 90, Row = 1, Col = 4, Name = "Silver Smelting",
                    In = new[] { SilverDust }, InN = new[] { 1 }, Out = new[] { SilverIngot }, OutN = new[] { 1 } },
                // GenesisBook: silver parting with nitric acid recovers more of both; Wohlwill electrolysis refines gold in hydrochloric acid.
                new Alt { Id = ChainBase + 4, Type = Tables.Chemical, Time = 180, Row = 1, Col = 5, Tech = 3, Name = "Nitric Acid Parting", Badge = "badge-fluid",
                    In = new[] { Slime, Nitric }, InN = new[] { 2, 1 }, Out = new[] { GoldDust, SilverDust }, OutN = new[] { 2, 3 } },
                new Alt { Id = ChainBase + 5, Type = Tables.Chemical, Time = 180, Row = 1, Col = 6, Tech = 3, Name = "Wohlwill Gold Refining", Badge = "badge-fluid",
                    In = new[] { GoldDust, HCl }, InN = new[] { 2, 1 }, Out = new[] { GoldIngot }, OutN = new[] { 3 } },
            };
            // GenesisBook: washing with seawater gives the same dust as water, and salt besides.
            for (int o = 0; o < Tables.Ores.Length; o++)
                all.Add(new Alt { Id = ChainBase + 6 + o, Type = Tables.Chemical, Time = 180, Row = 7, Col = o + 1, Tech = 3, Name = Tables.OreNames[o] + " Ore Seawater Washing", Badge = "badge-sea",
                    In = new[] { Tables.Ores[o], Seawater }, InN = new[] { 4, 2 }, Out = new[] { Tables.Dust(o), Salt }, OutN = new[] { 6, 1 } });
            return all;
        }

        public static bool OnEnrichmentTab(Alt a) => a.Row == 7;

        // The electronic parts themselves take one metal: dust x1.5, ingot x2, both x3 (from four batches).
        // GenesisBook: optical processor 7803, quantum computer 7805, "Beyond" X-1 7806.
        static readonly int[,] Components = { { 1202, SilverIngot }, { 1301, GoldIngot }, { 1302, SilverIngot }, { 1303, GoldIngot }, { 1205, SilverIngot }, { 1404, GoldIngot }, { 1305, GoldIngot },
            { 7803, GoldIngot }, { 7805, GoldIngot }, { 7806, GoldIngot } };
        public static readonly int[] ComponentOut = { 6, 8, 12 };
        public const int ComponentIn = 4, ComponentPer = 6, BuildingIn = 2, BuildingPer = 10;

        // Everything else made with these parts gets ingots of their metal: chips, boards and optics gold, coils, motors and
        // capacitors silver. Components x2 from four batches (x2.5 with both metals), buildings x1.5 from two (x2 with both).
        public static readonly int[] GoldParts = { 1301, 1302, 1303, 1305, 1402, 1404, 7803, 7805, 7806, 5201, 5202 };
        public static readonly int[] SilverParts = { 1202, 1203, 1204, 1205, 1206, 1401, 1403 };
        public static bool IsCore(int product)
        {
            for (int p = 0; p < Components.GetLength(0); p++) if (Components[p, 0] == product) return true;
            return false;
        }

        // UIRecipeEntry (item tooltips, recipe lists) draws at most 7 items per recipe, products and inputs together.
        public const int MaxIcons = 7;

        public static Variant Rule(int recipeId, int product, int[] items, bool building, int results = 1)
        {
            if (recipeId < 1 || recipeId > Catalysis.MaxSourceId || items == null || IsCore(product) || Array.IndexOf(Items, product) >= 0) return null;
            bool gold = false, silver = false;
            foreach (int i in items) { gold |= Array.IndexOf(GoldParts, i) >= 0; silver |= Array.IndexOf(SilverParts, i) >= 0; }
            if (!gold && !silver) return null;
            if (items.Length + results + 1 > MaxIcons) return null;
            if (gold && silver && items.Length + results + 2 > MaxIcons) silver = false;   // room for one metal only: gold
            bool both = gold && silver;
            var extra = both ? new[] { GoldIngot, SilverIngot } : new[] { gold ? GoldIngot : SilverIngot };
            return new Variant
            {
                Id = RuleBase + recipeId, Product = product, Extra = extra, ExtraN = new int[extra.Length],
                BatchesIn = building ? BuildingIn : ComponentIn, BatchesOut = building ? (both ? 4 : 3) : (both ? 10 : 8),
                Per = building ? BuildingPer : ComponentPer, Tech = building ? 2 : 1,
                Badge = both ? "badge-auag" : gold ? "badge-au2" : "badge-ag2", Suffix = both ? " (Gold and Silver)" : gold ? " (Gold)" : " (Silver)",
            };
        }
        public const int FluidIn = 4, FluidOut = 10, FluidAmount = 2;

        static int DustOf(int ingot) => ingot == GoldIngot ? GoldDust : SilverDust;
        static string Metal(int ingot) => ingot == GoldIngot ? "au" : "ag";

        public static List<Variant> Variants()
        {
            var all = new List<Variant>();
            int id = VariantBase;
            for (int p = 0; p < Components.GetLength(0); p++)
            {
                int product = Components[p, 0], ingot = Components[p, 1], dust = DustOf(ingot);
                var extras = new[] { new[] { dust }, new[] { ingot }, new[] { dust, ingot } };
                for (int v = 0; v < 3; v++)
                    all.Add(new Variant { Id = id++, Product = product, BatchesIn = ComponentIn, BatchesOut = ComponentOut[v], Extra = extras[v], ExtraN = new int[extras[v].Length],
                        Per = ComponentPer, Row = 2 + v, Col = p + 1, Tech = 1, Badge = "badge-" + Metal(ingot) + (v + 1), Suffix = new[] { " (Dust)", " (Ingot)", " (Dust and Ingot)" }[v] });
            }
            // GenesisBook fluids in enriched smelting: x2.5, between tier II and the rare-vein tier III.
            var fluids = new[]
            {
                new[] { 1103, 1101, 0, Oxygen, Tables.Smelt },                                  // basic oxygen steelmaking
                new[] { 1104, Tables.Ores[Tables.Copper], Tables.Dust(Tables.Copper), Ammonia, Tables.Chemical },  // ammonia leaching
                new[] { 1106, Tables.Ores[Tables.Titanium], Tables.Dust(Tables.Titanium), HCl, Tables.Chemical }, // chloride process
                new[] { 1105, Tables.Ores[Tables.Silicon], Tables.Dust(Tables.Silicon), HCl, Tables.Chemical },   // trichlorosilane
                new[] { 1110, Tables.Ores[Tables.Stone], Tables.Dust(Tables.Stone), NaOH, Tables.Smelt },         // soda glass
                new[] { 1108, Tables.Ores[Tables.Stone], Tables.Dust(Tables.Stone), CO2, Tables.Assemble },       // carbonated bricks
            };
            for (int f = 0; f < fluids.Length; f++)
                all.Add(new Variant { Id = id++, Product = fluids[f][0], Ore = fluids[f][1], Dust = fluids[f][2], BatchesIn = FluidIn, BatchesOut = FluidOut, Type = fluids[f][4],
                    Extra = new[] { fluids[f][3] }, ExtraN = new[] { FluidAmount }, Tab = 1, Row = 6, Col = f + 1, Tech = 3, Badge = "badge-fluid", Suffix = " (Fluid)" });
            return all;
        }

        // One extra unit per `per` input units, at least one.
        public static int Amount(int inputUnits, int per) => Math.Max(1, (inputUnits + per / 2) / per);

        // The variant's recipe from the normal one. False when the variant cannot be made from it.
        public static bool Make(Variant v, int[] items, int[] counts, int[] results, int[] resultCounts, out int[] items2, out int[] counts2, out int[] results2, out int[] resultCounts2)
        {
            items2 = counts2 = results2 = resultCounts2 = null;
            if (items == null || items.Length == 0 || results == null || results.Length == 0 || results[0] != v.Product) return false;
            var inItems = new List<int>();
            var inCounts = new List<int>();
            int units = 0;
            for (int i = 0; i < items.Length; i++)
            {
                int item = v.Dust != 0 && items[i] == v.Ore ? v.Dust : items[i];
                inItems.Add(item);
                inCounts.Add(counts[i] * v.BatchesIn);
                units += counts[i] * v.BatchesIn;
            }
            for (int e = 0; e < v.Extra.Length; e++)
            {
                if (inItems.Contains(v.Extra[e]) || Array.IndexOf(results, v.Extra[e]) >= 0) return false;  // already in the recipe
                inItems.Add(v.Extra[e]);
                inCounts.Add(v.ExtraN[e] > 0 ? v.ExtraN[e] : Amount(units, v.Per));
            }
            items2 = inItems.ToArray();
            counts2 = inCounts.ToArray();
            results2 = (int[])results.Clone();
            resultCounts2 = new int[resultCounts.Length];
            for (int i = 0; i < resultCounts.Length; i++) resultCounts2[i] = resultCounts[i] * v.BatchesOut;
            return true;
        }

        // ---- In the game ----

        static readonly Dictionary<int, int> plain = new Dictionary<int, int>();  // recipe id -> item whose picture it carries
        static readonly Dictionary<int, RecipeProto> copied = new Dictionary<int, RecipeProto>();  // recipe id -> recipe it was made from

        // Some mods give an item no picture and draw its recipe instead; the recipe's picture is the fallback.
        internal static Sprite IconFor(int recipeId)
        {
            var sprite = plain.TryGetValue(recipeId, out int item) ? LDB.items.Select(item)?.iconSprite : null;
            if (sprite == null && copied.TryGetValue(recipeId, out var source)) sprite = source.iconSprite;
            return sprite;
        }

        static RecipeProto Normal(Variant v, HashSet<int> mine)
        {
            RecipeProto best = null;
            foreach (var r in LDB.recipes.dataArray)
            {
                if (r?.Results == null || r.Items == null || r.Results.Length == 0 || r.Results[0] != v.Product || mine.Contains(r.ID) || r.ID >= Catalysis.IdBase) continue;
                if (r.ID >= Tables.CrushBase && r.ID < Tables.CrushBase + 200) continue;   // this mod's recipes
                if (v.Ore != 0 ? r.Items.Length != 1 || r.Items[0] != v.Ore : r.GridIndex < 1101) continue;
                if (best == null || r.ID < best.ID) best = r;
            }
            return best;
        }

        // Adds the chain, the variants and their techs after every mod's recipes are in place. Returns the new recipe ids.
        internal static HashSet<int> Generate(int preciousTab, int moreTab, int enrichTab, float x0, float dx, float y, int afterCrushing, int afterEnrichment)
        {
            var mine = new HashSet<int>();
            var made = new List<RecipeProto>();
            var alts = Chain();
            var variants = Variants();
            foreach (var a in alts) mine.Add(a.Id);
            foreach (var v in variants) mine.Add(v.Id);
            var unlocks = new List<int>[TechNames.Length];
            for (int i = 0; i < unlocks.Length; i++) unlocks[i] = new List<int>();
            bool Exists(int item) => LDB.items.Select(item) != null;

            var makes = new Dictionary<int, HashSet<int>>();
            void Edge(int[] from, int[] to)
            {
                foreach (int f in from)
                {
                    if (!makes.TryGetValue(f, out var set)) makes[f] = set = new HashSet<int>();
                    foreach (int t in to) set.Add(t);
                }
            }
            foreach (var r in LDB.recipes.dataArray) if (r?.Items != null && r.Results != null) Edge(r.Items, r.Results);

            int missing = 0, taken = 0, loops = 0, noRoom = 0;
            var cols = new Dictionary<string, Dictionary<int, int>>();  // per tab row group: product -> column, so missing products leave no gap
            int Column(string group, int key)
            {
                if (!cols.TryGetValue(group, out var c)) cols[group] = c = new Dictionary<int, int>();
                if (!c.TryGetValue(key, out int col)) c[key] = col = c.Count + 1;
                return col;
            }

            foreach (var a in alts)
            {
                bool ok = true;
                foreach (int i in a.In) ok &= Exists(i);
                foreach (int i in a.Out) ok &= Exists(i);
                if (!ok) { missing++; continue; }
                if (LDB.recipes.Exist(a.Id)) { taken++; continue; }
                bool washing = OnEnrichmentTab(a);
                int col = Column(washing ? "wash" : "chain", a.Id);
                string name = washing ? Lang.F("{0} Ore Seawater Washing", Lang.T(Tables.OreNames[a.Id - ChainBase - 6])) : Lang.T(a.Name);
                var r = New(a.Id, name, a.Type, a.Time, a.In, a.InN, a.Out, a.OutN, (washing ? enrichTab : preciousTab) * 1000 + a.Row * 100 + col);
                r.Description = Lang.T(Describe(a.Id));
                r.Handcraft = a.Type == (int)ERecipeType.Smelt && a.In.Length == 1 && (LDB.recipes.Select(3)?.Handcraft ?? false);
                Edge(a.In, a.Out);
                if (a.Badge != null) Mark(r, a.Out[0], a.Badge);
                made.Add(r);
                unlocks[a.Tech].Add(r.ID);
            }

            // A variant of `normal`; grid null: no free cell. False when it is left out.
            bool Add(Variant v, RecipeProto normal, Func<int> grid)
            {
                if (normal == null || !Make(v, normal.Items, normal.ItemCounts, normal.Results, normal.ResultCounts, out int[] items, out int[] counts, out int[] results, out int[] resultCounts)) { missing++; return false; }
                if (LDB.recipes.Exist(v.Id)) { taken++; return false; }
                if (Catalysis.Reaches(makes, results, items)) { loops++; return false; }
                int cell = grid();
                if (cell == 0) { noRoom++; return false; }
                string product = LDB.items.Select(v.Product).name;
                var r = New(v.Id, product + Lang.T(v.Suffix), v.Type != 0 ? v.Type : (int)normal.Type, normal.TimeSpend * v.BatchesOut, items, counts, results, resultCounts, cell);
                r.NonProductive = normal.NonProductive;   // buildings stay out of extra-product proliferation
                r.Handcraft = r.Type == normal.Type && normal.Handcraft;
                foreach (int e in v.Extra) r.Handcraft &= !(LDB.items.Select(e)?.IsFluid ?? true);
                int extraUnits = 0;
                var extraText = new List<string>();
                for (int e = 0; e < v.Extra.Length; e++)
                {
                    int k = Array.IndexOf(items, v.Extra[e]);
                    extraUnits += counts[k];
                    extraText.Add(counts[k] + " " + LDB.items.Select(v.Extra[e]).name);
                }
                r.Description = Lang.F("{0} {1} from {2} batches of the normal inputs and {3}. The normal recipe makes {4} from the same inputs.",
                    resultCounts[0], product, v.BatchesIn, string.Join(Lang.T(" and "), extraText.ToArray()), normal.ResultCounts[0] * v.BatchesIn)
                    + "\n" + Lang.F("Each extra unit gives {0} more {1}.", ((resultCounts[0] - normal.ResultCounts[0] * v.BatchesIn) / (double)extraUnits).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture), product);
                Edge(items, results);
                copied[r.ID] = normal;
                Mark(r, v.Product, v.Badge);
                made.Add(r);
                unlocks[v.Tech].Add(r.ID);
                return true;
            }

            foreach (var v in variants)
            {
                bool ok = Exists(v.Product);
                foreach (int e in v.Extra) ok &= Exists(e);
                var normal = ok ? Normal(v, mine) : null;
                int tab = v.Tab == 1 ? enrichTab : preciousTab;
                Add(v, normal, () => tab * 1000 + v.Row * 100 + Column(v.Tab + ":" + (v.Row >= 2 && v.Row <= 4 ? 2 : v.Row), v.Product));
            }

            // Every other recipe with electronic parts: components first, then buildings, each in the game's grid order.
            // Cells: rows 5-7 of the precious tab, then the second precious tab.
            var cells = new List<int>();
            for (int row = 5; row <= 7; row++) for (int c = 1; c <= 14; c++) cells.Add(preciousTab * 1000 + row * 100 + c);
            for (int row = 1; row <= 7; row++) for (int c = 1; c <= 14; c++) cells.Add(moreTab * 1000 + row * 100 + c);
            var sources = new List<KeyValuePair<RecipeProto, Variant>>();
            foreach (var r in LDB.recipes.dataArray)
            {
                if (r?.Items == null || r.Results == null || r.Results.Length == 0 || r.GridIndex < 1101 || mine.Contains(r.ID)) continue;
                int row = r.GridIndex % 1000 / 100;
                if (row < 1 || row > 8 || r.GridIndex % 100 == 0) continue;   // hidden: no replicator row shows it (MoreMegaStructure's receiver forms sit on row 0)
                if (r.Type == ERecipeType.Fractionate || r.Type == ERecipeType.Exchange || r.Type == ERecipeType.PhotonStore || r.Type == ERecipeType.None) continue;
                var product = LDB.items.Select(r.Results[0]);
                if (product == null) continue;
                var v = Rule(r.ID, product.ID, r.Items, product.CanBuild, r.Results.Length);
                if (v != null && Exists(v.Extra[0])) sources.Add(new KeyValuePair<RecipeProto, Variant>(r, v));
            }
            sources.Sort((a, b) => a.Value.Tech != b.Value.Tech ? a.Value.Tech.CompareTo(b.Value.Tech) : a.Key.GridIndex != b.Key.GridIndex ? a.Key.GridIndex.CompareTo(b.Key.GridIndex) : a.Key.ID.CompareTo(b.Key.ID));
            int next = 0, ruled = 0;
            foreach (var kv in sources)
                if (Add(kv.Value, kv.Key, () => next < cells.Count ? cells[next++] : 0)) ruled++;

            int[] gates = { Plugin.TechUnlocking(Tables.Acid), Plugin.TechUnlocking(1303), Plugin.TechUnlocking(1303), Gate(Nitric, HCl, Ammonia, Oxygen) };
            var techs = new List<TechProto>();
            for (int i = 0; i < TechNames.Length; i++)
            {
                if (unlocks[i].Count == 0) continue;   // fluid chemistry without GenesisBook
                int after = i == 0 ? afterCrushing : i == 3 ? afterEnrichment : TechBase + i - 1;
                var t = Plugin.NewTech(TechBase + i, Lang.T(TechNames[i]), Lang.T(TechDescs[i]), Plugin.IconRoot + (i == 3 ? "tab" : "tab-precious"), after,
                    gates[i], i + 2, unlocks[i].ToArray(), new Vector2(x0 + i * dx, y));
                techs.Add(t);
            }
            int first = Plugin.Append(LDB.recipes, made);
            Plugin.Insert(techs.ToArray());
            for (int i = 0; i < made.Count; i++) made[i].Preload(first + i);
            var noIcon = new List<string>();
            foreach (var r in made) if (r.iconSprite == null) noIcon.Add(r.ID + " " + r.Name);
            if (noIcon.Count > 0) Plugin.Log.LogWarning("Gold and silver recipes without a picture: " + string.Join(", ", noIcon.ToArray()));
            Plugin.Log.LogInfo("Gold and silver: " + made.Count + " recipes (" + ruled + " of " + sources.Count + " recipes with electronic parts) and " + techs.Count + " techs; "
                + missing + " left out (no such item or normal recipe here), " + taken + " ids taken, " + loops + " would close a loop, " + noRoom + " no free cell");
            foreach (var r in made) mine.Add(r.ID);
            return mine;
        }

        static readonly string[] TechDescs =
        {
            "Refine copper electrolytically: the anode slime it leaves holds gold and silver. Leach the slime with sulfuric acid and smelt the dust into ingots.",
            "Gold and silver in electronic parts and in everything made with them: parts give up to three times the products, other components twice.",
            "Gold contacts and silver wiring in buildings, turrets and factories: two batches of the normal materials and a few ingots give three buildings.",
            "GenesisBook fluids: nitric acid and hydrochloric acid for gold and silver, oxygen, ammonia, lye and carbon dioxide in dust smelting, and seawater washing that also gives salt.",
        };

        // The latest of the techs that make these items: the tech can be researched once all of them exist.
        static int Gate(params int[] items)
        {
            int best = 0;
            foreach (int item in items)
            {
                int t = Plugin.TechUnlocking(item);
                var proto = t > 0 ? LDB.techs.Select(t) : null;
                if (proto != null && (best == 0 || proto.Position.x > LDB.techs.Select(best).Position.x)) best = t;
            }
            return best;
        }

        static string Describe(int id)
        {
            switch (id - ChainBase)
            {
                case 0: return "Copper dust refined in sulfuric acid gives as much copper as smelting, and anode slime besides.";
                case 1: return "Sulfuric acid dissolves the copper left in the slime; gold and silver stay behind as dust.";
                case 2: return "Gold dust smelted into ingots.";
                case 3: return "Silver dust smelted into ingots.";
                case 4: return "Nitric acid parts silver from gold: more of both from the same slime than sulfuric acid gives.";
                case 5: return "Gold dust refined by electrolysis in hydrochloric acid: three ingots from two dust.";
                default: return "Washing with seawater gives as much dust as washing with water, and salt besides.";
            }
        }

        internal static RecipeProto New(int id, string name, int type, int time, int[] items, int[] counts, int[] results, int[] resultCounts, int grid) => new RecipeProto
        {
            ID = id,
            Name = name,
            Description = "",
            Type = (ERecipeType)type,
            Handcraft = false,
            Explicit = true,
            TimeSpend = time,
            Items = items,
            ItemCounts = counts,
            Results = results,
            ResultCounts = resultCounts,
            GridIndex = grid,
            IconPath = "",
            IconTag = "",
            NonProductive = false,
        };

        // A recipe with the picture of an item and a badge laid over it (no atlas cell of its own; see Catalysis).
        internal static void Mark(RecipeProto r, int item, string badge)
        {
            plain[r.ID] = item;
            r.IconPath = Plugin.IconRoot + "pv/" + r.ID;
            Catalysis.Mark(r, () => IconFor(r.ID), badge);
        }

        // The game lists, per item, the recipes that make and use it; it builds those lists when items load, before any
        // of the recipes added here existed.
        internal static void RefreshItemRecipes()
        {
            var find = AccessTools.Method(typeof(ItemProto), "FindRecipes");
            foreach (var item in LDB.items.dataArray)
            {
                if (item == null) continue;
                item.recipes = null;
                find.Invoke(item, null);
            }
        }
    }
}

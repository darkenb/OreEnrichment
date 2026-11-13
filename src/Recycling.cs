using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OreEnrichment
{
    // Dark Fog drops turned into finished items: ammunition, matrices, fuel rods, Dyson sphere parts, combat units and a
    // few buildings. Each group is cut into levels from its simplest items to its hardest, every level with items of its
    // own. A recipe is worth what its drops are worth: every item costs its raw materials through the game's own recipes,
    // each drop has a fixed worth in the same raw materials, and a level takes as many drops as its hardest item needs,
    // the cheaper items coming in the numbers that match. It runs in the building the game makes its hardest item in.
    // Each level is its own tech, opened by the tech that unlocks its hardest item.
    public static class Recycling
    {
        public const int Base = 9656, Count = 44;  // 9656-9699: inside the range BUG_CalculatorPatch keeps as alternatives
        public const int TechId = 1984, LevelTechBase = 1850, MaxLevels = 3;  // main tech page ids stay below 2000; 1827-1899 is free in vanilla and GenesisBook

        // Dark Fog Matrix 5201, Silicon-based Neuron 5202, Matter Recombinator 5203, Negentropy Singularity 5204, Core Element 5205, Energy Shard 5206.
        public static readonly int[] AmmoSources = { 5201, 5202, 5203, 5204, 5206, 5205 };
        // A drop's worth in raw materials: an iron ore, a stone or a water each count 1. Rarer drops are worth more.
        public static readonly Dictionary<int, double> DropWorth = new Dictionary<int, double>
        {
            { 5201, 20 }, { 5202, 25 }, { 5203, 30 }, { 5204, 40 }, { 5205, 60 }, { 5206, 3 },
        };
        // Critical photons come from no recipe but take a Dyson sphere: priced, or antimatter would come cheap.
        public static readonly Dictionary<int, double> FixedCosts = DropWorth.Concat(new[] { new KeyValuePair<int, double>(1208, 8) }).ToDictionary(p => p.Key, p => p.Value);
        public static readonly int[] Dyson = { 1501, 1125, 1502, 1503, 2311, 2312 };      // sail, frame, component, rocket, EM rail ejector, vertical launching silo
        public static readonly int[] Units = { 5101, 5102, 5103, 5111, 5112 };            // ground and space fighters
        public static readonly int[] Power = { 2206, 2213, 2211, 2208, 2210, 6261, 6221 }; // accumulator ... artificial stars, isotope generator (GenesisBook)
        public static readonly int[] Buildings = { 2318, 2319, 3006 };                     // top assembler, top smelter, jammer tower

        public class Ammo { public int Id, Grid, Key; }
        public class Group
        {
            public string Name;  // English Lang key; null: the name of its first item
            public int Source, First = 1, Row, Slot;
            public List<int> Main = new List<int>(), Bonus = new List<int>();
        }
        public class Level { public Group Group; public int Number, InN, Hardest, Type; public int[] Out, OutN; public int Shown => Group.First + Number - 1; }
        public class Recipe { public int[] In, InN, Out, OutN; public int Type; }

        // Group of a round: its ammunition type, or its bomb type for bombs that are not ammunition.
        public static int Key(int ammoType, int bombType) => ammoType != 0 ? ammoType : bombType != 0 ? 100 + bombType : 0;

        static readonly Dictionary<int, string> AmmoNames = new Dictionary<int, string>
        {
            { 1, "Magazines" }, { 3, "Shells" }, { 4, "Plasma Capsules" }, { 5, "Missiles" }, { 6, "Jamming Capsules" }, { 102, "Explosive Units" },
        };

        // Raw-material cost of every item the recipes reach, and the building type of its cheapest recipe. Items no recipe
        // makes cost 1; the drops cost their worth. Settled by repeating until nothing gets cheaper, so loops do no harm.
        public static Dictionary<int, double> Costs(IList<Recipe> recipes, IDictionary<int, double> fixedCosts, Dictionary<int, int> types)
        {
            var cost = new Dictionary<int, double>(fixedCosts);
            var made = new HashSet<int>(recipes.SelectMany(r => r.Out));
            foreach (var r in recipes)
                foreach (int i in r.In)
                    if (!made.Contains(i) && !cost.ContainsKey(i)) cost[i] = 1;
            for (int pass = 0, changed = 1; pass < 200 && changed > 0; pass++)
            {
                changed = 0;
                foreach (var r in recipes)
                {
                    double sum = 0;
                    bool known = r.In.Length > 0;
                    for (int j = 0; j < r.In.Length && known; j++)
                        if (cost.TryGetValue(r.In[j], out double c)) sum += c * r.InN[j]; else known = false;
                    int outs = r.OutN.Sum();
                    if (!known || outs <= 0) continue;
                    double each = sum / outs;
                    foreach (int o in r.Out)
                        if (!fixedCosts.ContainsKey(o) && (!cost.TryGetValue(o, out double old) || each < old - 1e-9))
                        {
                            cost[o] = each;
                            types[o] = r.Type;
                            changed++;
                        }
                }
            }
            return cost;
        }

        // Rounds grouped by type, groups in grid order. A round standing apart from its group in the grid is a bonus.
        public static List<Group> AmmoGroups(IEnumerable<Ammo> ammo)
        {
            var groups = new List<Group>();
            foreach (var g in ammo.Where(a => a.Key != 0 && a.Grid > 0).GroupBy(a => a.Key).OrderBy(g => g.Min(a => a.Grid)).Take(AmmoSources.Length))
            {
                var group = new Group { Name = AmmoNames.TryGetValue(g.Key, out var n) ? n : null, Source = AmmoSources[groups.Count] };
                var sorted = g.OrderBy(a => a.Grid).ToList();
                for (int i = 0; i < sorted.Count; i++)
                {
                    bool apart = group.Bonus.Count > 0 || i > 0 && (sorted[i].Grid / 100 != sorted[i - 1].Grid / 100 || sorted[i].Grid % 100 > sorted[i - 1].Grid % 100 + 1);
                    (apart ? group.Bonus : group.Main).Add(sorted[i].Id);
                }
                groups.Add(group);
            }
            return groups;
        }

        // More than a recipe's six products: even parts, each from its own drop, levels numbered on from the part before.
        public static List<Group> Split(string name, IList<int> items, int[] sources)
        {
            var parts = new List<Group>();
            int n = items.Count, count = (n + Precious.MaxIcons - 2) / (Precious.MaxIcons - 1), first = 1, at = 0;
            for (int c = 0; c < count; c++)
            {
                int size = n / count + (c < n % count ? 1 : 0);
                var g = new Group { Name = name, Source = sources[Math.Min(c, sources.Length - 1)], First = first };
                g.Main.AddRange(items.Skip(at).Take(size));
                at += size;
                first += Math.Min(MaxLevels, size);
                parts.Add(g);
            }
            return parts;
        }

        // Three groups side by side per row of the tab and of the tech tree, one column per level; each list starts a new row.
        public static List<Group> Arrange(params List<Group>[] rows)
        {
            var all = new List<Group>();
            int row = 0;
            foreach (var r in rows)
                for (int i = 0; i < r.Count; i++)
                {
                    if (i % 3 == 0) row++;
                    r[i].Row = row;
                    r[i].Slot = i % 3;
                    all.Add(r[i]);
                }
            return all;
        }

        public static List<Level> Plan(IEnumerable<Group> groups, Func<int, double> cost, Func<int, int> type)
        {
            var levels = new List<Level>();
            foreach (var g in groups)
            {
                int n = g.Main.Count, count = Math.Min(MaxLevels, n), from = 0;
                for (int t = 1; t <= count; t++)
                {
                    int to = (n * t + count - 1) / count;
                    var band = g.Main.Skip(from).Take(to - from).ToList();
                    from = to;
                    if (t == count)
                        foreach (int b in g.Bonus)
                            if (band.Count + 1 < Precious.MaxIcons) band.Add(b);  // one icon is the input
                    double worth = DropWorth.TryGetValue(g.Source, out double w) ? w : 20, most = band.Max(cost);
                    int inN = Math.Max(1, (int)Math.Ceiling(band.Count * most / worth - 1e-9));
                    double share = inN * worth / band.Count;  // each item gets an even part of the drops' worth
                    int hardest = band.First(i => cost(i) == most);
                    levels.Add(new Level { Group = g, Number = t, InN = inN, Out = band.ToArray(), Hardest = hardest, Type = type(hardest),
                        OutN = band.Select(i => Math.Max(1, (int)Math.Round(share / cost(i)))).ToArray() });
                }
            }
            return levels;
        }

        // The groups of the loaded game, in tab order: two rows of ammunition, then matrices with Dyson parts, fuel rods
        // with combat units, power buildings with the other buildings. Every group but the ammunition goes cheapest first.
        public static List<Group> Groups(IList<ItemProto> items, Func<int, double> cost)
        {
            Func<IEnumerable<int>, List<int>> cheapest = ids => ids.OrderBy(cost).ToList();
            Func<int[], List<int>> present = ids => cheapest(ids.Where(id => items.Any(i => i.ID == id)));
            var ammo = AmmoGroups(items.Where(i => (i.Type == EItemType.Product || i.Type == EItemType.Component) && (i.ID < 5201 || i.ID > 5206))  // GenesisBook gives water a bomb type
                .Select(i => new Ammo { Id = i.ID, Grid = i.GridIndex, Key = Key((int)i.AmmoType, (int)i.BombType) }));
            var matrices = Split("Matrices", cheapest(items.Where(i => i.Type == EItemType.Matrix).Select(i => i.ID)), new[] { 5201, 5204 });
            var fuel = Split("Fuel Rods", cheapest(items.Where(i => i.Type == EItemType.Product && i.HeatValue > 0).Select(i => i.ID)), new[] { 5206, 5205 });
            var dyson = Split("Dyson Sphere", present(Dyson), new[] { 5203 });
            var units = Split("Combat Units", present(Units), new[] { 5202 });
            var power = Split("Power Facilities", present(Power), new[] { 5205, 5204 });
            var buildings = Split("Advanced Buildings", present(Buildings), new[] { 5203 });
            return Arrange(ammo, matrices.Concat(dyson).ToList(), fuel.Concat(units).ToList(), power.Concat(buildings).ToList());
        }

        static readonly string[] Roman = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
        static string Number(int n) => n < Roman.Length ? Roman[n] : n.ToString();

        // This mod's own recipes (copies, enrichment, gold and silver, catalysis) do not set what an item costs.
        static bool Mine(int id) => (id > 7000 && id < 9000) || (id >= 9501 && id <= 9699) || (id > 10000 && id < 12000);

        internal static void GenerateLevels(int tab, float x0, float dx, float y0, float dy)
        {
            var recipes = LDB.recipes.dataArray.Where(x => x?.Items != null && x.Results != null && x.ItemCounts != null && x.ResultCounts != null && !Mine(x.ID))
                .Select(x => new Recipe { In = x.Items, InN = x.ItemCounts, Out = x.Results, OutN = x.ResultCounts, Type = (int)x.Type }).ToList();
            var types = new Dictionary<int, int>();
            var costs = Costs(recipes, FixedCosts, types);
            Func<int, double> cost = id => costs.TryGetValue(id, out double c) ? c : double.PositiveInfinity;
            // Fractionators, power exchangers and Dyson swarms are no place for a recipe of several items: an assembler instead.
            Func<int, int> type = id => types.TryGetValue(id, out int t) && t != 0 && t != 6 && t != 7 && t != 8 ? t : Tables.Assemble;
            var items = LDB.items.dataArray.Where(i => i != null && i.GridIndex > 0 && !double.IsInfinity(cost(i.ID))).ToList();
            var plan = Plan(Groups(items, cost), cost, type);

            var makes = new Dictionary<int, HashSet<int>>();  // a drop some mod makes from these products must not close a loop
            foreach (var r in recipes)
                foreach (int f in r.In)
                {
                    if (!makes.TryGetValue(f, out var set)) makes[f] = set = new HashSet<int>();
                    foreach (int t in r.Out) set.Add(t);
                }
            var made = new List<RecipeProto>();
            var techs = new List<TechProto>();
            var last = new Dictionary<string, int>();
            var dropped = new HashSet<Group>();
            int skipped = 0;
            for (int i = 0; i < plan.Count; i++)
            {
                var l = plan[i];
                string key = l.Group.Name ?? "#" + l.Group.Main[0];
                int id = Base + i, techId = LevelTechBase + i;
                var source = LDB.items.Select(l.Group.Source);
                if (i >= Count || dropped.Contains(l.Group) || source == null || LDB.recipes.Exist(id) || LDB.techs.Exist(techId) || Catalysis.Reaches(makes, l.Out, new[] { l.Group.Source }))
                {
                    dropped.Add(l.Group);  // later levels and parts need this one
                    last.Remove(key);
                    skipped++;
                    continue;
                }
                if (l.Number == 1 && l.Group.First > 1 && !last.ContainsKey(key)) { dropped.Add(l.Group); skipped++; continue; }
                string group = l.Group.Name != null ? Lang.T(l.Group.Name) : LDB.items.Select(l.Group.Main[0]).name;
                int col = l.Group.Slot * MaxLevels + l.Number;
                var r = Precious.New(id, Lang.F("{0} Recycling: {1} {2}", source.name, group, Number(l.Shown)), l.Type, 60 * (l.Number + 1),
                    new[] { l.Group.Source }, new[] { l.InN }, l.Out, l.OutN, tab * 1000 + l.Group.Row * 100 + col);
                r.Description = Lang.T("Turns Dark Fog drops into finished items worth about as much as the drops; the harder the item, the more drops it takes.");
                Precious.Mark(r, l.Hardest, "badge-recycle");
                made.Add(r);
                int after = l.Number == 1 && l.Group.First == 1 ? TechId : last[key];
                var hardest = LDB.items.Select(l.Hardest);
                techs.Add(Plugin.NewTech(techId, Lang.F("Dark Fog Recycling: {0} {1}", group, Number(l.Shown)),
                    Lang.F("Turns {0} into {1}, in the building that makes them; each recipe is worth about as much as its drops.", source.name, group),
                    string.IsNullOrEmpty(hardest?.IconPath) ? Plugin.IconRoot + "tab-recycle" : hardest.IconPath,
                    after, Plugin.TechUnlocking(l.Hardest), Math.Min(6, l.Number + 1), new[] { id }, new Vector2(x0 + (col - 1) * dx, y0 - (l.Group.Row - 1) * dy)));
                last[key] = techId;
            }
            if (made.Count == 0) return;
            int first = Plugin.Append(LDB.recipes, made);
            Plugin.Insert(techs.ToArray());
            for (int i = 0; i < made.Count; i++) made[i].Preload(first + i);
            Plugin.Log.LogInfo("Dark Fog recycling: " + made.Count + " recipes and techs in " + plan.Select(p => p.Group).Distinct().Count() + " groups; " + skipped + " left out (ids taken, past " + Count + " or a loop); "
                + string.Join(", ", plan.Take(Count).Select(p => p.InN + "x" + p.Group.Source + "->" + string.Join("+", p.Out.Select((o, k) => p.OutN[k] + "x" + o).ToArray()) + "@" + p.Type).ToArray()));
        }

        // The root tech every recycling level follows.
        internal static void Generate(Vector2 position, int after)
        {
            Plugin.Insert(new[] { Plugin.NewTech(TechId, Lang.T("Dark Fog Recycling"),
                Lang.T("Opens the research that turns Dark Fog drops into ammunition, matrices, fuel rods, Dyson sphere parts, combat units and buildings."),
                Plugin.IconRoot + "tab-recycle", after, after, 2, new int[0], position) });
        }
    }
}

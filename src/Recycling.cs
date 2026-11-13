using System.Collections.Generic;
using UnityEngine;

namespace OreEnrichment
{
    // Dark Fog drops broken down into raw materials in an assembler (energy shards in a chemical plant).
    public static class Recycling
    {
        public const int Base = 9680, TechId = 1984;  // ids inside the gold and silver range that BUG_CalculatorPatch already knows

        // Dark Fog Matrix 5201, Silicon-based Neuron 5202, Matter Recombinator 5203, Negentropy Singularity 5204, Core Element 5205, Energy Shard 5206.
        public static List<Alt> All() => new List<Alt>
        {
            new Alt { Id = Base, Type = Tables.Assemble, Time = 120, Col = 1, Name = "Dark Fog Matrix Recycling",
                In = new[] { 5201 }, InN = new[] { 1 }, Out = new[] { 1001, 1002 }, OutN = new[] { 4, 4 } },
            new Alt { Id = Base + 1, Type = Tables.Assemble, Time = 120, Col = 2, Name = "Silicon-based Neuron Recycling",
                In = new[] { 5202 }, InN = new[] { 1 }, Out = new[] { 1003, 1013 }, OutN = new[] { 6, 1 } },
            new Alt { Id = Base + 2, Type = Tables.Assemble, Time = 120, Col = 3, Name = "Matter Recombinator Recycling",
                In = new[] { 5203 }, InN = new[] { 1 }, Out = new[] { 1004, 1014 }, OutN = new[] { 6, 1 } },
            new Alt { Id = Base + 3, Type = Tables.Assemble, Time = 120, Col = 4, Name = "Negentropy Singularity Recycling",
                In = new[] { 5204 }, InN = new[] { 1 }, Out = new[] { 1016, 1015 }, OutN = new[] { 4, 2 } },
            new Alt { Id = Base + 4, Type = Tables.Assemble, Time = 120, Col = 5, Name = "Core Element Recycling",
                In = new[] { 5205 }, InN = new[] { 1 }, Out = new[] { 1012, 1011 }, OutN = new[] { 4, 4 } },
            new Alt { Id = Base + 5, Type = Tables.Chemical, Time = 120, Col = 6, Name = "Energy Shard Recycling",
                In = new[] { 5206 }, InN = new[] { 10 }, Out = new[] { Tables.Hydrogen, 1121 }, OutN = new[] { 6, 2 } },
        };

        // Recipes whose items exist in the loaded game, on the recycling tab with no gaps, and their tech. Returns the new ids.
        internal static HashSet<int> Generate(int tab, Vector2 position, int after)
        {
            var mine = new HashSet<int>();
            var made = new List<RecipeProto>();
            int missing = 0, taken = 0, loops = 0;
            // A drop some mod makes from these products (GenesisBook makes energy shards from critical photons) must not close a loop.
            var makes = new Dictionary<int, HashSet<int>>();
            foreach (var x in LDB.recipes.dataArray)
                if (x?.Items != null && x.Results != null)
                    foreach (int f in x.Items)
                    {
                        if (!makes.TryGetValue(f, out var set)) makes[f] = set = new HashSet<int>();
                        foreach (int t in x.Results) set.Add(t);
                    }
            foreach (var a in All())
            {
                var outs = new List<int>();
                var outN = new List<int>();
                for (int i = 0; i < a.Out.Length; i++)
                    if (LDB.items.Select(a.Out[i]) != null) { outs.Add(a.Out[i]); outN.Add(a.OutN[i]); }
                var source = LDB.items.Select(a.In[0]);
                if (source == null || outs.Count == 0) { missing++; continue; }
                if (LDB.recipes.Exist(a.Id)) { taken++; continue; }
                if (Catalysis.Reaches(makes, outs, a.In)) { loops++; continue; }
                var r = Precious.New(a.Id, Lang.F("{0} Recycling", source.name), a.Type, a.Time, a.In, a.InN, outs.ToArray(), outN.ToArray(), tab * 1000 + 100 + made.Count + 1);
                r.Description = Lang.T("Breaks Dark Fog drops down into raw materials.");
                Precious.Mark(r, a.In[0], "badge-recycle");
                made.Add(r);
            }
            if (made.Count == 0) return mine;
            var ids = new int[made.Count];
            for (int i = 0; i < made.Count; i++) ids[i] = made[i].ID;
            var tech = Plugin.NewTech(TechId, Lang.T("Dark Fog Recycling"), Lang.T("Assemblers break Dark Fog drops down into ores and rare minerals; a chemical plant turns energy shards into hydrogen and deuterium."),
                Plugin.IconRoot + "tab-recycle", after, after, 2, ids, position);
            int first = Plugin.Append(LDB.recipes, made);
            Plugin.Insert(new[] { tech });
            for (int i = 0; i < made.Count; i++) made[i].Preload(first + i);
            Plugin.Log.LogInfo("Dark Fog recycling: " + made.Count + " recipes and tech " + TechId + "; " + missing + " left out (no such item here), " + taken + " ids taken, " + loops + " would close a loop");
            foreach (var r in made) mine.Add(r.ID);
            return mine;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using CommonAPI;
using CommonAPI.Systems;
using HarmonyLib;
using UnityEngine;

[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]
[assembly: AssemblyInformationalVersion("1.1.0")]

namespace OreEnrichment
{
    // One alternative recipe. Type and item ids are plain ints so the table can be checked without the game.
    public sealed class Alt
    {
        public int Id, Type, Time, Row, Col, Tech;  // Tech: 0 crushing, 1-3 enrichment tier
        public int GbType;        // building type while GenesisBook has a building for it; 0: Type
        public int Normal;        // enrichment: what the normal recipe makes from the same main input
        public int[] In, InN, Out, OutN;
        public string Name;       // unique: LDBTool keys its id and grid overrides by name, so two recipes of one name end up on one id
        public string Tier;       // enrichment: shown after the product's translated name
        public string Badge;      // gold/silver chain: badge laid over the product's picture
        public bool Handcraft;
    }

    // Ores become dust (1:1, or 2 ore + 1 water -> 3 dust), and dust plus an additive smelts into more of the vanilla
    // product than the ore would: x1.5, x2 and x3 over three research tiers. Vanilla recipes are untouched.
    // A scarce additive (sulfuric acid, fire ice ...) goes in once per batch, so one unit of it adds four or eight products.
    public static class Tables
    {
        public const int Smelt = 1, Chemical = 2, Refine = 3, Assemble = 4, Particle = 5;  // ERecipeType
        public const int Water = 1000, Hydrogen = 1120, Acid = 1116, Oil = 1114, Graphite = 1109, FireIce = 1011, Organic = 1117, Unipolar = 1016, Fractal = 1013, Kimberlite = 1012, Grating = 1014, Spiniform = 1015;
        public const int Iron = 0, Copper = 1, Stone = 2, Coal = 3, Silicon = 4, Titanium = 5;
        public const int DustBase = 9451, CrushBase = 9501, WashBase = 9511, EnrichBase = 9521;

        public static readonly int[] Ores = { 1001, 1002, 1005, 1006, 1003, 1004 };
        public static readonly string[] OreNames = { "Iron", "Copper", "Stone", "Coal", "Silicon", "Titanium" };
        public static readonly string[] Icons = { "iron-dust", "copper-dust", "stone-dust", "coal-dust", "silicon-dust", "titanium-dust" };

        public static int Dust(int ore) => DustBase + ore;

        // Batches of the vanilla recipe that go in, and products that come out, per tier.
        public static readonly int[] TierIn = { 4, 4, 4 }, TierOut = { 6, 8, 12 };

        // Plentiful additives: tier 1 takes two of them per batch, everything else one.
        public static bool Plentiful(int item) => item == Hydrogen || item == Water || (item >= DustBase && item < DustBase + 6);
        public static readonly string[] TierNames = { "Enriched I", "Enriched II", "Enriched III" };
        static readonly string[] ProductNames = { "Iron Ingot", "Magnet", "Copper Ingot", "Stone Brick", "Glass", "Energetic Graphite", "High-Purity Silicon", "Titanium Ingot", "Steel" };

        // GenesisBook's building types; used only while a building of that type exists.
        public const int GbFurnace = 11, GbPlasmaFurnace = 13, GbPolymer = 16;

        // product, main input, main input per product (as vanilla), vanilla ticks per product; additives of tier 1 / 2 / 3;
        // building type per tier; building type per tier with GenesisBook (0: the same)
        static readonly int[][][] Products =
        {
            new[] { new[] { 1101, Dust(Iron), 1, 60 }, new[] { Hydrogen }, new[] { Hydrogen, Dust(Stone) }, new[] { FireIce },             // iron ingot
                new[] { Smelt, Smelt, Smelt }, new[] { 0, GbFurnace, GbPlasmaFurnace } },
            new[] { new[] { 1102, Dust(Iron), 1, 90 }, new[] { Dust(Coal) }, new[] { Hydrogen }, new[] { Unipolar },                        // magnet
                new[] { Smelt, Smelt, Particle }, new[] { 0, GbFurnace, 0 } },
            new[] { new[] { 1104, Dust(Copper), 1, 60 }, new[] { Acid }, new[] { Acid, Dust(Iron) }, new[] { Organic },                     // copper ingot: leaching
                new[] { Chemical, Chemical, Chemical }, new[] { 0, 0, GbPolymer } },
            new[] { new[] { 1108, Dust(Stone), 1, 60 }, new[] { Dust(Coal) }, new[] { Water }, new[] { Acid },                              // stone brick
                new[] { Smelt, Assemble, Chemical }, new[] { 0, 0, 0 } },
            new[] { new[] { 1110, Dust(Stone), 2, 120 }, new[] { Dust(Silicon) }, new[] { Acid }, new[] { Fractal },                        // glass
                new[] { Smelt, Chemical, Smelt }, new[] { 0, 0, GbFurnace } },
            new[] { new[] { 1109, Dust(Coal), 2, 120 }, new[] { Hydrogen }, new[] { Oil }, new[] { FireIce },                               // energetic graphite
                new[] { Smelt, Refine, Chemical }, new[] { 0, 0, GbPolymer } },
            new[] { new[] { 1105, Dust(Silicon), 2, 120 }, new[] { Dust(Coal) }, new[] { Graphite }, new[] { Hydrogen, Grating },           // high-purity silicon
                new[] { Smelt, Smelt, Chemical }, new[] { GbFurnace, GbFurnace, 0 } },
            new[] { new[] { 1106, Dust(Titanium), 2, 120 }, new[] { Acid }, new[] { Acid, Dust(Coal) }, new[] { Hydrogen, Spiniform },      // titanium ingot
                new[] { Chemical, Chemical, Smelt }, new[] { 0, 0, GbPlasmaFurnace } },
            new[] { new[] { 1103, 1101, 3, 180 }, new[] { Dust(Coal) }, new[] { Graphite }, new[] { Kimberlite },                           // steel, from iron ingots
                new[] { Smelt, Smelt, Smelt }, new[] { 0, GbFurnace, GbPlasmaFurnace } },
        };

        public const double WashGain = 1.5;  // 2 ore -> 3 dust

        // Tooltip of a recipe: what it gives compared with the normal recipe, per ore, and what the additive buys.
        // name: item id -> shown name. Numbers use '.', whatever the system language.
        public static string Describe(Alt a, Func<int, string> name)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            if (a.Tech == 0)
                return Lang.T(a.In.Length == 1
                    ? "1 ore gives 1 dust.\nSmelted with an additive, dust gives 1.5, 2 or 3 times what the ore gives in its normal recipe (Ore Enrichment I to III).\nWashing instead of crushing gives 1.5 times as much dust."
                    : "2 ore and 1 water give 3 dust: 1.5 times the dust of crushing.\nThis multiplies with Ore Enrichment: washed dust ends up as 2.25, 3 or 4.5 times what the ore gives in its normal recipe.");
            int normal = a.Normal;
            double gain = (double)a.OutN[0] / normal;
            bool fromDust = a.In[0] >= DustBase && a.In[0] < DustBase + Ores.Length;
            var text = new System.Text.StringBuilder();
            text.Append(Lang.F(fromDust ? "{0} {1} from {2} {3}. The normal recipe makes {4} from the same ore." : "{0} {1} from {2} {3}. The normal recipe makes {4} from the same amount.",
                a.OutN[0], name(a.Out[0]), a.InN[0], name(a.In[0]), normal));
            if (fromDust)
                text.Append("\n").Append(Lang.F("Per ore: x{0} with crushed dust, x{1} with washed dust.", gain.ToString("0.##", inv), (gain * WashGain).ToString("0.##", inv)));
            for (int i = 1; i < a.In.Length; i++)
                text.Append("\n").Append(Lang.F(i == 1 ? "{0} {1} per batch" : "{0} {1} as well", a.InN[i], name(a.In[i])))
                    .Append(i == a.In.Length - 1 ? Lang.F(": the batch gives {0} more than without.", a.OutN[0] - normal) : ",");
            return text.ToString();
        }

        public static List<Alt> All()
        {
            var all = new List<Alt>();
            for (int o = 0; o < Ores.Length; o++)
            {
                all.Add(new Alt { Id = CrushBase + o, Type = Assemble, Time = 30, Row = 1, Col = o + 1, Tech = 0, Handcraft = true, Name = OreNames[o] + " Ore Crushing",
                    In = new[] { Ores[o] }, InN = new[] { 1 }, Out = new[] { Dust(o) }, OutN = new[] { 1 } });
                all.Add(new Alt { Id = WashBase + o, Type = Chemical, Time = 90, Row = 2, Col = o + 1, Tech = 0, Name = OreNames[o] + " Ore Washing",
                    In = new[] { Ores[o], Water }, InN = new[] { 2, 1 }, Out = new[] { Dust(o) }, OutN = new[] { 3 } });
            }
            for (int p = 0; p < Products.Length; p++)
                for (int t = 0; t < 3; t++)
                {
                    int[] head = Products[p][0], add = Products[p][t + 1];
                    var input = new List<int> { head[1] };
                    var counts = new List<int> { head[2] * TierIn[t] };
                    foreach (int a in add) { input.Add(a); counts.Add(t == 0 && Plentiful(a) ? 2 : 1); }
                    all.Add(new Alt { Id = EnrichBase + p * 3 + t, Type = Products[p][4][t], GbType = Products[p][5][t], Time = head[3] * TierOut[t], Row = 3 + t, Col = p + 1, Tech = t + 1, Name = ProductNames[p] + " (" + TierNames[t] + ")", Tier = TierNames[t], Normal = TierIn[t],
                        In = input.ToArray(), InN = counts.ToArray(), Out = new[] { head[0] }, OutN = new[] { TierOut[t] } });
                }
            return all;
        }
    }

    [BepInPlugin(Guid, "BUG_OreEnrichment", "1.1.0")]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [BepInDependency("me.xiaoye97.plugin.Dyson.LDBTool")]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry), nameof(TabSystem))]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "bug.dsp.oreenrichment", IconRoot = "BUGOreEnrichment/";
        public const int TechBase = 1971;  // main tech page needs ids below 2000; GenesisBook ends at 1963
        internal static ManualLogSource Log;
        static readonly List<RecipeProto> recipes = new List<RecipeProto>();
        static readonly List<Alt> alts = Tables.All();
        static bool techsBuilt;

        void Awake()
        {
            Log = Logger;
            var harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(Plugin));
            harmony.PatchAll(typeof(Catalysis));
            harmony.PatchAll(typeof(TabLayout));
            TabLayout.Own = () => new[] { tabIndex, preciousTab, preciousTab2, recycleTab }.Concat(catalysisTabs);
            Lang.Current = Config.Bind("General", "Language", Language.English,
                "Language of the mod's own texts: English, Chinese or Turkish (applies after restarting the game). Item and building names follow the game's language.").Value;
            using (ProtoRegistry.StartModLoad(Guid))
            {
                int tab = tabIndex = TabSystem.RegisterTab(Guid + ":tab", new TabData(Lang.T("Enrichment"), IconRoot + "tab"));
                catalysisTabs = new[] { TabSystem.RegisterTab(Guid + ":cat1", new TabData(Lang.T("Catalysis"), IconRoot + "tab-cat")),
                    TabSystem.RegisterTab(Guid + ":cat2", new TabData(Lang.T("Catalysis II"), IconRoot + "tab-cat")) };
                for (int o = 0; o < Tables.Ores.Length; o++)
                    ProtoRegistry.RegisterItem(Tables.Dust(o), Lang.F("{0} Dust", Lang.T(Tables.OreNames[o])),
                        Lang.F("Crushed {0} ore: 1 per ore from crushing, 3 per 2 ore from washing. Smelted with an additive it gives 1.5, 2 or 3 times what the ore gives in its normal recipe (Ore Enrichment I to III).", Lang.T(Tables.OreNames[o])),
                        IconRoot + Tables.Icons[o], ProtoRegistry.GetGridIndex(tab, o + 1, 1), 100, EItemType.Material).Productive = true;
                preciousTab = TabSystem.RegisterTab(Guid + ":precious", new TabData("Au-Ag", IconRoot + "tab-precious"));
                preciousTab2 = TabSystem.RegisterTab(Guid + ":precious2", new TabData("Au-Ag II", IconRoot + "tab-precious"));
                recycleTab = TabSystem.RegisterTab(Guid + ":recycle", new TabData(Lang.T("Recycling"), IconRoot + "tab-recycle"));
                for (int i = 0; i < Precious.Items.Length; i++)
                    ProtoRegistry.RegisterItem(Precious.Items[i], Lang.T(Precious.ItemNames[i]), Lang.T(PreciousItemDescs[i]),
                        IconRoot + Precious.ItemIcons[i], ProtoRegistry.GetGridIndex(preciousTab, i + 1, 1), 100, EItemType.Material).Productive = true;
                foreach (var a in alts)
                {
                    var r = ProtoRegistry.RegisterRecipe(a.Id, (ERecipeType)a.Type, a.Time, a.In, a.InN, a.Out, a.OutN, "", 0,
                        ProtoRegistry.GetGridIndex(tab, a.Col, a.Row),
                        a.Tech > 0 ? a.Name : Lang.F(a.In.Length == 1 ? "{0} Ore Crushing" : "{0} Ore Washing", Lang.T(Tables.OreNames[a.Col - 1])), "");
                    r.Handcraft = a.Handcraft;
                    r.Explicit = true;  // listed under its own name, not as the product's default recipe
                    recipes.Add(r);
                }
            }
        }

        static readonly string[] PreciousItemDescs =
        {
            "Left at the anode when copper is refined electrolytically. Leached, it gives gold and silver dust.",
            "Gold recovered from anode slime. Goes into circuit boards, processors and power buildings, or is smelted into ingots.",
            "Silver recovered from anode slime. Goes into coils and power buildings, or is smelted into ingots.",
            "Smelted gold. Doubles the products of electronics recipes, and with gold dust triples them.",
            "Smelted silver. Doubles the products of coil and chip recipes, and saves material in power buildings.",
        };

        // Item, tab and tech icons are PNGs inside this DLL, served under IconRoot.
        static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();

        internal static Sprite Icon(string name)
        {
            if (icons.TryGetValue(name, out var sprite)) return sprite;
            using (var s = typeof(Plugin).Assembly.GetManifestResourceStream(name + ".png"))
            {
                if (s != null)
                {
                    var bytes = new byte[s.Length];
                    s.Read(bytes, 0, bytes.Length);
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                    tex.LoadImage(bytes);
                    sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
            return icons[name] = sprite;
        }

        // Every alternative gets its own picture: the product's icon with a tier badge, washing the dust with a drop.
        // The game copies 80x80 textures into its icon atlas, so the composed ones are exactly that size.
        internal static Sprite Badged(Sprite icon, string badge)
        {
            var mark = Icon(badge);
            if (icon == null || mark == null) return icon;
            var rt = RenderTexture.GetTemporary(80, 80, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var before = RenderTexture.active;
            Graphics.Blit(icon.texture, rt);  // game icons are not readable; a blit is
            RenderTexture.active = rt;
            var tex = new Texture2D(80, 80, TextureFormat.RGBA32, true);
            tex.ReadPixels(new Rect(0, 0, 80, 80), 0, 0);
            RenderTexture.active = before;
            RenderTexture.ReleaseTemporary(rt);
            Color32[] under = tex.GetPixels32(), over = mark.texture.GetPixels32();
            if (over.Length != under.Length) return icon;
            for (int i = 0; i < under.Length; i++)
            {
                int a = over[i].a;
                if (a == 0) continue;
                under[i] = new Color32((byte)((over[i].r * a + under[i].r * (255 - a)) / 255), (byte)((over[i].g * a + under[i].g * (255 - a)) / 255),
                    (byte)((over[i].b * a + under[i].b * (255 - a)) / 255), (byte)Math.Max(a, under[i].a));
            }
            tex.SetPixels32(under);
            tex.Apply(true);
            return Sprite.Create(tex, new Rect(0, 0, 80, 80), new Vector2(0.5f, 0.5f));
        }

        static void SetIcons()
        {
            for (int i = 0; i < recipes.Count; i++)
            {
                var a = alts[i];
                string badge = a.Tech > 0 ? "badge-" + a.Tech : a.In.Length > 1 ? "badge-wash" : null;
                if (badge == null) continue;
                var sprite = Badged(LDB.items.Select(a.Out[0])?.iconSprite, badge);
                if (sprite != null) Traverse.Create(recipes[i]).Field("_iconSprite").SetValue(sprite);
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Resources), nameof(Resources.Load), typeof(string), typeof(Type))]
        static bool LoadIcon(string path, ref UnityEngine.Object __result)
        {
            if (path == null || !path.StartsWith(IconRoot)) return true;
            string name = path.Substring(IconRoot.Length);
            __result = name.StartsWith("cat/") && int.TryParse(name.Substring(4), out int source) ? Catalysis.IconFor(source)
                : name.StartsWith("pv/") && int.TryParse(name.Substring(3), out int recipe) ? Precious.IconFor(recipe) : Icon(name);
            return false;
        }

        // ---- The tab shows only in buildings that can run one of its unlocked recipes ----
        // Asked of the picker itself, so the game's filter and other mods' changes to it (building families) both count.
        static int tabIndex, preciousTab, preciousTab2, recycleTab;
        static int[] catalysisTabs = new int[0];

        [HarmonyPostfix, HarmonyPatch(typeof(UIRecipePicker), "_OnOpen")]
        [HarmonyAfter("dsp.common-api.CommonAPI")]
        static void FilterTab(UIRecipePicker __instance)
        {
            try
            {
                var t = Traverse.Create(__instance);
                int current = t.Field<int>("currentType").Value;
                var click = AccessTools.Method(typeof(UIRecipePicker), "OnTypeButtonClick");
                var shown = new Dictionary<int, bool> { { tabIndex, false }, { preciousTab, false }, { preciousTab2, false }, { recycleTab, false } };
                foreach (int tab in catalysisTabs) shown[tab] = false;
                foreach (int tab in new List<int>(shown.Keys))
                {
                    click.Invoke(__instance, new object[] { tab });
                    foreach (var p in t.Field<RecipeProto[]>("protoArray").Value) shown[tab] |= p != null;
                }
                click.Invoke(__instance, new object[] { shown.TryGetValue(current, out bool visible) && !visible ? 1 : current });
                foreach (var b in __instance.pickerTrans.GetComponentsInChildren<UITabButton>(true))
                {
                    int index = Traverse.Create(b).Field<int>("tabIndex").Value, hiddenBefore = 0;
                    foreach (var kv in shown) if (!kv.Value && kv.Key < index) hiddenBefore++;
                    if (shown.TryGetValue(index, out bool on)) b.gameObject.SetActive(on);
                    // CommonAPI places tab n at x 70n - 54; later tabs close the gaps of the hidden ones.
                    ((RectTransform)b.transform).anchoredPosition = new Vector2(70 * (index - hiddenBefore) - 54, -75);
                }
            }
            catch (Exception e) { Log.LogWarning("Tab filter skipped: " + e.Message); }
        }

        // ---- Research: four techs on the main page, added once every mod's tech tree is in place ----

        static readonly string[] TechNames = { "Ore Crushing", "Ore Enrichment I", "Ore Enrichment II", "Ore Enrichment III" };
        static readonly string[] TechDescs =
        {
            "Crush ores into dust in an assembler, or wash them with water in a chemical plant: three dust from two ore.",
            "Smelt dust with a simple additive such as hydrogen or coal dust: one and a half times the products of the ore.",
            "Smelt dust with stronger additives such as sulfuric acid or graphite: twice the products of the ore.",
            "Smelt dust with a rare-vein additive such as fire ice or unipolar magnets: three times the products of the ore.",
        };
        static readonly int[] GateItems = { 2306, Tables.Hydrogen, Tables.Acid, 2104 };  // water pump, hydrogen, sulfuric acid, interstellar station

        // Last of all: content mods (GenesisBook) finish their recipes, buildings and techs in their own load steps, and
        // anything set earlier (icons, names, building types) is overwritten by them.
        [HarmonyPostfix, HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
        [HarmonyAfter("me.xiaoye97.plugin.Dyson.LDBTool", "dsp.common-api.CommonAPI", "org.LoShin.GenesisBook", "bug.dsp.deliveryitemchain", "bug.dsp.dronecoverage")]
        static void Register()
        {
            if (techsBuilt) return;
            techsBuilt = true;
            try { Adapt(); }
            catch (Exception e) { Log.LogError("Recipes keep the vanilla ratios: " + e); }
            try { BuildTechs(); }
            catch (Exception e) { Log.LogError("Research not added, the recipes cannot be unlocked: " + e); }
            try { SetIcons(); }
            catch (Exception e) { Log.LogWarning("Recipe icons keep the product's picture: " + e.Message); }
            try { Precious.Generate(preciousTab, preciousTab2, tabIndex, rowX, rowDx, rowY - 2 * RowGap, TechBase, TechBase + 1); }
            catch (Exception e) { Log.LogError("Gold and silver recipes not added: " + e); }
            try
            {
                Recycling.Generate(new Vector2(rowX + Precious.TechNames.Length * rowDx, rowY - 2 * RowGap), TechBase);
                Recycling.GenerateLevels(recycleTab, rowX, rowDx, rowY - 3 * RowGap, RowGap);
            }
            catch (Exception e) { Log.LogError("Dark Fog recycling recipes not added: " + e); }
            try
            {
                var skip = new HashSet<int>(normals);  // the enrichment tiers already cover these
                foreach (var a in alts) skip.Add(a.Id);
                Catalysis.Generate(skip, catalysisTabs, rowX, rowDx, rowY - RowGap);
            }
            catch (Exception e) { Log.LogError("Catalysed recipes not added: " + e); }
            try { Precious.RefreshItemRecipes(); }
            catch (Exception e) { Log.LogWarning("Item tooltips do not list the new recipes: " + e.Message); }
        }

        // The tiers are multiples of the product's normal recipe, and content mods change that recipe (GenesisBook makes
        // glass from one stone, not two, and has no magnets at all). So amounts and time are taken from the normal
        // recipe the loaded game really has; a product without one loses its enrichment recipes.
        static readonly HashSet<int> normals = new HashSet<int>();

        static void Adapt()
        {
            var mine = new HashSet<int>();
            foreach (var a in alts) mine.Add(a.Id);
            int changed = 0, hidden = 0;
            for (int i = 0; i < alts.Count; i++)
            {
                var a = alts[i];
                if (a.Tech == 0) continue;
                int source = a.In[0] >= Tables.DustBase && a.In[0] < Tables.DustBase + Tables.Ores.Length ? Tables.Ores[a.In[0] - Tables.DustBase] : a.In[0];
                RecipeProto normal = null;
                foreach (var c in LDB.recipes.dataArray)
                    if (c != null && !mine.Contains(c.ID) && c.Items.Length == 1 && c.Items[0] == source && c.Results.Length == 1 && c.Results[0] == a.Out[0]) { normal = c; break; }
                var r = recipes[i];
                if (normal == null)
                {
                    r.GridIndex = 0;  // not listed anywhere, and BuildTechs leaves it locked
                    a.Row = 0;
                    hidden++;
                    continue;
                }
                int inN = Tables.TierIn[a.Tech - 1] * normal.ItemCounts[0], outN = Tables.TierOut[a.Tech - 1] * normal.ResultCounts[0];
                int time = normal.TimeSpend * Tables.TierOut[a.Tech - 1];
                if (inN != a.InN[0] || outN != a.OutN[0] || time != a.Time) changed++;
                normals.Add(normal.ID);
                a.Normal = Tables.TierIn[a.Tech - 1] * normal.ResultCounts[0];
                a.Time = r.TimeSpend = time;
                r.ItemCounts[0] = a.InN[0] = inN;
                r.ResultCounts[0] = a.OutN[0] = outN;
            }
            // Products left out leave no empty column.
            var column = new Dictionary<int, int>();
            for (int i = 0; i < alts.Count; i++)
            {
                var a = alts[i];
                if (a.Tech == 0 || a.Row == 0) continue;
                if (!column.TryGetValue(a.Out[0], out int col)) column[a.Out[0]] = col = column.Count + 1;
                a.Col = col;
                recipes[i].GridIndex = ProtoRegistry.GetGridIndex(tabIndex, col, a.Row);
            }
            if (changed + hidden > 0) Log.LogInfo(changed + " recipes follow a changed normal recipe, " + hidden + " hidden: their product has no normal recipe here");
        }

        static void BuildTechs()
        {
            var set = LDB.techs;
            foreach (var r in recipes)
                if (LDB.recipes.Select(r.ID) != r) throw new InvalidOperationException("recipe " + r.ID + " was not added (id taken?)");
            Layout(set.dataArray, out float x0, out float dx, out float y0);
            rowX = x0; rowDx = dx; rowY = y0;
            var techs = new TechProto[TechNames.Length];
            for (int i = 0; i < techs.Length; i++)
            {
                var unlocks = new List<int>();
                foreach (var a in alts) if (a.Tech == i && a.Row > 0) unlocks.Add(a.Id);
                techs[i] = NewTech(TechBase + i, Lang.T(TechNames[i]), Lang.T(TechDescs[i]), IconRoot + (i == 0 ? Tables.Icons[0] : "tab"), i == 0 ? 0 : techs[i - 1].ID,
                    TechUnlocking(GateItems[i]), i + 1, unlocks.ToArray(), new Vector2(x0 + i * dx, y0));
            }
            Insert(techs);

            // GenesisBook splits smelting and chemistry over more buildings; a recipe moves only if such a building exists.
            var buildings = new HashSet<int>();
            foreach (var item in LDB.items.dataArray)
                if (item?.prefabDesc != null && item.prefabDesc.isAssembler) buildings.Add((int)item.prefabDesc.assemblerRecipeType);
            int moved = 0;
            for (int i = 0; i < recipes.Count; i++)
            {
                var r = recipes[i];
                if (alts[i].GbType != 0 && buildings.Contains(alts[i].GbType)) { r.Type = (ERecipeType)alts[i].GbType; moved++; }
                r.preTech = techs[alts[i].Tech];
                if (alts[i].Tech > 0) r.name = (LDB.items.Select(r.Results[0])?.name ?? "") + " (" + Lang.T(alts[i].Tier) + ")";
                r.description = Tables.Describe(alts[i], id => LDB.items.Select(id)?.name ?? "#" + id);
            }
            Log.LogInfo(moved + " recipes moved to other mods' building types; added " + recipes.Count + " recipes and " + techs.Length + " techs (ids " + techs[0].ID + "-" + techs[techs.Length - 1].ID + ") at x " + x0 + " y " + y0);
        }

        // cubes: how many kinds of matrix it costs, 100 of each for one kind, doubling per kind.
        internal static TechProto NewTech(int id, string name, string desc, string icon, int after, int gate, int cubes, int[] unlocks, Vector2 position)
        {
            if (LDB.techs.Exist(id)) throw new InvalidOperationException("tech id " + id + " is taken by another mod");
            var items = new int[cubes];
            var points = new int[cubes];
            for (int c = 0; c < cubes; c++) { items[c] = TechProto.matrixIds[c]; points[c] = 10; }
            long hash = 36000L << (cubes - 1);
            bool lab = true;
            // On a changed tech tree the cost is that of the tech this one waits for: its matrices exist at that point.
            var like = gate > 0 ? LDB.techs.Select(gate) : null;
            if (like?.Items != null && like.Items.Length > 0 && like.ItemPoints != null && like.ItemPoints.Length == like.Items.Length && like.HashNeeded > 0
                && !SameItems(like.Items, items))
            {
                items = (int[])like.Items.Clone();
                points = (int[])like.ItemPoints.Clone();
                hash = like.HashNeeded;
                lab = like.IsLabTech;
            }
            return new TechProto
            {
                ID = id,
                Name = name,
                Desc = desc,
                Conclusion = "",
                Published = true,
                IsLabTech = lab,
                IconPath = icon,
                IconTag = "",
                PreTechs = after == 0 ? new int[0] : new[] { after },
                PreTechsImplicit = gate > 0 ? new[] { gate } : new int[0],
                PreItem = new int[0],
                Items = items,
                ItemPoints = points,
                HashNeeded = hash,
                UnlockRecipes = unlocks,
                UnlockFunctions = new int[0],
                UnlockValues = new double[0],
                AddItems = new int[0],
                AddItemCounts = new int[0],
                PropertyOverrideItems = new int[0],
                PropertyItemCounts = new int[0],
                PropertyOverrideItemArray = new IDCNT[0],
                Position = position,
            };
        }

        // Appends to a loaded proto table; returns the index of the first new entry.
        internal static int Append<T>(ProtoSet<T> set, IList<T> add) where T : Proto
        {
            var old = set.dataArray;
            set.Init(old.Length + add.Count);
            Array.Copy(old, set.dataArray, old.Length);
            for (int i = 0; i < add.Count; i++) set.dataArray[old.Length + i] = add[i];
            var index = new Dictionary<int, int>();  // not OnAfterDeserialize: it resets every translated name to its key
            for (int i = 0; i < set.dataArray.Length; i++) index[set.dataArray[i].ID] = i;
            Traverse.Create(set).Field("dataIndices").SetValue(index);
            if (add.Count > 0 && set.Select(add[0].ID) != add[0]) throw new InvalidOperationException("proto index not rebuilt");
            return old.Length;
        }

        // The vanilla ladder (blue, then blue and red ...) is kept when the awaited tech costs matrices from that ladder only.
        static bool SameItems(int[] theirs, int[] ours)
        {
            foreach (int item in theirs) if (Array.IndexOf(TechProto.matrixIds, item) < 0) return false;
            return true;
        }

        internal static void Insert(TechProto[] techs)
        {
            Append(LDB.techs, techs);
            foreach (var p in techs) p.Preload();
            foreach (var p in techs) p.Preload2();
        }

        // The tech that unlocks a recipe making this item, so each tier opens when its additive exists on any tech tree.
        internal static int TechUnlocking(int item)
        {
            var made = new HashSet<int>();
            foreach (var r in LDB.recipes.dataArray)
                if (r?.Results != null && Array.IndexOf(r.Results, item) >= 0) made.Add(r.ID);
            foreach (var t in LDB.techs.dataArray)
                if (t?.UnlockRecipes != null)
                    foreach (int id in t.UnlockRecipes) if (made.Contains(id)) return t.ID;
            return 0;
        }

        // A new row under the lowest row of the main tech page, on that page's own spacing and starting column.
        internal static float RowGap = 4f;
        static float rowX, rowDx, rowY;

        public static void Layout(TechProto[] techs, out float x0, out float dx, out float y0)
        {
            float minY = float.MaxValue;
            var rowStart = new Dictionary<float, float>();
            var ys = new SortedSet<float>();
            var xs = new SortedSet<float>();
            foreach (var t in techs)
            {
                if (t == null || t.page != 0 || !t.Published) continue;
                var p = t.Position;
                minY = Math.Min(minY, p.y);
                ys.Add(p.y);
                xs.Add(p.x);
                rowStart[p.y] = rowStart.TryGetValue(p.y, out float x) ? Math.Min(x, p.x) : p.x;
            }
            dx = CommonGap(xs, 4f);
            float dy = RowGap = CommonGap(ys, 4f);
            x0 = Mode(rowStart.Values, 0f);
            y0 = (minY == float.MaxValue ? 0f : minY) - dy;
        }

        static float CommonGap(SortedSet<float> values, float fallback)
        {
            var gaps = new List<float>();
            float last = float.NaN;
            foreach (float v in values) { if (!float.IsNaN(last) && v - last > 0.5f) gaps.Add(v - last); last = v; }
            return Mode(gaps, fallback);
        }

        static float Mode(IEnumerable<float> values, float fallback)
        {
            var counts = new Dictionary<float, int>();
            float best = fallback;
            int most = 0;
            foreach (float v in values)
            {
                counts[v] = counts.TryGetValue(v, out int n) ? n + 1 : 1;
                if (counts[v] > most || counts[v] == most && v < best) { most = counts[v]; best = v; }
            }
            return best;
        }
    }
}

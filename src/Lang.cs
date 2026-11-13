using System.Collections.Generic;

namespace OreEnrichment
{
    public enum Language { English, Chinese, Turkish }

    // The mod's own texts. The English text is the key; item and building names come from the game's localisation.
    // Chosen in the config file: Turkish players run the game in its English slot, so the game's language cannot tell.
    public static class Lang
    {
        public static Language Current = Language.English;

        // English -> { Chinese, Turkish }
        static readonly Dictionary<string, string[]> texts = new Dictionary<string, string[]>
        {
            ["Iron"] = new[] { "铁", "Demir" },
            ["Copper"] = new[] { "铜", "Bakır" },
            ["Stone"] = new[] { "石", "Taş" },
            ["Coal"] = new[] { "煤", "Kömür" },
            ["Silicon"] = new[] { "硅", "Silisyum" },
            ["Titanium"] = new[] { "钛", "Titanyum" },
            ["{0} Dust"] = new[] { "{0}粉", "{0} Tozu" },
            ["{0} Ore Crushing"] = new[] { "{0}矿粉碎", "{0} Cevheri Öğütme" },
            ["{0} Ore Washing"] = new[] { "{0}矿水洗", "{0} Cevheri Yıkama" },
            ["Crushed {0} ore: 1 per ore from crushing, 3 per 2 ore from washing. Smelted with an additive it gives 1.5, 2 or 3 times what the ore gives in its normal recipe (Ore Enrichment I to III)."] = new[]
            {
                "粉碎的{0}矿：粉碎每 1 矿得 1 粉，水洗每 2 矿得 3 粉。加入添加剂冶炼时，产量为该矿普通配方的 1.5、2 或 3 倍（矿物富集 I 至 III）。",
                "Öğütülmüş {0} cevheri: öğütmede cevher başına 1, yıkamada 2 cevherden 3 toz. Katkı maddesiyle eritildiğinde cevherin normal tarifinin 1.5, 2 veya 3 katını verir (Cevher Zenginleştirme I-III).",
            },
            ["Enriched I"] = new[] { "富集 I", "Zenginleştirilmiş I" },
            ["Enriched II"] = new[] { "富集 II", "Zenginleştirilmiş II" },
            ["Enriched III"] = new[] { "富集 III", "Zenginleştirilmiş III" },
            ["Enrichment"] = new[] { "富集", "Zenginleştirme" },
            ["Catalysis"] = new[] { "催化", "Kataliz" },
            ["Catalysis II"] = new[] { "催化 II", "Kataliz II" },
            [" (Catalysed)"] = new[] { "（催化）", " (Katalizli)" },

            ["Ore Crushing"] = new[] { "矿物粉碎", "Cevher Öğütme" },
            ["Ore Enrichment I"] = new[] { "矿物富集 I", "Cevher Zenginleştirme I" },
            ["Ore Enrichment II"] = new[] { "矿物富集 II", "Cevher Zenginleştirme II" },
            ["Ore Enrichment III"] = new[] { "矿物富集 III", "Cevher Zenginleştirme III" },
            ["Catalysis I"] = new[] { "催化 I", "Kataliz I" },
            ["Catalysis III"] = new[] { "催化 III", "Kataliz III" },
            ["Catalysis IV"] = new[] { "催化 IV", "Kataliz IV" },
            ["Catalysis V"] = new[] { "催化 V", "Kataliz V" },
            ["Crush ores into dust in an assembler, or wash them with water in a chemical plant: three dust from two ore."] = new[]
            {
                "在制造台中把矿石粉碎成矿粉，或在化工厂中用水冲洗：每两份矿石得三份矿粉。",
                "Cevherleri montaj makinesinde toza öğüt ya da kimya tesisinde suyla yıka: iki cevherden üç toz.",
            },
            ["Smelt dust with a simple additive such as hydrogen or coal dust: one and a half times the products of the ore."] = new[]
            {
                "用氢、煤粉等简单添加剂冶炼矿粉：产量为矿石的 1.5 倍。",
                "Tozu hidrojen veya kömür tozu gibi basit bir katkıyla erit: cevherin verdiğinin bir buçuk katı ürün.",
            },
            ["Smelt dust with stronger additives such as sulfuric acid or graphite: twice the products of the ore."] = new[]
            {
                "用硫酸、石墨等更强的添加剂冶炼矿粉：产量为矿石的 2 倍。",
                "Tozu sülfürik asit veya grafit gibi daha güçlü katkılarla erit: cevherin verdiğinin iki katı ürün.",
            },
            ["Smelt dust with a rare-vein additive such as fire ice or unipolar magnets: three times the products of the ore."] = new[]
            {
                "用可燃冰、单极磁石等珍奇矿物添加剂冶炼矿粉：产量为矿石的 3 倍。",
                "Tozu yanan buz veya tek kutuplu mıknatıs gibi nadir damar katkılarıyla erit: cevherin verdiğinin üç katı ürün.",
            },
            ["Catalysed copies of recipes: three batches of the inputs and one catalyst give four batches of the products, in the same building. This stage covers recipes of its research level and uses {0} where the recipe does not already contain it."] = new[]
            {
                "配方的催化版本：三批原料加一份催化剂产出四批产物，使用同一种建筑。本阶段覆盖对应研究层级的配方，在配方本身不含{0}时以其作为催化剂。",
                "Tariflerin katalizli kopyaları: üç parti girdi ve bir katalizör, aynı binada dört parti ürün verir. Bu kademe kendi araştırma seviyesindeki tarifleri kapsar ve tarif zaten içermiyorsa {0} kullanır.",
            },

            ["1 ore gives 1 dust.\nSmelted with an additive, dust gives 1.5, 2 or 3 times what the ore gives in its normal recipe (Ore Enrichment I to III).\nWashing instead of crushing gives 1.5 times as much dust."] = new[]
            {
                "1 份矿石得 1 份矿粉。\n加入添加剂冶炼时，矿粉的产量为矿石普通配方的 1.5、2 或 3 倍（矿物富集 I 至 III）。\n改用水洗可得 1.5 倍的矿粉。",
                "1 cevher 1 toz verir.\nKatkı maddesiyle eritilen toz, cevherin normal tarifinin 1.5, 2 veya 3 katını verir (Cevher Zenginleştirme I-III).\nÖğütmek yerine yıkamak 1.5 kat toz verir.",
            },
            ["2 ore and 1 water give 3 dust: 1.5 times the dust of crushing.\nThis multiplies with Ore Enrichment: washed dust ends up as 2.25, 3 or 4.5 times what the ore gives in its normal recipe."] = new[]
            {
                "2 份矿石加 1 份水得 3 份矿粉：是粉碎的 1.5 倍。\n这与矿物富集相乘：水洗矿粉最终为矿石普通配方的 2.25、3 或 4.5 倍。",
                "2 cevher ve 1 su 3 toz verir: öğütmenin 1.5 katı.\nBu, Cevher Zenginleştirme ile çarpılır: yıkanmış toz, cevherin normal tarifinin 2.25, 3 veya 4.5 katına ulaşır.",
            },
            ["{0} {1} from {2} {3}. The normal recipe makes {4} from the same ore."] = new[]
            {
                "{2} 份{3}产出 {0} 份{1}。同样的矿石用普通配方只得 {4} 份。",
                "{2} {3} ile {0} {1}. Normal tarif aynı cevherden {4} üretir.",
            },
            ["{0} {1} from {2} {3}. The normal recipe makes {4} from the same amount."] = new[]
            {
                "{2} 份{3}产出 {0} 份{1}。同样的数量用普通配方只得 {4} 份。",
                "{2} {3} ile {0} {1}. Normal tarif aynı miktardan {4} üretir.",
            },
            ["Per ore: x{0} with crushed dust, x{1} with washed dust."] = new[]
            {
                "按矿石计：粉碎矿粉 x{0}，水洗矿粉 x{1}。",
                "Cevher başına: öğütülmüş tozla x{0}, yıkanmış tozla x{1}.",
            },
            ["{0} {1} per batch"] = new[] { "每批 {0} 份{1}", "Parti başına {0} {1}" },
            ["{0} {1} as well"] = new[] { "另加 {0} 份{1}", "ayrıca {0} {1}" },
            [": the batch gives {0} more than without."] = new[] { "：每批比不加时多 {0} 份。", ": parti, katkısız hale göre {0} fazla verir." },
            ["{0} {1} from three batches of the inputs and 1 {2}. The normal recipe makes {3} from the same inputs.\nRuns in the same building as the normal recipe."] = new[]
            {
                "三批原料加 1 份{2}产出 {0} 份{1}。同样的原料用普通配方只得 {3} 份。\n与普通配方使用同一种建筑。",
                "Üç parti girdi ve 1 {2} ile {0} {1}. Normal tarif aynı girdilerden {3} üretir.\nNormal tarifle aynı binada çalışır.",
            },
            // ---- Gold and silver (Precious.cs) ----
            ["Anode Slime"] = new[] { "阳极泥", "Anot Çamuru" },
            ["Gold Dust"] = new[] { "金粉", "Altın Tozu" },
            ["Silver Dust"] = new[] { "银粉", "Gümüş Tozu" },
            ["Gold Ingot"] = new[] { "金锭", "Altın Külçe" },
            ["Silver Ingot"] = new[] { "银锭", "Gümüş Külçe" },
            ["Left at the anode when copper is refined electrolytically. Leached, it gives gold and silver dust."] = new[] { "铜电解精炼时留在阳极的残渣。浸出后得到金粉和银粉。", "Bakır elektrolizle arıtılırken anotta kalan tortu. Liç edilince altın ve gümüş tozu verir." },
            ["Gold recovered from anode slime. Goes into circuit boards, processors and power buildings, or is smelted into ingots."] = new[] { "从阳极泥中回收的金。可用于电路板、处理器和电力建筑，或冶炼成锭。", "Anot çamurundan kazanılan altın. Devre kartı, işlemci ve enerji binalarında kullanılır ya da külçeye eritilir." },
            ["Silver recovered from anode slime. Goes into coils and power buildings, or is smelted into ingots."] = new[] { "从阳极泥中回收的银。可用于线圈和电力建筑，或冶炼成锭。", "Anot çamurundan kazanılan gümüş. Bobinlerde ve enerji binalarında kullanılır ya da külçeye eritilir." },
            ["Smelted gold. Doubles the products of electronics recipes, and with gold dust triples them."] = new[] { "冶炼后的金。使电子配方产量翻倍，与金粉同用则为三倍。", "Eritilmiş altın. Elektronik tariflerinin ürününü iki katına, altın tozuyla birlikte üç katına çıkarır." },
            ["Smelted silver. Doubles the products of coil and chip recipes, and saves material in power buildings."] = new[] { "冶炼后的银。使线圈和芯片配方产量翻倍，并节省电力建筑的材料。", "Eritilmiş gümüş. Bobin ve çip tariflerinin ürününü iki katına çıkarır, enerji binalarında malzeme tasarrufu sağlar." },
            ["Precious Metal Recovery"] = new[] { "贵金属回收", "Değerli Metal Kazanımı" },
            ["Gold and Silver Electronics"] = new[] { "金银电子", "Altın ve Gümüş Elektronik" },
            ["Gold and Silver Construction"] = new[] { "金银建造", "Altın ve Gümüş İnşaat" },
            ["Fluid Chemistry"] = new[] { "流体化学", "Akışkan Kimyası" },
            ["Refine copper electrolytically: the anode slime it leaves holds gold and silver. Leach the slime with sulfuric acid and smelt the dust into ingots."] = new[] { "电解精炼铜：留下的阳极泥含有金和银。用硫酸浸出阳极泥，再把金属粉冶炼成锭。", "Bakırı elektrolizle arıt: geride kalan anot çamuru altın ve gümüş içerir. Çamuru sülfürik asitle liç et, tozu külçeye erit." },
            ["Gold and silver in electronic parts and in everything made with them: parts give up to three times the products, other components twice."] = new[] { "在电子零件及所有用它们制造的物品中加入金银：零件产量最高为 3 倍，其他组件为 2 倍。", "Elektronik parçalarda ve onlarla yapılan her şeyde altın ve gümüş: parçalar üç katına kadar, diğer bileşenler iki kat ürün verir." },
            ["Gold contacts and silver wiring in buildings, turrets and factories: two batches of the normal materials and a few ingots give three buildings."] = new[] { "建筑、炮塔和工厂中的金触点和银导线：两批普通材料加少量金属锭可造出三座建筑。", "Binalarda, taretlerde ve fabrikalarda altın kontak ve gümüş kablo: iki parti normal malzeme ve birkaç külçe üç bina verir." },
            ["GenesisBook fluids: nitric acid and hydrochloric acid for gold and silver, oxygen, ammonia, lye and carbon dioxide in dust smelting, and seawater washing that also gives salt."] = new[] { "创世之书的流体：硝酸和盐酸用于提炼金银，氧气、氨、烧碱和二氧化碳用于矿粉冶炼，海水冲洗还能得到盐。", "GenesisBook akışkanları: altın ve gümüş için nitrik ve hidroklorik asit, toz eritmede oksijen, amonyak, kostik ve karbondioksit, ayrıca tuz da veren deniz suyuyla yıkama." },
            ["Electrolytic Copper Refining"] = new[] { "铜电解精炼", "Elektrolitik Bakır Arıtma" },
            ["Anode Slime Leaching"] = new[] { "阳极泥浸出", "Anot Çamuru Liçi" },
            ["Gold Smelting"] = new[] { "金冶炼", "Altın Eritme" },
            ["Silver Smelting"] = new[] { "银冶炼", "Gümüş Eritme" },
            ["Nitric Acid Parting"] = new[] { "硝酸分金", "Nitrik Asitle Ayrıştırma" },
            ["Wohlwill Gold Refining"] = new[] { "沃威尔金精炼", "Wohlwill Altın Arıtma" },
            ["{0} Ore Seawater Washing"] = new[] { "{0}矿海水冲洗", "{0} Cevheri Deniz Suyuyla Yıkama" },
            ["Copper dust refined in sulfuric acid gives as much copper as smelting, and anode slime besides."] = new[] { "在硫酸中精炼铜粉，得到的铜与冶炼相同，另得阳极泥。", "Sülfürik asitte arıtılan bakır tozu, eritmeyle aynı miktarda bakır ve ayrıca anot çamuru verir." },
            ["Sulfuric acid dissolves the copper left in the slime; gold and silver stay behind as dust."] = new[] { "硫酸溶解阳极泥中残留的铜，金和银以粉末形式留下。", "Sülfürik asit çamurdaki bakırı çözer; altın ve gümüş toz olarak geride kalır." },
            ["Gold dust smelted into ingots."] = new[] { "把金粉冶炼成锭。", "Altın tozu külçeye eritilir." },
            ["Silver dust smelted into ingots."] = new[] { "把银粉冶炼成锭。", "Gümüş tozu külçeye eritilir." },
            ["Nitric acid parts silver from gold: more of both from the same slime than sulfuric acid gives."] = new[] { "硝酸把银从金中分离：同样的阳极泥得到的金银都比硫酸多。", "Nitrik asit gümüşü altından ayırır: aynı çamurdan sülfürik asitten daha fazla altın ve gümüş." },
            ["Gold dust refined by electrolysis in hydrochloric acid: three ingots from two dust."] = new[] { "在盐酸中电解精炼金粉：两份金粉得三块金锭。", "Altın tozu hidroklorik asitte elektrolizle arıtılır: iki tozdan üç külçe." },
            ["Washing with seawater gives as much dust as washing with water, and salt besides."] = new[] { "用海水冲洗得到的矿粉与用水冲洗相同，另得盐。", "Deniz suyuyla yıkama, suyla yıkama kadar toz ve ayrıca tuz verir." },
            [" (Dust)"] = new[] { "（金属粉）", " (Toz)" },
            [" (Ingot)"] = new[] { "（金属锭）", " (Külçe)" },
            [" (Dust and Ingot)"] = new[] { "（粉与锭）", " (Toz ve Külçe)" },
            [" (Gold)"] = new[] { "（金）", " (Altın)" },
            [" (Silver)"] = new[] { "（银）", " (Gümüş)" },
            [" (Gold and Silver)"] = new[] { "（金银）", " (Altın ve Gümüş)" },
            [" (Fluid)"] = new[] { "（流体）", " (Akışkan)" },
            [" and "] = new[] { "和", " ve " },
            ["{0} {1} from {2} batches of the normal inputs and {3}. The normal recipe makes {4} from the same inputs."] = new[] { "{2} 批普通原料加{3}产出 {0} 份{1}。同样的原料用普通配方只得 {4} 份。", "{2} parti normal girdi ve {3} ile {0} {1}. Normal tarif aynı girdilerden {4} üretir." },
            ["Each extra unit gives {0} more {1}."] = new[] { "每份额外材料多得 {0} 份{1}。", "Her ek birim {0} {1} daha verir." },
        };

        public static string T(string english) =>
            Current != Language.English && texts.TryGetValue(english, out var t) ? t[(int)Current - 1] : english;

        public static string F(string english, params object[] args) => string.Format(System.Globalization.CultureInfo.InvariantCulture, T(english), args);

        // For the check: every text has both translations, with the same placeholders as the English.
        public static IEnumerable<KeyValuePair<string, string[]>> All => texts;
    }
}

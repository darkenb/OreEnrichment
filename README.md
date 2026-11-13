# BUG_OreEnrichment

More ingots from the same ore. Ores are crushed into dust, and dust smelted together with an additive gives more of the normal product than the ore would. Copper refining also yields gold and silver, which raise the output of electronics and power buildings. Nothing in the game is replaced: every vanilla recipe still works, and there are no new buildings and no new end products.

## How it works

| Step | Building | Recipe |
|---|---|---|
| Crushing | Assembler | 1 ore -> 1 dust |
| Washing | Chemical plant | 2 ore + 1 water -> 3 dust |
| Enriched smelting | Depends on the additive, see below | dust + additive -> more products |

There are six dusts: iron, copper, stone, coal, silicon and titanium. All new recipes are on their own "Enrichment" tab.

## Enriched smelting

Each tier is a separate recipe, measured against what the same ore gives in the vanilla recipe: tier I gives 1.5 times as much, tier II twice, tier III three times. A batch is four vanilla batches of dust; plentiful additives (hydrogen, water, dust) go in twice per batch in tier I, everything else once. One sulfuric acid in tier II therefore adds four products, one fire ice in tier III eight. Every tier III recipe needs a rare-vein item or sulfuric acid.

| Product | Tier I | Tier II | Tier III |
|---|---|---|---|
| Iron ingot | Hydrogen (smelter) | Hydrogen + stone dust (smelter) | Fire ice (smelter) |
| Magnet | Coal dust (smelter) | Hydrogen (smelter) | Unipolar magnet (particle collider) |
| Copper ingot | Sulfuric acid (chemical plant) | Sulfuric acid + iron dust (chemical plant) | Organic crystal (chemical plant) |
| Stone brick | Coal dust (smelter) | Water (assembler) | Sulfuric acid (chemical plant) |
| Glass | Silicon dust (smelter) | Sulfuric acid (chemical plant) | Fractal silicon (smelter) |
| Energetic graphite | Hydrogen (smelter) | Refined oil (refinery) | Fire ice (chemical plant) |
| High-purity silicon | Coal dust (smelter) | Energetic graphite (smelter) | Hydrogen + optical grating crystal (chemical plant) |
| Titanium ingot | Sulfuric acid (chemical plant) | Sulfuric acid + coal dust (chemical plant) | Hydrogen + spiniform stalagmite crystal (smelter) |
| Steel (from iron ingots) | Coal dust (smelter) | Energetic graphite (smelter) | Kimberlite ore (smelter) |

The "Enrichment" tab only shows in a building that can run one of the recipes you have unlocked. Each recipe has its own icon: the product with a I, II or III badge, and a water drop for washing.

With GenesisBook installed, some recipes move to its own buildings: silicon, the tier II smelter recipes and tier III glass to its standard furnace, the other tier III smelter recipes to its plasma furnace, and the organic crystal and fire ice recipes to its polymer chemical plant.

The amounts above are for the vanilla game. With a mod that changes a product's normal recipe, the enrichment recipes follow it, so the tiers stay 1.5, 2 and 3 times the normal recipe; a product that has no normal recipe there (GenesisBook has no magnets) gets no enrichment recipes.

Proliferator works on these recipes as on any other, so the bonuses multiply.

## Gold and silver

On Earth most silver and much of the gold come out of copper refining, and so they do here. There are no gold or silver veins: the metals come from copper you mine anyway. All of it is on the "Au-Ag" tab.

| Step | Building | Recipe |
|---|---|---|
| Electrolytic copper refining | Chemical plant | 4 copper dust + 1 sulfuric acid -> 4 copper ingots + 2 anode slime |
| Slime leaching | Chemical plant | 2 anode slime + 1 sulfuric acid -> 1 gold dust + 2 silver dust |
| Smelting | Smelter | 1 gold dust -> 1 gold ingot, 1 silver dust -> 1 silver ingot |

Refining gives as much copper as smelting the same dust, so the slime comes on top. Every 4 copper ore refined give 1 gold dust and 2 silver dust.

**Electronic parts.** The parts themselves get three gold or silver recipes each, made from four batches of their normal recipe. The copper and other materials of the normal recipe stay in it.

| Metal added | Products | Example: circuit board (normal: 2 iron + 1 copper -> 2) |
|---|---|---|
| Dust | x1.5 | 8 iron + 4 copper + 2 gold dust -> 12 |
| Ingot | x2 | 8 iron + 4 copper + 2 gold ingots -> 16 |
| Dust and ingot | x3 | 8 iron + 4 copper + 2 gold dust + 2 gold ingots -> 24 |

Gold goes into circuit boards, processors, quantum chips and photon combiners (with GenesisBook also the optical processor, quantum computer and "Beyond" X-1); silver into magnetic coils, microcrystalline components and super-magnetic rings. One unit of metal goes with every six units of the normal inputs.

**Everything made with those parts.** Every other recipe that uses an electronic part gets a gold or silver copy, whichever mod it comes from: matrices, logistics drones and vessels, fighters and missiles, belts, sorters, assemblers, smelters, power plants, turrets and logistics stations. The metal follows the parts inside:

| Parts in the recipe | Metal | Component (four batches) | Building, turret (two batches) |
|---|---|---|---|
| Circuit board, microcrystalline, processor, quantum chip, photon combiner, particle broadband, optical processor, quantum computer, memory, silicon neuron | Gold ingots | x2 | x1.5 |
| Magnetic coil, electric motor, electromagnetic turbine, super-magnetic ring, particle container, plasma exciter, fusion capacitor | Silver ingots | x2 | x1.5 |
| Both kinds | Gold and silver ingots | x2.5 | x2 |

One ingot of each metal goes with every six units of a component's inputs, or every ten of a building's: one silver ingot for a Tesla tower, 28 of each for an interstellar logistics station. Buildings still cannot be proliferated for extra products. Recipes a mod keeps out of the replicator, such as the receiver forms of MoreMegaStructure, get no copy. The game lists at most seven items per recipe, so a recipe that would go over gets only gold, or no copy at all (with GenesisBook the ray receiver and the fission power station take gold only).

These copies sit on the "Au-Ag" tab under the parts and on "Au-Ag II", components first, then buildings.

Each recipe shows its product with a badge: gold or silver dots for dust, a bar for an ingot, both for both, and a half gold, half silver disc where it takes both metals. The tooltip gives the amounts and how much each unit of metal adds.

## With GenesisBook

GenesisBook's fluids add a "Fluid Chemistry" technology:

- Nitric acid parting: 2 anode slime + 1 nitric acid -> 2 gold dust + 3 silver dust.
- Wohlwill gold refining: 2 gold dust + 1 hydrochloric acid -> 3 gold ingots.
- Fluid smelting of dust, 2.5 times the products of the ore (between tier II and tier III): steel with oxygen, copper with ammonia, titanium and silicon with hydrochloric acid, glass with sodium hydroxide, stone bricks with carbon dioxide.
- Seawater washing: 4 ore + 2 seawater -> 6 dust + 1 salt.

Without GenesisBook these recipes and the technology do not exist.

## Catalysed recipes

Every other recipe that makes a material, component, ammunition or matrix also gets a catalysed copy: three batches of the inputs and one catalyst give four batches of the products, a third more from the same materials. The copy runs in the same building as the normal recipe, so each assembler, smelter and chemical plant tier lists the copies of its own recipes on the "Catalysis" tabs.

The catalyst follows the research level of the normal recipe: refined oil, hydrogen, graphene, carbon nanotubes, strange matter. If the recipe already contains it, the next simpler one is used.

A catalysed recipe shows the icon of its normal recipe with a flask badge.

The copies are made from the recipes the loaded game has, so recipes of other mods get them too. There are none for buildings, for recipes the game bars from extra products, and for any recipe whose copy would close a production loop.

## Dark Fog recycling

The drops of the Dark Fog turn into finished items on the "Recycling" tab. Every drop has more than one choice:

| Drop | Makes |
|---|---|
| Dark Fog Matrix | magazines; matrices I-III |
| Silicon-based Neuron | explosive units; combat units |
| Matter Recombinator | shells; Dyson sphere parts (sail, frame, component, rocket, EM rail ejector, vertical launching silo); top assembler, top smelter and jammer tower |
| Negentropy Singularity | missiles; matrices IV-VI; power facilities IV-VI |
| Core Element | plasma and antimatter capsules; fuel rods IV-VI; power facilities I-III |
| 10 Energy Shards | jamming capsules; fuel rods I-III |

The groups come from the loaded game, so GenesisBook's extra rounds, matrices and fuel rods are part of them. Every group but the ammunition runs from the cheapest item to the dearest; a group of more than six items is split in two, each half from its own drop. Power facilities are the accumulator, geothermal and fusion plants, ray receiver, artificial stars and (with GenesisBook) the isotope generator.

Every group comes in levels (up to three per half), each a technology of its own, and each level makes items of its own: level I the simplest, the last level the hardest. A round that stands apart from its group in the item grid joins the group's last level.

A recipe is worth about what its drops are worth. Every item costs the raw materials of the game's own recipes (an ore, a stone or a water count 1), and each drop has a fixed worth in the same units: Energy Shard 3, Dark Fog Matrix 20, Silicon-based Neuron 25, Matter Recombinator 30, Negentropy Singularity 40, Core Element 60. A level takes as many drops as its hardest item needs and gives the cheaper items in matching numbers. With GenesisBook one Dark Fog Matrix makes 10 machine-gun and 3 steel-core magazines, while a vertical launching silo with six rockets takes 193 Matter Recombinators.

Each recipe runs in the building the game uses for its hardest item, so the levels spread over the assemblers, GenesisBook's other assembly buildings and, for matrices, the matrix lab.

Each level opens after the previous one and after the technology that unlocks its hardest item, so nothing is made before it is researched. The first levels follow Dark Fog Recycling. Works with any Dark Fog settings, including BUG_DarkFogTuner's.

## Research

Four technologies on the main tech page, in a row under the lowest one: Ore Crushing, then Ore Enrichment I, II and III. Each also needs the technology that gives its additives (water pump, hydrogen, sulfuric acid, interstellar logistics). A second row holds Catalysis I to V; each needs the technology that gives its catalyst. A third row holds Precious Metal Recovery (after sulfuric acid), Gold and Silver Electronics and Gold and Silver Construction (both after processors) and, with GenesisBook, Fluid Chemistry. Dark Fog Recycling follows them on the same row, after Ore Crushing; its levels sit on five rows under it, three groups side by side.

In the vanilla game each technology costs one more kind of matrix than the one before it (100, 200, 400 ... of each). On a changed tech tree with its own matrices, each technology costs what the technology it waits for costs.

## Settings

`BepInEx/config/bug.dsp.oreenrichment.cfg`:

- `Language`: `English` (default), `Chinese` or `Turkish`, for the mod's own texts. Applies after restarting the game. Item and building names follow the game's language.

## Compatibility

- **Replicator**: ore crushing, gold and silver smelting, and the gold or silver copies of recipes the replicator already makes can be made by hand. Recipes with fluids or acids, chemical-plant recipes and catalysed copies need a building, as in the game.
- **GenesisBook**: supported. The recipes follow its changed normal recipes, move to its buildings where it has them, and its fluids add the recipes above.
- **Galactic Scale** and other mods that change planets: no effect, this mod adds no veins.
- **BUG_CalculatorPatch** (DSPCalculator): lists these recipes as alternatives after the normal ones.
- Other content mods: their recipes get catalysed copies, and their changed normal recipes are followed.

## Before you install

- **Saves depend on this mod.** Dust in a save, and buildings set to the new recipes, need the mod. Removing it from a save that uses them can break that save. Keep a backup.
- Needs CommonAPI and LDBTool.
- Saves made with this version need it: gold, silver and slime in a save are items of this mod.
- Other content mods: the mod finds its prerequisite technologies and its place on the tech page from the loaded tech tree, and it only moves a recipe to another mod's building type when a building of that type exists.

## Licence

MIT. Written from scratch; it contains no code or files of other mods.

## Building from source

You need a current .NET SDK and the .NET Framework 4.7.2 targeting pack. The game, Unity and BepInEx assemblies are referenced from your own installation and are not part of this repository.

```text
dotnet build -c Release "-p:DSPGamePath=D:\Games\Dyson Sphere Program" "-p:BepInExCorePath=C:\path\to\BepInEx\core"
```

The output is `bin/Release/net472/BUG_OreEnrichment.dll`. Copy it to `BepInEx/plugins/BUG_OreEnrichment/`.

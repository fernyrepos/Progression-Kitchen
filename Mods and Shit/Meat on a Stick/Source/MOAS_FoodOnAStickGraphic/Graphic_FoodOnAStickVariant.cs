using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ProgressionKitchen.MeatOnAStick
{
    // Picks which of the Expansion's per-type textures to show on the consolidated
    // "food on a stick" item, based on what was actually cooked into it, so none of
    // the bundled art goes unused. Reuses Graphic_StackCount's rendering/atlas plumbing
    // (including its own "_a"/"_b" random-variant convention per slot) and only
    // overrides which slot gets picked.
    public class Graphic_FoodOnAStickVariant : Graphic_StackCount
    {
        private static readonly string[] SubPaths =
        {
            "Things/Item/Meal/MeatOnAStick", // meat
            "Items/chrisb_moas_blend",       // meat + veg/fruit/fungus blend
            "Items/chrisb_moas_fish",        // fish
            "Items/chrisb_moas_fruit",       // fruit
            "Items/chrisb_moas_foas",        // fungus
            "Items/chrisb_moas_voas",        // vegetables
            "Items/chrisb_moas_meatless",    // no food ingredient (skewer only)
        };

        private const int IndexMeat = 0;
        private const int IndexBlend = 1;
        private const int IndexFish = 2;
        private const int IndexFruit = 3;
        private const int IndexFungus = 4;
        private const int IndexVeg = 5;
        private const int IndexMeatless = 6;

        // Vanilla items whose only "fruit" signal is that they were carved out of the
        // Expansion's own veg recipe by defName, not by category or foodType - mirror
        // that same defName check here instead of guessing at a category hierarchy.
        private static readonly HashSet<string> FruitThingDefNames = new HashSet<string> { "RawAgave", "RawBerries" };

        private static ThingCategoryDef fishCategory;
        private static bool fishCategoryResolved;

        private static ThingCategoryDef FishCategory
        {
            get
            {
                if (!fishCategoryResolved)
                {
                    fishCategory = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("Fish");
                    fishCategoryResolved = true;
                }
                return fishCategory;
            }
        }

        private static ThingCategoryDef fruitCategory;
        private static bool fruitCategoryResolved;

        private static ThingCategoryDef FruitCategory
        {
            get
            {
                if (!fruitCategoryResolved)
                {
                    fruitCategory = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("VCE_Fruit");
                    fruitCategoryResolved = true;
                }
                return fruitCategory;
            }
        }

        public override void Init(GraphicRequest req)
        {
            data = req.graphicData;
            path = req.path;
            color = req.color;
            colorTwo = req.colorTwo;
            drawSize = req.drawSize;

            subGraphics = new Graphic[SubPaths.Length];
            for (int i = 0; i < SubPaths.Length; i++)
            {
                subGraphics[i] = GraphicDatabase.Get<Graphic_StackCount>(SubPaths[i], req.shader, req.drawSize, req.color, req.colorTwo);
            }
        }

        public override Graphic SubGraphicFor(Thing thing)
        {
            return subGraphics[PickIndex(thing.TryGetComp<CompIngredients>()?.ingredients)];
        }

        private static int PickIndex(List<ThingDef> ingredients)
        {
            if (ingredients.NullOrEmpty())
            {
                return IndexMeatless;
            }

            bool hasMeat = false;
            bool hasFish = false;
            bool hasFungus = false;
            bool hasFruit = false;
            bool hasVeg = false;

            foreach (ThingDef ingredient in ingredients)
            {
                if (FishCategory != null && FishCategory.ContainedInThisOrDescendant(ingredient))
                {
                    hasFish = true;
                    continue;
                }

                FoodTypeFlags foodType = ingredient.ingestible?.foodType ?? FoodTypeFlags.None;

                if ((foodType & FoodTypeFlags.Meat) != 0)
                {
                    hasMeat = true;
                }
                else if ((foodType & FoodTypeFlags.Fungus) != 0)
                {
                    hasFungus = true;
                }
                else if (FruitThingDefNames.Contains(ingredient.defName) ||
                    (FruitCategory != null && FruitCategory.ContainedInThisOrDescendant(ingredient)))
                {
                    hasFruit = true;
                }
                else if ((foodType & FoodTypeFlags.VegetableOrFruit) != 0)
                {
                    hasVeg = true;
                }
            }

            if (hasFish)
            {
                return IndexFish;
            }

            if (hasMeat && (hasVeg || hasFruit || hasFungus))
            {
                return IndexBlend;
            }

            if (hasMeat)
            {
                return IndexMeat;
            }

            if (hasFruit)
            {
                return IndexFruit;
            }

            if (hasFungus)
            {
                return IndexFungus;
            }

            return IndexVeg;
        }
    }
}

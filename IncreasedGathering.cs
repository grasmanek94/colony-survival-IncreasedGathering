using System.IO;
using System.Collections.Generic;
using Pipliz;
using Jobs;
using static ModLoader;
using Recipes;
using NPC;

namespace grasmanek94.IncreasedGathering
{
    [ModManager]
    public static class IncreasedGathering
    {
        static int factor;
        static HashSet<ushort> jobs;
        static string types_file;

        [ModCallback(EModCallbackType.OnAssemblyLoaded, "OnAssemblyLoaded")]
        static void OnLoad(string assemblyPath)
        {
            factor = 10;
            jobs = new HashSet<ushort>();

            string config = Path.Combine(Path.GetDirectoryName(assemblyPath), "factor.txt");
            if (File.Exists(config))
            {
                string factor_data = File.ReadAllText(config);
                if (!int.TryParse(factor_data, out factor))
                {
                    Log.WriteError("IncreasedGathering: Failed to parse factor.txt");
                }
                else
                {
                    factor = Math.Clamp(factor, 1, int.MaxValue / 2);
                }
            }

            types_file = Path.Combine(Path.GetDirectoryName(assemblyPath), "types.txt");

            if (!File.Exists(types_file))
            {
                Log.WriteWarning("IncreasedGathering: No types.txt found at \"{0}\"", types_file);
                return;
            }

            Log.Write("IncreasedGathering: factor = {0}", factor);
        }

        [ModCallback(EModCallbackType.AfterWorldLoad, "AfterWorldLoad")]
        static void AfterWorldLoad()
        {         
            if (!File.Exists(types_file))
            {
                return;
            }

            string[] type_data = File.ReadAllLines(types_file);

            foreach (var type in type_data)
            {
                if(type.StartsWith("//"))
                {
                    continue;
                }

                NPCType value;
                if (NPCType.NPCTypesByKeyName.TryGetValue(type, out value))
                {
                    jobs.Add(value.Type);
                }
                else
                {
                    Log.WriteWarning("IncreasedGathering: Type \"{0}\" not found", type);
                }
            }

            Log.Write("IncreasedGathering: Loaded {0} types", jobs.Count);
        }

        static bool InvalidJobNPCType(IJob job)
        {
            return job == null || job.NPC == null || !jobs.Contains(job.NPC.NPCType.Type);
        }

        [ModCallback(EModCallbackType.OnNPCGathered, "OnNPCGathered")]
        static void OnNPCGathered(IJob job, Vector3Int pos, List<ItemTypes.ItemTypeDrops> items)
        {
            if(InvalidJobNPCType(job) || items == null)
            {
                return;
            }

            List<ItemTypes.ItemTypeDrops> adjusted_items = new List<ItemTypes.ItemTypeDrops>();
            foreach (var item in items)
            {
                adjusted_items.Add(new ItemTypes.ItemTypeDrops(item.Type, item.Amount * factor, item.chance));
            }

            items.Clear();

            foreach (var item in adjusted_items)
            {
                items.Add(item);
            }
        }

        [ModCallback(EModCallbackType.OnNPCCraftedRecipe, "OnNPCCraftedRecipe")]
        static void OnNPCCraftedRecipe(IJob job, Recipe recipe, List<RecipeResult> result)
        {
            if (InvalidJobNPCType(job) || result == null)
            {
                return;
            }

            List<RecipeResult> adjusted_items = new List<RecipeResult>();
            foreach (var item in result)
            {
                adjusted_items.Add(new RecipeResult(item.Type, item.Amount * factor, item.Chance, item.IsOptional));
            }

            result.Clear();

            foreach (var item in adjusted_items)
            {
                result.Add(item);
            }
        }
    }
}

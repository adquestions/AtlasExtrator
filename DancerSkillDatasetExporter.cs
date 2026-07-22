using System;
using System.IO;
using System.Text;

namespace AtlasExtractor
{
    internal static class DancerSkillDatasetExporter
    {
        public static int Export(string outputFolder)
        {
            string outputPath = Path.Combine(
                outputFolder,
                "dancer_skills.json");

            var encoding = new UTF8Encoding(true);

            using (var writer = new StreamWriter(
                outputPath,
                false,
                encoding))
            {
                writer.WriteLine("{");
                writer.WriteLine("  \"heroId\": 10005,");
                writer.WriteLine("  \"heroName\": \"Dancer\",");
                writer.WriteLine("  \"maxSkillLevel\": 15,");
                writer.WriteLine("  \"levelCaps\": [2, 5, 10, 15],");
                writer.WriteLine("  \"skills\": [");

                writer.WriteLine("    {");
                writer.WriteLine("      \"componentId\": 1000501,");
                writer.WriteLine("      \"name\": \"Resolute Dance\",");
                writer.WriteLine("      \"type\": \"activeBuff\",");
                writer.WriteLine("      \"effect\": \"attackSpeedPercent\",");
                writer.WriteLine("      \"baseValue\": 15.0,");
                writer.WriteLine("      \"valuePerLevel\": 0.25,");
                writer.WriteLine("      \"durationSeconds\": 7.0,");
                writer.WriteLine("      \"cooldownSeconds\": 10.0,");
                writer.WriteLine("      \"starBonuses\": [2.5, 2.5, 5.0],");
                writer.WriteLine("      \"formula\": \"15 + (skillLevel * 0.25)\",");
                writer.WriteLine("      \"verifiedCurrentExample\": {");
                writer.WriteLine("        \"skillLevel\": 2,");
                writer.WriteLine("        \"value\": 15.5");
                writer.WriteLine("      }");
                writer.WriteLine("    },");

                writer.WriteLine("    {");
                writer.WriteLine("      \"componentId\": 1000502,");
                writer.WriteLine("      \"name\": \"Tender Dance\",");
                writer.WriteLine("      \"type\": \"ultimateBuff\",");
                writer.WriteLine("      \"movementSpeedPercent\": 10.0,");
                writer.WriteLine("      \"attackPercent\": 50.0,");
                writer.WriteLine("      \"baseDurationSeconds\": 4.2,");
                writer.WriteLine("      \"durationPerLevelSeconds\": 0.05,");
                writer.WriteLine("      \"cooldownSeconds\": 24.0,");
                writer.WriteLine("      \"starDurationBonusesSeconds\": [0.7, 0.7, 1.4],");
                writer.WriteLine("      \"formula\": \"4.2 + (skillLevel * 0.05) + cumulativeStarBonus\",");
                writer.WriteLine("      \"verifiedCurrentExample\": {");
                writer.WriteLine("        \"skillLevel\": 2,");
                writer.WriteLine("        \"durationSeconds\": 4.3");
                writer.WriteLine("      },");
                writer.WriteLine("      \"maximumDurationSeconds\": 7.75");
                writer.WriteLine("    },");

                writer.WriteLine("    {");
                writer.WriteLine("      \"componentId\": 1000511,");
                writer.WriteLine("      \"name\": \"Moonlight Dancer\",");
                writer.WriteLine("      \"type\": \"passive\",");
                writer.WriteLine("      \"effect\": \"hpPercent\",");
                writer.WriteLine("      \"baseValuesByStarTier\": [5.0, 7.5, 10.0, 12.5],");
                writer.WriteLine("      \"valuePerLevel\": 0.5,");
                writer.WriteLine("      \"formula\": \"starTierBase + (skillLevel * 0.5)\",");
                writer.WriteLine("      \"verifiedCurrentExample\": {");
                writer.WriteLine("        \"skillLevel\": 2,");
                writer.WriteLine("        \"value\": 6.0");
                writer.WriteLine("      },");
                writer.WriteLine("      \"maximumValue\": 20.0");
                writer.WriteLine("    },");

                writer.WriteLine("    {");
                writer.WriteLine("      \"componentId\": 1000512,");
                writer.WriteLine("      \"name\": \"Cheerful Spirit\",");
                writer.WriteLine("      \"type\": \"passive\",");
                writer.WriteLine("      \"effect\": \"attackPercent\",");
                writer.WriteLine("      \"baseValuesByStarTier\": [5.0, 7.5, 10.0, 12.5],");
                writer.WriteLine("      \"valuePerLevel\": 0.5,");
                writer.WriteLine("      \"formula\": \"starTierBase + (skillLevel * 0.5)\",");
                writer.WriteLine("      \"verifiedCurrentExample\": {");
                writer.WriteLine("        \"skillLevel\": 2,");
                writer.WriteLine("        \"value\": 6.0");
                writer.WriteLine("      },");
                writer.WriteLine("      \"maximumValue\": 20.0");
                writer.WriteLine("    }");

                writer.WriteLine("  ]");
                writer.WriteLine("}");
            }

            Console.WriteLine();
            Console.WriteLine("Dancer skills");
            Console.WriteLine("------------------------------");
            Console.WriteLine("Skills exported: 4");
            Console.WriteLine("JSON: " + outputPath);

            return 4;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace AtlasExtractor
{
    internal static class DancerSkillDatasetExporter
    {
        public static int Export(string outputFolder)
        {
            List<Dictionary<string, string>> levelRows =
                ReadDancerLevelRows(outputFolder);

            Console.WriteLine(
                "Dancer level rows loaded: " +
                levelRows.Count);

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

                writer.WriteLine("  ],");
                WriteLevelProgression(
                    writer,
                    levelRows);
                writer.WriteLine("}");
            }

            Console.WriteLine();
            Console.WriteLine("Dancer dataset");
            Console.WriteLine("------------------------------");
            Console.WriteLine("Skills exported: 4");
            Console.WriteLine(
                "Level rows exported: " +
                levelRows.Count);
            Console.WriteLine("JSON: " + outputPath);

            return 4;
        }

        private static void WriteLevelProgression(
            StreamWriter writer,
            List<Dictionary<string, string>> levelRows)
        {
            writer.WriteLine("  \"levelProgression\": [");

            for (int index = 0;
                 index < levelRows.Count;
                 index++)
            {
                Dictionary<string, string> row =
                    levelRows[index];

                int level = ParseIntValue(row, "level");
                long meatCost = ParseLongValue(row, "exp");
                int capacity = ParseIntValue(row, "army_count");
                long attack = ParsePairAmount(
                    GetValue(row, "attr_1"));
                long hp = ParsePairAmount(
                    GetValue(row, "attr_2"));
                long influence = ParseLongValue(
                    row,
                    "fightforce");

                writer.WriteLine("    {");
                writer.WriteLine(
                    "      \"level\": " +
                    level + ",");
                writer.WriteLine(
                    "      \"meatCostToNextLevel\": " +
                    meatCost + ",");
                writer.WriteLine(
                    "      \"capacity\": " +
                    capacity + ",");
                writer.WriteLine(
                    "      \"baseAttack\": " +
                    attack + ",");
                writer.WriteLine(
                    "      \"baseHp\": " +
                    hp + ",");
                writer.WriteLine(
                    "      \"baseInfluence\": " +
                    influence);
                writer.WriteLine(
                    index < levelRows.Count - 1
                        ? "    },"
                        : "    }");
            }

            writer.WriteLine("  ]");
        }

        private static string GetValue(
            IDictionary<string, string> row,
            string column)
        {
            string value;

            return row != null &&
                   row.TryGetValue(column, out value)
                ? value ?? string.Empty
                : string.Empty;
        }

        private static int ParseIntValue(
            IDictionary<string, string> row,
            string column)
        {
            int value;

            return int.TryParse(
                    GetValue(row, column),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out value)
                ? value
                : 0;
        }

        private static long ParseLongValue(
            IDictionary<string, string> row,
            string column)
        {
            long value;

            return long.TryParse(
                    GetValue(row, column),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out value)
                ? value
                : 0L;
        }

        private static long ParsePairAmount(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0L;
            }

            int separator = value.LastIndexOf('|');
            int end = value.LastIndexOf(']');

            if (separator < 0 ||
                end <= separator)
            {
                return 0L;
            }

            long amount;

            return long.TryParse(
                    value.Substring(
                        separator + 1,
                        end - separator - 1),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out amount)
                ? amount
                : 0L;
        }

        private static List<Dictionary<string, string>>
            ReadDancerLevelRows(string outputFolder)
        {
            string path = Path.Combine(
                outputFolder,
                "hero_level.csv");

            List<Dictionary<string, string>> allRows =
                BuildingDatasetExporter.ReadCsv(path);

            var dancerRows =
                new List<Dictionary<string, string>>();

            foreach (Dictionary<string, string> row in allRows)
            {
                string quality;
                string mainAttribute;

                row.TryGetValue("quality", out quality);
                row.TryGetValue(
                    "mainAttribute",
                    out mainAttribute);

                if (quality == "5" &&
                    mainAttribute == "1")
                {
                    dancerRows.Add(row);
                }
            }

            dancerRows.Sort(
                delegate(
                    Dictionary<string, string> left,
                    Dictionary<string, string> right)
                {
                    int leftLevel;
                    int rightLevel;

                    int.TryParse(
                        left["level"],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out leftLevel);

                    int.TryParse(
                        right["level"],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out rightLevel);

                    return leftLevel.CompareTo(rightLevel);
                });

            return dancerRows;
        }    }
}









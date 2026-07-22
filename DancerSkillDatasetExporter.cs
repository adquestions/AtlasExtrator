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

            List<Dictionary<string, string>> starRows =
                ReadDancerStarRows(outputFolder);

            List<Dictionary<string, string>> traitRows =
                ReadDancerTraitRows(outputFolder);

            List<Dictionary<string, string>> traitProgressionRows =
                ReadDancerTraitProgressionRows(outputFolder);

            Console.WriteLine(
                "Dancer level rows loaded: " +
                levelRows.Count);

            Console.WriteLine(
                "Dancer star rows loaded: " +
                starRows.Count);

            Console.WriteLine(
                "Dancer trait rows loaded: " +
                traitRows.Count);

            Console.WriteLine(
                "Dancer trait progression rows loaded: " +
                traitProgressionRows.Count);

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
                writer.WriteLine(",");
                WriteStarProgression(
                    writer,
                    starRows);
                writer.WriteLine(",");
                WriteTraitProgression(
                    writer,
                    traitRows,
                    traitProgressionRows);
                writer.WriteLine("}");
            }

            Console.WriteLine();
            Console.WriteLine("Dancer dataset");
            Console.WriteLine("------------------------------");
            Console.WriteLine("Skills exported: 4");
            Console.WriteLine(
                "Level rows exported: " +
                levelRows.Count);
            Console.WriteLine(
                "Star rows exported: " +
                starRows.Count);
            Console.WriteLine(
                "Trait rows exported: " +
                traitRows.Count);
            Console.WriteLine(
                "Trait progression rows exported: " +
                traitProgressionRows.Count);
            Console.WriteLine("JSON: " + outputPath);

            return 4;
        }

        private static void WriteTraitProgression(
            StreamWriter writer,
            List<Dictionary<string, string>> traitRows,
            List<Dictionary<string, string>> traitProgressionRows)
        {
            writer.WriteLine("  \"traits\": [");

            for (int i = 0; i < traitRows.Count; i++)
            {
                Dictionary<string, string> trait = traitRows[i];
                string groupId = GetValue(trait, "talent_group");

                writer.WriteLine("    {");
                writer.WriteLine(
                    "      \"slot\": " +
                    ParseIntValue(trait, "slot") + ",");
                writer.WriteLine(
                    "      \"groupId\": " +
                    ParseIntValue(trait, "talent_group") + ",");
                writer.WriteLine(
                    "      \"unlockDiamondCost\": " +
                    ParsePairAmount(GetValue(trait, "cost")) + ",");
                writer.WriteLine(
                    "      \"shardsPerLevel\": " +
                    ParsePairAmount(GetValue(trait, "upgrade_cost")) + ",");
                writer.WriteLine("      \"levels\": [");

                List<Dictionary<string, string>> levels =
                    traitProgressionRows.FindAll(
                        row => GetValue(row, "group_id") == groupId);

                for (int levelIndex = 0; levelIndex < levels.Count; levelIndex++)
                {
                    Dictionary<string, string> level = levels[levelIndex];
                    string attributeValue = GetValue(level, "hero_attr");

                    if (string.IsNullOrWhiteSpace(attributeValue) || attributeValue == "[]")
                    {
                        attributeValue = GetValue(level, "player_attr");
                    }

                    writer.WriteLine("        {");
                    writer.WriteLine(
                        "          \"level\": " +
                        ParseIntValue(level, "level") + ",");
                    writer.WriteLine(
                        "          \"attributeId\": " +
                        ParsePairId(attributeValue) + ",");
                    writer.WriteLine(
                        "          \"attributeValue\": " +
                        ParsePairAmount(attributeValue) + ",");
                    writer.WriteLine(
                        "          \"power\": " +
                        ParseLongValue(level, "power"));
                    writer.Write("        }");

                    if (levelIndex < levels.Count - 1)
                    {
                        writer.Write(",");
                    }

                    writer.WriteLine();
                }

                writer.WriteLine("      ]");
                writer.Write("    }");

                if (i < traitRows.Count - 1)
                {
                    writer.Write(",");
                }

                writer.WriteLine();
            }

            writer.WriteLine("  ]");
        }

        private static void WriteStarProgression(
            StreamWriter writer,
            List<Dictionary<string, string>> starRows)
        {
            writer.WriteLine("  \"starProgression\": [");

            for (int index = 0;
                 index < starRows.Count;
                 index++)
            {
                Dictionary<string, string> row =
                    starRows[index];

                int star = ParseIntValue(row, "star");
                int substep = ParseIntValue(row, "star_sub");
                int shardCost = ParseIntValue(row, "cosr");
                long attackBonus = ParsePairAmount(
                    GetValue(row, "extra_1"));
                long hpBonus = ParsePairAmount(
                    GetValue(row, "extra_2"));
                double influenceMultiplier =
                    ParseDoubleValue(
                        row,
                        "fightforce_ratio");
                int skillId = ParseIntValue(row, "skill");
                int skillLevel =
                    ParseIntValue(row, "skill_level");

                writer.WriteLine("    {");
                writer.WriteLine(
                    "      \"star\": " +
                    star + ",");
                writer.WriteLine(
                    "      \"substep\": " +
                    substep + ",");
                writer.WriteLine(
                    "      \"shardCost\": " +
                    shardCost + ",");
                writer.WriteLine(
                    "      \"cumulativeAttackBonus\": " +
                    attackBonus + ",");
                writer.WriteLine(
                    "      \"cumulativeHpBonus\": " +
                    hpBonus + ",");
                writer.WriteLine(
                    "      \"influenceMultiplier\": " +
                    influenceMultiplier.ToString(
                        CultureInfo.InvariantCulture) + ",");
                writer.WriteLine(
                    "      \"skillId\": " +
                    skillId + ",");
                writer.WriteLine(
                    "      \"skillLevel\": " +
                    skillLevel);
                writer.WriteLine(
                    index < starRows.Count - 1
                        ? "    },"
                        : "    }");
            }

            writer.WriteLine("  ]");
        }

        private static double ParseDoubleValue(
            IDictionary<string, string> row,
            string column)
        {
            double value;

            return double.TryParse(
                    GetValue(row, column),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out value)
                ? value
                : 0D;
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

        private static int ParsePairId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            string cleaned = value.Trim('[', ']');
            string[] parts = cleaned.Split('|');

            if (parts.Length < 2)
            {
                return 0;
            }

            int result;

            return int.TryParse(
                parts[0],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out result)
                    ? result
                    : 0;
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
            ReadDancerTraitRows(string outputFolder)
        {
            string path = Path.Combine(
                outputFolder,
                "hero_talent_new.csv");

            List<Dictionary<string, string>> rows =
                BuildingDatasetExporter.ReadCsv(path);

            var results = new List<Dictionary<string, string>>();

            foreach (Dictionary<string, string> row in rows)
            {
                if (GetValue(row, "hero_id") == "10005")
                {
                    results.Add(row);
                }
            }

            results.Sort(
                (left, right) =>
                    ParseIntValue(left, "slot").CompareTo(
                        ParseIntValue(right, "slot")));

            return results;
        }

        private static List<Dictionary<string, string>>
            ReadDancerTraitProgressionRows(string outputFolder)
        {
            string path = Path.Combine(
                outputFolder,
                "hero_talent_group.csv");

            List<Dictionary<string, string>> rows =
                BuildingDatasetExporter.ReadCsv(path);

            var allowedGroups = new HashSet<string>
            {
                "40101",
                "40201",
                "10114",
                "40301"
            };

            var results = new List<Dictionary<string, string>>();

            foreach (Dictionary<string, string> row in rows)
            {
                if (allowedGroups.Contains(GetValue(row, "group_id")))
                {
                    results.Add(row);
                }
            }

            results.Sort(
                (left, right) =>
                {
                    int groupCompare =
                        ParseIntValue(left, "group_id").CompareTo(
                            ParseIntValue(right, "group_id"));

                    if (groupCompare != 0)
                    {
                        return groupCompare;
                    }

                    return ParseIntValue(left, "level").CompareTo(
                        ParseIntValue(right, "level"));
                });

            return results;
        }

        private static List<Dictionary<string, string>>
            ReadDancerStarRows(string outputFolder)
        {
            string path = Path.Combine(
                outputFolder,
                "hero_star.csv");

            List<Dictionary<string, string>> allRows =
                BuildingDatasetExporter.ReadCsv(path);

            var dancerRows =
                new List<Dictionary<string, string>>();

            foreach (Dictionary<string, string> row in allRows)
            {
                if (GetValue(row, "hero_id") == "10005")
                {
                    dancerRows.Add(row);
                }
            }

            dancerRows.Sort(
                delegate(
                    Dictionary<string, string> left,
                    Dictionary<string, string> right)
                {
                    int starComparison =
                        ParseIntValue(left, "star").CompareTo(
                            ParseIntValue(right, "star"));

                    return starComparison != 0
                        ? starComparison
                        : ParseIntValue(left, "star_sub").CompareTo(
                            ParseIntValue(right, "star_sub"));
                });

            return dancerRows;
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















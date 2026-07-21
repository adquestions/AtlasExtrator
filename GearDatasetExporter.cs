using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AtlasExtractor
{
    internal static class GearDatasetExporter
    {
        public static int Export(string outputFolder)
        {
            string starUpPath = Path.Combine(
                outputFolder,
                "equipment_star_up.csv");

            string breakPath = Path.Combine(
                outputFolder,
                "equipment_break.csv");

            RequireFile(starUpPath);
            RequireFile(breakPath);

            List<Dictionary<string, string>> rows =
                ReadCsv(starUpPath);

            List<GearLevelCostRecord> records =
                rows
                    .Where(row =>
                        Get(row, "model_id") == "1511")
                    .Select(row =>
                        new GearLevelCostRecord
                        {
                            Level = ParseInt(
                                Get(row, "star")),

                            GearEnhanceRunes =
                                ParseItemAmount(
                                    Get(
                                        row,
                                        "star_up_cost"),
                                    "206")
                        })
                    .OrderBy(record => record.Level)
                    .ToList();

            string outputPath = Path.Combine(
                outputFolder,
                "current_gear_level_costs.csv");

            WriteLevelCosts(
                outputPath,
                records);

            List<GearBreakCostRecord> breakRecords =
                ReadCsv(breakPath)
                    .Where(row =>
                        Get(row, "model_id") == "1511" &&
                        ParseInt(Get(row, "lv")) >= 1)
                    .Select(ParseBreakCost)
                    .OrderBy(record => record.BreakLevel)
                    .ToList();

            string breakOutputPath = Path.Combine(
                outputFolder,
                "current_gear_break_costs.csv");

            WriteBreakCosts(
                breakOutputPath,
                breakRecords);

            List<GearUpgradeTotalRecord> totalRecords =
                BuildUpgradeTotals(
                    records,
                    breakRecords);

            string totalsOutputPath = Path.Combine(
                outputFolder,
                "current_gear_upgrade_totals.csv");

            WriteUpgradeTotals(
                totalsOutputPath,
                totalRecords);

            Console.WriteLine();
            Console.WriteLine(
                "Current gear level costs");

            Console.WriteLine(
                "------------------------------");

            Console.WriteLine(
                "Rows exported: " +
                records.Count.ToString("N0"));

            Console.WriteLine(
                "Level CSV: " +
                outputPath);

            Console.WriteLine(
                "Break rows exported: " +
                breakRecords.Count.ToString("N0"));

            Console.WriteLine(
                "Break CSV: " +
                breakOutputPath);

            Console.WriteLine(
                "Totals rows exported: " +
                totalRecords.Count.ToString("N0"));

            Console.WriteLine(
                "Totals CSV: " +
                totalsOutputPath);

            return records.Count +
                   breakRecords.Count +
                   totalRecords.Count;
        }

        private static List<GearUpgradeTotalRecord>
            BuildUpgradeTotals(
                IEnumerable<GearLevelCostRecord> levelRecords,
                IEnumerable<GearBreakCostRecord> breakRecords)
        {
            int levelRunes =
                levelRecords.Sum(
                    record =>
                        record.GearEnhanceRunes);

            int breakRunes =
                breakRecords.Sum(
                    record =>
                        record.GearEnhanceRunes);

            int ingots =
                breakRecords.Sum(
                    record =>
                        record.SolarGoldIngots);

            int rubies =
                breakRecords.Sum(
                    record =>
                        record.Rubies);

            int legendaryStones =
                breakRecords.Sum(
                    record =>
                        record.LegendaryPromotionStones);

            int mythicStones =
                breakRecords.Sum(
                    record =>
                        record.MythicPromotionStones);

            var onePiece =
                new GearUpgradeTotalRecord
                {
                    Scope = "One Piece",
                    GearEnhanceRunes =
                        levelRunes + breakRunes,
                    SolarGoldIngots = ingots,
                    Rubies = rubies,
                    LegendaryPromotionStones =
                        legendaryStones,
                    MythicPromotionStones =
                        mythicStones
                };

            var fourPieceSet =
                new GearUpgradeTotalRecord
                {
                    Scope = "Four-Piece Set",
                    GearEnhanceRunes =
                        onePiece.GearEnhanceRunes * 4,
                    SolarGoldIngots =
                        onePiece.SolarGoldIngots * 4,
                    Rubies =
                        onePiece.Rubies * 4,
                    LegendaryPromotionStones =
                        onePiece.LegendaryPromotionStones * 4,
                    MythicPromotionStones =
                        onePiece.MythicPromotionStones * 4
                };

            return new List<GearUpgradeTotalRecord>
            {
                onePiece,
                fourPieceSet
            };
        }

        private static void WriteUpgradeTotals(
            string outputPath,
            IEnumerable<GearUpgradeTotalRecord> records)
        {
            var encoding =
                new UTF8Encoding(true);

            using (var writer = new StreamWriter(
                outputPath,
                false,
                encoding))
            {
                writer.WriteLine(
                    "Scope," +
                    "GearEnhanceRunes," +
                    "SolarGoldIngots," +
                    "Rubies," +
                    "LegendaryPromotionStones," +
                    "MythicPromotionStones");

                foreach (GearUpgradeTotalRecord record
                    in records)
                {
                    writer.WriteLine(
                        EscapeCsv(record.Scope) +
                        "," +
                        record.GearEnhanceRunes.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.SolarGoldIngots.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Rubies.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.LegendaryPromotionStones.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.MythicPromotionStones.ToString(
                            CultureInfo.InvariantCulture));
                }
            }
        }
        private static GearBreakCostRecord ParseBreakCost(
            Dictionary<string, string> row)
        {
            var record = new GearBreakCostRecord
            {
                BreakLevel =
                    ParseInt(Get(row, "lv"))
            };

            string costs =
                Get(row, "break_cost_goods") +
                "|" +
                Get(row, "break_cost_no_back");

            foreach (Match match in Regex.Matches(
                costs,
                @"(\d+)\|(\d+)\|(\d+)"))
            {
                string type =
                    match.Groups[1].Value;

                string id =
                    match.Groups[2].Value;

                int amount =
                    ParseInt(match.Groups[3].Value);

                string key =
                    type + "|" + id;

                switch (key)
                {
                    case "2|404100005":
                        record.SolarGoldIngots += amount;
                        break;

                    case "2|206":
                        record.GearEnhanceRunes += amount;
                        break;

                    case "999|6":
                        record.Rubies += amount;
                        break;

                    case "2|151":
                        record.LegendaryPromotionStones +=
                            amount;
                        break;

                    case "2|152":
                        record.MythicPromotionStones +=
                            amount;
                        break;
                }
            }

            return record;
        }

        private static void WriteBreakCosts(
            string outputPath,
            IEnumerable<GearBreakCostRecord> records)
        {
            var encoding =
                new UTF8Encoding(true);

            using (var writer = new StreamWriter(
                outputPath,
                false,
                encoding))
            {
                writer.WriteLine(
                    "BreakLevel," +
                    "SolarGoldIngots," +
                    "GearEnhanceRunes," +
                    "Rubies," +
                    "LegendaryPromotionStones," +
                    "MythicPromotionStones");

                foreach (GearBreakCostRecord record
                    in records)
                {
                    writer.WriteLine(
                        record.BreakLevel.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.SolarGoldIngots.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.GearEnhanceRunes.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Rubies.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.LegendaryPromotionStones.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.MythicPromotionStones.ToString(
                            CultureInfo.InvariantCulture));
                }
            }
        }
        private static int ParseItemAmount(
            string value,
            string itemId)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            Match match = Regex.Match(
                value,
                @"\[2\|" +
                Regex.Escape(itemId) +
                @"\|(\d+)\]");

            return match.Success
                ? ParseInt(match.Groups[1].Value)
                : 0;
        }

        private static void WriteLevelCosts(
            string outputPath,
            IEnumerable<GearLevelCostRecord> records)
        {
            var encoding =
                new UTF8Encoding(true);

            using (var writer = new StreamWriter(
                outputPath,
                false,
                encoding))
            {
                writer.WriteLine(
                    "Level,GearEnhanceRunes");

                foreach (GearLevelCostRecord record
                    in records)
                {
                    writer.WriteLine(
                        record.Level.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.GearEnhanceRunes.ToString(
                            CultureInfo.InvariantCulture));
                }
            }
        }

        private static List<Dictionary<string, string>>
            ReadCsv(string path)
        {
            var rows =
                new List<List<string>>();

            var currentRow =
                new List<string>();

            var currentField =
                new StringBuilder();

            bool insideQuotes = false;

            using (var reader = new StreamReader(
                path,
                Encoding.UTF8,
                true))
            {
                while (true)
                {
                    int next = reader.Read();

                    if (next < 0)
                    {
                        if (insideQuotes)
                        {
                            throw new InvalidDataException(
                                "CSV ended inside a quoted field: " +
                                path);
                        }

                        if (currentField.Length > 0 ||
                            currentRow.Count > 0)
                        {
                            currentRow.Add(
                                currentField.ToString());

                            rows.Add(currentRow);
                        }

                        break;
                    }

                    char character = (char)next;

                    if (insideQuotes)
                    {
                        if (character == '"')
                        {
                            if (reader.Peek() == '"')
                            {
                                reader.Read();
                                currentField.Append('"');
                            }
                            else
                            {
                                insideQuotes = false;
                            }
                        }
                        else
                        {
                            currentField.Append(character);
                        }

                        continue;
                    }

                    if (character == '"' &&
                        currentField.Length == 0)
                    {
                        insideQuotes = true;
                    }
                    else if (character == ',')
                    {
                        currentRow.Add(
                            currentField.ToString());

                        currentField.Clear();
                    }
                    else if (character == '\r')
                    {
                        if (reader.Peek() == '\n')
                        {
                            reader.Read();
                        }

                        currentRow.Add(
                            currentField.ToString());

                        rows.Add(currentRow);

                        currentRow =
                            new List<string>();

                        currentField.Clear();
                    }
                    else if (character == '\n')
                    {
                        currentRow.Add(
                            currentField.ToString());

                        rows.Add(currentRow);

                        currentRow =
                            new List<string>();

                        currentField.Clear();
                    }
                    else
                    {
                        currentField.Append(character);
                    }
                }
            }

            if (rows.Count == 0)
            {
                return new List<
                    Dictionary<string, string>>();
            }

            List<string> headers =
                rows[0];

            var result =
                new List<
                    Dictionary<string, string>>();

            for (int rowIndex = 1;
                 rowIndex < rows.Count;
                 rowIndex++)
            {
                List<string> values =
                    rows[rowIndex];

                if (values.Count == 1 &&
                    values[0].Length == 0)
                {
                    continue;
                }

                var dictionary =
                    new Dictionary<string, string>(
                        StringComparer.OrdinalIgnoreCase);

                for (int columnIndex = 0;
                     columnIndex < headers.Count;
                     columnIndex++)
                {
                    dictionary[headers[columnIndex]] =
                        columnIndex < values.Count
                            ? values[columnIndex]
                            : string.Empty;
                }

                result.Add(dictionary);
            }

            return result;
        }

        private static string Get(
            IDictionary<string, string> row,
            string column)
        {
            if (row == null)
            {
                return string.Empty;
            }

            string value;

            return row.TryGetValue(
                    column,
                    out value)
                ? value ?? string.Empty
                : string.Empty;
        }

        private static string EscapeCsv(string value)
        {
            value = value ?? string.Empty;

            bool requiresQuotes =
                value.IndexOf(',') >= 0 ||
                value.IndexOf('"') >= 0 ||
                value.IndexOf('\r') >= 0 ||
                value.IndexOf('\n') >= 0;

            if (!requiresQuotes)
            {
                return value;
            }

            return "\"" +
                   value.Replace("\"", "\"\"") +
                   "\"";
        }
        private static int ParseInt(string value)
        {
            int result;

            return int.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out result)
                ? result
                : 0;
        }

        private static void RequireFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "Required dataset file was not found.",
                    path);
            }
        }

        private sealed class GearUpgradeTotalRecord
        {
            public string Scope { get; set; }

            public int GearEnhanceRunes { get; set; }

            public int SolarGoldIngots { get; set; }

            public int Rubies { get; set; }

            public int LegendaryPromotionStones
            {
                get;
                set;
            }

            public int MythicPromotionStones
            {
                get;
                set;
            }
        }
        private sealed class GearBreakCostRecord
        {
            public int BreakLevel { get; set; }

            public int SolarGoldIngots { get; set; }

            public int GearEnhanceRunes { get; set; }

            public int Rubies { get; set; }

            public int LegendaryPromotionStones
            {
                get;
                set;
            }

            public int MythicPromotionStones
            {
                get;
                set;
            }
        }
        private sealed class GearLevelCostRecord
        {
            public int Level { get; set; }

            public int GearEnhanceRunes { get; set; }
        }
    }
}






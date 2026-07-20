using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace AtlasExtractor
{
    internal static class BuildingDatasetExporter
    {
        public static int Export(string outputFolder)
        {
            string buildingsPath = Path.Combine(
                outputFolder,
                "citybuilding.csv");

            string upgradesPath = Path.Combine(
                outputFolder,
                "citybuilding_lvup_new.csv");

            string goodsPath = Path.Combine(
                outputFolder,
                "goods.csv");

            string localizationPath = Path.Combine(
                outputFolder,
                "localization_en.csv");

            RequireFile(buildingsPath);
            RequireFile(upgradesPath);
            RequireFile(goodsPath);
            RequireFile(localizationPath);

            Dictionary<string, Dictionary<string, string>> buildings =
                ReadCsv(buildingsPath)
                    .Where(row => Get(row, "id").Length > 0)
                    .ToDictionary(
                        row => Get(row, "id"),
                        row => row,
                        StringComparer.Ordinal);

            Dictionary<string, Dictionary<string, string>> goods =
                ReadCsv(goodsPath)
                    .Where(row => Get(row, "id").Length > 0)
                    .ToDictionary(
                        row => Get(row, "id"),
                        row => row,
                        StringComparer.Ordinal);

            Dictionary<string, string> localization =
                ReadCsv(localizationPath)
                    .Where(row => Get(row, "key").Length > 0)
                    .ToDictionary(
                        row => Get(row, "key"),
                        row => Get(row, "value"),
                        StringComparer.Ordinal);

            List<Dictionary<string, string>> upgradeRows =
                ReadCsv(upgradesPath);

            var records = new List<BuildingUpgradeRecord>();

            foreach (Dictionary<string, string> upgrade
                in upgradeRows)
            {
                string buildingId = Get(upgrade, "index");

                Dictionary<string, string> building;

                buildings.TryGetValue(
                    buildingId,
                    out building);

                int targetLevel =
                    ParseInt(Get(upgrade, "lv"));

                var record = new BuildingUpgradeRecord
                {
                    UpgradeId = Get(upgrade, "id"),
                    BuildingId = buildingId,

                    BuildingNameKey =
                        Get(building, "name"),

                    BuildingName =
                        Resolve(
                            localization,
                            Get(building, "name")),

                    BuildingDescriptionKey =
                        Get(building, "des"),

                    BuildingDescription =
                        Resolve(
                            localization,
                            Get(building, "des")),

                    CurrentLevel = targetLevel,

                    TargetLevel =
                        targetLevel + 1,

                    BuildTimeSeconds =
                        ParseLong(
                            Get(upgrade, "buildTime")),

                    RequiredTeamLevel =
                        ParseInt(
                            Get(upgrade, "teamlevel")),

                    RequiredBuildingLevel =
                        ParseInt(
                            Get(
                                upgrade,
                                "pre_building_level")),

                    FightForce =
                        ParseDouble(
                            Get(upgrade, "fightforce")),

                    Model =
                        Get(upgrade, "model")
                };

                record.Resource1 =
                    ParseResource(
                        Get(upgrade, "cost1"),
                        goods,
                        localization);

                record.Resource2 =
                    ParseResource(
                        Get(upgrade, "cost2"),
                        goods,
                        localization);

                record.Resource3 =
                    ParseResource(
                        Get(upgrade, "cost3"),
                        goods,
                        localization);

                records.Add(record);
            }

            records = records
                .OrderBy(record =>
                    ParseInt(record.BuildingId))
                .ThenBy(record =>
                    record.TargetLevel)
                .ToList();

            string csvPath = Path.Combine(
                outputFolder,
                "building_upgrades_en.csv");

            string jsonPath = Path.Combine(
                outputFolder,
                "building_upgrades_en.json");

            WriteCsv(csvPath, records);
            WriteJson(jsonPath, records);

            Console.WriteLine();
            Console.WriteLine("Building dataset");
            Console.WriteLine("------------------------------");
            Console.WriteLine(
                "Upgrade records exported: " +
                records.Count.ToString("N0"));

            Console.WriteLine("CSV:  " + csvPath);
            Console.WriteLine("JSON: " + jsonPath);

            return records.Count;
        }

        private static ResourceCost ParseResource(
            string rawCost,
            IDictionary<string,
                Dictionary<string, string>> goods,
            IDictionary<string, string> localization)
        {
            var result = new ResourceCost
            {
                RawValue = rawCost ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(rawCost))
            {
                return result;
            }

            string[] parts =
                rawCost.Split('|');

            if (parts.Length < 3)
            {
                return result;
            }

            /*
             * Cost format:
             * category | resource ID | amount
             */
            result.ResourceId =
            parts[1].Trim().Trim('[', ']');

            result.Amount =
            ParseDouble(
            parts[2].Trim().Trim('[', ']'));

            Dictionary<string, string> good;

            if (goods.TryGetValue(
                    result.ResourceId,
                    out good))
            {
                result.ResourceNameKey =
                    Get(good, "name");

                result.ResourceName =
                    Resolve(
                        localization,
                        result.ResourceNameKey);
            }

            return result;
        }

        private static void WriteCsv(
            string outputPath,
            IEnumerable<BuildingUpgradeRecord> records)
        {
            var encoding =
                new UTF8Encoding(true);

            using (var writer = new StreamWriter(
                outputPath,
                false,
                encoding))
            {
                writer.WriteLine(
                    "upgrade_id," +
                    "building_id," +
                    "building_name," +
                    "building_name_key," +
                    "building_description," +
                    "building_description_key," +
                    "current_level," +
                    "target_level," +
                    "build_time_seconds," +
                    "required_team_level," +
                    "required_building_level," +
                    "fight_force," +
                    "resource_1_id," +
                    "resource_1_name," +
                    "resource_1_amount," +
                    "resource_2_id," +
                    "resource_2_name," +
                    "resource_2_amount," +
                    "resource_3_id," +
                    "resource_3_name," +
                    "resource_3_amount," +
                    "model");

                foreach (BuildingUpgradeRecord record
                    in records)
                {
                    var values = new[]
                    {
                        record.UpgradeId,
                        record.BuildingId,
                        record.BuildingName,
                        record.BuildingNameKey,
                        record.BuildingDescription,
                        record.BuildingDescriptionKey,
                        record.CurrentLevel.ToString(
                            CultureInfo.InvariantCulture),
                        record.TargetLevel.ToString(
                            CultureInfo.InvariantCulture),
                        record.BuildTimeSeconds.ToString(
                            CultureInfo.InvariantCulture),
                        record.RequiredTeamLevel.ToString(
                            CultureInfo.InvariantCulture),
                        record.RequiredBuildingLevel.ToString(
                            CultureInfo.InvariantCulture),
                        record.FightForce.ToString(
                            CultureInfo.InvariantCulture),
                        record.Resource1.ResourceId,
                        record.Resource1.ResourceName,
                        FormatNumber(
                            record.Resource1.Amount),
                        record.Resource2.ResourceId,
                        record.Resource2.ResourceName,
                        FormatNumber(
                            record.Resource2.Amount),
                        record.Resource3.ResourceId,
                        record.Resource3.ResourceName,
                        FormatNumber(
                            record.Resource3.Amount),
                        record.Model
                    };

                    writer.WriteLine(
                        string.Join(
                            ",",
                            values.Select(EscapeCsv)));
                }
            }
        }

        private static void WriteJson(
            string outputPath,
            IList<BuildingUpgradeRecord> records)
        {
            var encoding =
                new UTF8Encoding(false);

            using (var writer = new StreamWriter(
                outputPath,
                false,
                encoding))
            {
                writer.WriteLine("[");

                for (int index = 0;
                     index < records.Count;
                     index++)
                {
                    BuildingUpgradeRecord record =
                        records[index];

                    writer.WriteLine("  {");
                    WriteJsonString(
                        writer,
                        "upgrade_id",
                        record.UpgradeId,
                        true);

                    WriteJsonString(
                        writer,
                        "building_id",
                        record.BuildingId,
                        true);

                    WriteJsonString(
                        writer,
                        "building_name",
                        record.BuildingName,
                        true);

                    WriteJsonString(
                        writer,
                        "building_name_key",
                        record.BuildingNameKey,
                        true);

                    WriteJsonString(
                        writer,
                        "building_description",
                        record.BuildingDescription,
                        true);

                    writer.WriteLine(
                        "    \"current_level\": " +
                        record.CurrentLevel + ",");

                    writer.WriteLine(
                        "    \"target_level\": " +
                        record.TargetLevel + ",");

                    writer.WriteLine(
                        "    \"build_time_seconds\": " +
                        record.BuildTimeSeconds + ",");

                    writer.WriteLine(
                        "    \"required_team_level\": " +
                        record.RequiredTeamLevel + ",");

                    writer.WriteLine(
                        "    \"required_building_level\": " +
                        record.RequiredBuildingLevel + ",");

                    writer.WriteLine(
                        "    \"fight_force\": " +
                        FormatNumber(record.FightForce) +
                        ",");

                    WriteJsonResource(
                        writer,
                        "resource_1",
                        record.Resource1,
                        true);

                    WriteJsonResource(
                        writer,
                        "resource_2",
                        record.Resource2,
                        true);

                    WriteJsonResource(
                        writer,
                        "resource_3",
                        record.Resource3,
                        true);

                    WriteJsonString(
                        writer,
                        "model",
                        record.Model,
                        false);

                    writer.Write("  }");

                    if (index < records.Count - 1)
                    {
                        writer.Write(",");
                    }

                    writer.WriteLine();
                }

                writer.WriteLine("]");
            }
        }

        private static void WriteJsonResource(
            TextWriter writer,
            string propertyName,
            ResourceCost cost,
            bool appendComma)
        {
            writer.WriteLine(
                "    \"" +
                EscapeJson(propertyName) +
                "\": {");

            WriteJsonString(
                writer,
                "id",
                cost.ResourceId,
                true,
                6);

            WriteJsonString(
                writer,
                "name",
                cost.ResourceName,
                true,
                6);

            WriteJsonString(
                writer,
                "name_key",
                cost.ResourceNameKey,
                true,
                6);

            writer.WriteLine(
                "      \"amount\": " +
                FormatNumber(cost.Amount));

            writer.Write("    }");

            if (appendComma)
            {
                writer.Write(",");
            }

            writer.WriteLine();
        }

        private static void WriteJsonString(
            TextWriter writer,
            string propertyName,
            string value,
            bool appendComma,
            int spaces = 4)
        {
            writer.Write(
                new string(' ', spaces));

            writer.Write("\"");
            writer.Write(EscapeJson(propertyName));
            writer.Write("\": \"");
            writer.Write(EscapeJson(value));
            writer.Write("\"");

            if (appendComma)
            {
                writer.Write(",");
            }

            writer.WriteLine();
        }

        private static string Resolve(
            IDictionary<string, string> localization,
            string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            string value;

            return localization.TryGetValue(
                    key,
                    out value)
                ? value
                : key;
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

            return row.TryGetValue(column, out value)
                ? value ?? string.Empty
                : string.Empty;
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

        private static long ParseLong(string value)
        {
            long result;

            return long.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out result)
                ? result
                : 0L;
        }

        private static double ParseDouble(string value)
        {
            double result;

            return double.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out result)
                ? result
                : 0D;
        }

        private static string FormatNumber(double value)
        {
            return value.ToString(
                "0.################",
                CultureInfo.InvariantCulture);
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

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var result =
                new StringBuilder();

            foreach (char character in value)
            {
                switch (character)
                {
                    case '\\':
                        result.Append("\\\\");
                        break;

                    case '"':
                        result.Append("\\\"");
                        break;

                    case '\r':
                        result.Append("\\r");
                        break;

                    case '\n':
                        result.Append("\\n");
                        break;

                    case '\t':
                        result.Append("\\t");
                        break;

                    default:
                        if (character < 32)
                        {
                            result.Append(
                                "\\u" +
                                ((int)character).ToString(
                                    "x4"));
                        }
                        else
                        {
                            result.Append(character);
                        }

                        break;
                }
            }

            return result.ToString();
        }

        private sealed class BuildingUpgradeRecord
        {
            public string UpgradeId = string.Empty;
            public string BuildingId = string.Empty;
            public string BuildingName = string.Empty;
            public string BuildingNameKey = string.Empty;
            public string BuildingDescription = string.Empty;
            public string BuildingDescriptionKey = string.Empty;
            public int CurrentLevel;
            public int TargetLevel;
            public long BuildTimeSeconds;
            public int RequiredTeamLevel;
            public int RequiredBuildingLevel;
            public double FightForce;
            public string Model = string.Empty;

            public ResourceCost Resource1 =
                new ResourceCost();

            public ResourceCost Resource2 =
                new ResourceCost();

            public ResourceCost Resource3 =
                new ResourceCost();
        }

        private sealed class ResourceCost
        {
            public string ResourceId = string.Empty;
            public string ResourceName = string.Empty;
            public string ResourceNameKey = string.Empty;
            public string RawValue = string.Empty;
            public double Amount;
        }
    }
}

namespace AtlasExtractor.Models
{
    internal sealed class ExportResult
    {
        public string TableName { get; set; }

        public string MetaTypeName { get; set; }

        public bool Success { get; set; }

        public long RowCount { get; set; }

        public string OutputPath { get; set; }

        public string ErrorMessage { get; set; }
    }
}

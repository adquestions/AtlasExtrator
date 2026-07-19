using System.Collections.Generic;

namespace AtlasExtractor.Models
{
    internal sealed class ExportSummary
    {
        public ExportSummary()
        {
            Results = new List<ExportResult>();
        }

        public int TablesDiscovered { get; set; }

        public int TablesExported { get; set; }

        public int TablesEmpty { get; set; }

        public int TablesFailed { get; set; }

        public long RowsExported { get; set; }

        public List<ExportResult> Results { get; private set; }
    }
}

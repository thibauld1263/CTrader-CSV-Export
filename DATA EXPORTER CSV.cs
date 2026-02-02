using System;
using System.IO;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class BacktestDataExporter : Robot
    {
        [Parameter("Start Date (UTC)", DefaultValue = "2024-01-01")]
        public DateTime StartDate { get; set; }

        [Parameter("File Name", DefaultValue = "BacktestDataExport")]
        public string FileName { get; set; }

        private StreamWriter _writer;
        private DateTime _lastExportedTime = DateTime.MinValue;

        protected override void OnStart()
        {
            // Define the output path (Desktop)
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string fullPath = Path.Combine(desktopPath, FileName + ".csv");

            try
            {
                // Initialize the writer (append: false to overwrite on new run)
                _writer = new StreamWriter(fullPath, false);
                
                // Write CSV Header
                _writer.WriteLine("Date,Open,High,Low,Close,Volume");
                Print($"Export started. Writing to: {fullPath}");

                // Export existing history (Bars loaded before backtest start)
                // We assume Bars.Count - 1 is the currently forming bar, so we stop at Count - 2
                // We guard against empty history with Math.Max
                int limit = Bars.Count - 1; 

                for (int i = 0; i < limit; i++)
                {
                    ExportBar(i);
                }
            }
            catch (Exception ex)
            {
                Print("Error initializing exporter: " + ex.Message);
                Stop();
            }
        }

        protected override void OnBar()
        {
            // triggered when a new bar opens. 
            // The previously closed bar is at index: Bars.Count - 2
            // We check the last few bars just to be safe and ensure continuity
            
            // Loop from the bar just before the last one, up to the closed bar
            int closedBarIndex = Bars.Count - 2;
            
            if (closedBarIndex >= 0)
            {
                ExportBar(closedBarIndex);
            }
        }

        private void ExportBar(int index)
        {
            if (index < 0 || index >= Bars.Count) return;

            DateTime barTime = Bars.OpenTimes[index];

            // Filter by date and ensure we don't export duplicates
            if (barTime >= StartDate && barTime > _lastExportedTime)
            {
                // Format: yyyy-MM-dd HH:mm:ss, InvariantCulture for dot decimals
                string line = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:yyyy-MM-dd HH:mm:ss},{1},{2},{3},{4},{5}",
                    barTime,
                    Bars.OpenPrices[index],
                    Bars.HighPrices[index],
                    Bars.LowPrices[index],
                    Bars.ClosePrices[index],
                    Bars.TickVolumes[index]);

                _writer.WriteLine(line);
                _writer.Flush(); // Flush immediately so data is visible if backtest stops/crashes
                
                _lastExportedTime = barTime;
            }
        }

        protected override void OnStop()
        {
            if (_writer != null)
            {
                _writer.Close();
                _writer.Dispose();
                _writer = null;
            }
            Print("Export stopped.");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;

namespace heatmap
{
    public class CsvOutput
    {
        public DateTime date { get; set; }
        public string countystate { get; set; }
        public string county { get; set; }
        public string state { get; set; }
        public int cases { get; set; }
        public int deaths { get; set; }
        public double? lat { get; set; }
        public double? lng { get; set; }
        public double monthly_change_in_deaths { get; set; }
        public double montly_change_in_cases { get; set; }
    }
    public class CsvInput
    {
        public string date { get; set; }
        public string county { get; set; }
        public string state { get; set; }
        public int cases { get; set; }
        public int deaths { get; set; }
        public double? lat { get; set; }
        public double? lng { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var monthlyDeaths = new Dictionary<string, Tuple<int, int>>();
            var monthlyCases = new Dictionary<string, Tuple<int, int>>();
            var records = new List<CsvOutput>();
            using(var reader = new StreamReader(@"C:\src-other\Web\heatmap\us_geocoded.csv"))
            using(var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                records = csv.GetRecords<CsvInput>()
                    .Select(x => new CsvOutput {
                        date = DateTime.Parse(x.date),
                        county = x.county,
                        state = x.state,
                        lat = x.lat,
                        lng = x.lng,
                        countystate = $"{x.county}, {x.state}"
                    })
                    .ToList();


                var monthlyData = records.GroupBy(x => x.date.Month).ToList();

                Parallel.ForEach(monthlyData.Select((x, idx) => (x, idx)), x =>
                {
                    var m = x.x;
                    var idx = x.idx;
                    var stateGroups = m.GroupBy(x => x.countystate).ToList();
                    if(idx > 1)
                    {
                        foreach (var s in stateGroups)
                        {
                            var sFirst = s.First();
                            var previousMonthsDeaths = monthlyData
                                .Where(x => x.Key == s.First().date.Month)
                                .SelectMany(x => x)
                                .Select(x => x.deaths)
                                .Sum();

                            var previousMonthsCases = monthlyData
                                .Where(x => x.Key == s.First().date.Month)
                                .SelectMany(x => x)
                                .Select(x => x.cases)
                                .Sum();

                            var totalDeaths = s.Sum(x => x.deaths);
                            var totalCases = s.Sum(x => x.cases);

                            var recordCount = s.Count();

                            double changeInDeaths = totalDeaths > 0 ? previousMonthsDeaths / totalDeaths : 0;
                            double changeInCases = totalCases > 0 ? previousMonthsCases / totalCases : 0;
                            records
                                .Where(x => x.state == sFirst.state && x.county == sFirst.county && x.date.Equals(sFirst.date))
                                .ToList()
                                .ForEach(x => {
                                    x.monthly_change_in_deaths = changeInDeaths;
                                    x.montly_change_in_cases = changeInCases;
                                });
                        }
                    }
                });
            }
        using (var writer = new StreamWriter(@"C:\src-other\Web\heatmap\us_features.csv"))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(records);
        }

        }
    }
}

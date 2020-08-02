using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Newtonsoft.Json;

namespace heatmap
{
    public class CsvOutput
    {
        public string date { get; set; }
        public string county { get; set; }
        public string state { get; set; }
        public int cases { get; set; }
        public int deaths { get; set; }
        public double? lat { get; set; }
        public double? lng { get; set; }
        public double monthly_change_in_deaths { get; set; }
        public double monthly_change_in_cases { get; set; }
    }

    public class GeoCsv
    {
        public string ID { get; set; }
        public string STATE_CODE { get; set; }
        public string STATE_NAME { get; set; }
        public string CITY { get; set; }
        public string COUNTY { get; set; }
        public double LATITUDE { get; set; }
        public double LONGITUDE { get; set; }

    }
    public class CsvProcess
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
        public double monthly_change_in_cases { get; set; }
    }
    public class CsvInput
    {
        public string date { get; set; }
        public string county { get; set; }
        public string state { get; set; }
        public int cases { get; set; }
        public int deaths { get; set; }
    }

    public class RecordData
    {
        public int Total { get; set; }
        public int Count { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var geoData = new List<GeoCsv>();
            using (var reader = new StreamReader(@"C:\src-other\Web\heatmap\us_cities.csv"))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                geoData = csv.GetRecords<GeoCsv>().ToList();
            }
            var monthlyDeaths = new Dictionary<string, RecordData>();
            var monthlyCases = new Dictionary<string, RecordData>();
            var geoDict = new Dictionary<string, Tuple<double, double>>();
            var records = new List<CsvProcess>();
            using (var reader = new StreamReader(@"C:\src-other\Web\heatmap\us.csv"))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                records = csv.GetRecords<CsvInput>()
                    .Select(x => new CsvProcess
                    {
                        date = DateTime.Parse(x.date),
                        county = x.county,
                        state = x.state,
                        cases = x.cases,
                        deaths = x.deaths,
                        countystate = $"{x.county}, {x.state}"
                    })
                    .ToList();


                foreach(var r in records)
                {
                    var key = $"{r.date.Month} {r.state} {r.county}";
                    if (monthlyCases.ContainsKey(key))
                    {
                        var caseData = monthlyCases[key];
                        caseData.Count++;
                        caseData.Total += r.cases;
                        monthlyCases[key] = caseData;

                        var deathData = monthlyDeaths[key];
                        deathData.Count++;
                        deathData.Total += r.deaths;
                        monthlyDeaths[key] = deathData;
                    }
                    else
                    {
                        var caseData = new RecordData()
                        {
                            Count = 1,
                            Total = r.cases,
                        };
                        monthlyCases[key] = caseData;

                        var deathData = new RecordData()
                        {
                            Count = 1,
                            Total = r.deaths,
                        };

                        monthlyDeaths[key] = deathData;
                    }

                    var geoKey = $"{r.county} {r.state}";
                    if(!geoDict.ContainsKey(geoKey))
                    {
                        var geo = geoData.FirstOrDefault(x => x.STATE_NAME == r.state && x.COUNTY == r.county);

                        if(geo != null)
                            geoDict[geoKey] = new Tuple<double, double>(geo.LATITUDE, geo.LONGITUDE);
                        else
                            geoDict[geoKey] = new Tuple<double, double>(0, 0);
                    }
                }

                foreach(var r in records)
                {
                    var key = $"{r.date.Month} {r.state} {r.county}";
                    var prevKey = $"{r.date.Month - 1} {r.state} {r.county}";
                    (var lat, var lng) = geoDict[$"{r.county} {r.state}"];
                    r.lat = lat;
                    r.lng = lng;

                    if(monthlyCases.ContainsKey(prevKey))
                    {
                        var cases = monthlyCases[key];
                        var prevCases = monthlyCases[prevKey];
                        r.monthly_change_in_cases = prevCases.Total > 0 ? (cases.Total - prevCases.Total) / (double)prevCases.Total : 0;

                        var deaths = monthlyDeaths[key];
                        var prevDeaths = monthlyDeaths[prevKey];
                        r.monthly_change_in_deaths = prevDeaths.Total > 0 ? (deaths.Total - prevDeaths.Total) / (double)prevDeaths.Total : 0;
                    }

                }
            }

            var fmt = "00";
            var output = records.Select(x => new CsvOutput {
                    date = $"{x.date.Year}-{x.date.Month.ToString(fmt)}-{x.date.Day.ToString(fmt)}",
                    county = x.county,
                    state = x.state,
                    cases = x.cases,
                    deaths = x.deaths,
                    lat = x.lat,
                    lng = x.lng,
                    monthly_change_in_cases = x.monthly_change_in_cases,
                    monthly_change_in_deaths = x.monthly_change_in_deaths,
                });

            using (var writer = new StreamWriter(@"C:\src-other\Web\heatmap\us_features.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(records);
            }

            if(File.Exists(@"C:\src-other\Web\heatmap\dates.json"))
                File.Delete(@"C:\src-other\Web\heatmap\dates.json");

            if(File.Exists(@"C:\src-other\Web\heatmap\us_features.json"))
                File.Delete(@"C:\src-other\Web\heatmap\us_features.json");


            File.WriteAllText(@"C:\src-other\Web\heatmap\us_features.json", JsonConvert.SerializeObject(output));

            var dates = output.Select(x => x.date)
                .Distinct()
                .ToList();

            File.WriteAllText(@"C:\src-other\Web\heatmap\dates.json",
                JsonConvert.SerializeObject(dates));




        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Threading;
using System.IO;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;

namespace PubNubMessaging.Core
{
    public class PerformanceMonitor
    {
        private string publisherKey = "demo";
        private string subscriberKey = "demo";
        private string origin = "pubsub.pubnub.com";
        private string secretKey = "";
        private string cipherKey = "";
        private bool ssl = false;

        private const int maximumLatency = 2000;
        private const int publishIntervalInMilliseconds = 500;

        private static string channel = "";
        private static bool _isRun = false;

        private static int numberOfPublishedMessages = 0;

        private static string message = "1";
        private int startPublishTime;

        private static long averageTimeLatency = 0;
        private static double mps_avg = 0;
        private static double latencyAverage = 0;
        private static List<long> latencyCollection;

        private double[] median;
        private int medianLength;
        private int updateInterval;
        private int publishTimeout;

        //private static Dictionary<string, Stopwatch> stopwatchDictionary;
        private static Stopwatch publishStopwatch;
        private static Pubnub pubnub;
        private static int maximumPublishCount = 50;

        static public void Main(string[] args)
        {
            if (args != null && args.Length == 1)
            {
                Int32.TryParse(args[0].ToString(), out maximumPublishCount);
                if (maximumPublishCount <= 0) maximumPublishCount = 50;
            }
            pubnub = new Pubnub("demo", "demo");

            int beginCursorLeftPosition = Console.CursorLeft;
            int beginCursorTopPosition = Console.CursorTop;
            Console.SetCursorPosition(beginCursorLeftPosition, beginCursorTopPosition);

            Start();

            //Console.WriteLine("Press any key for SD calculation");
            //Console.ReadLine();

            Console.ReadLine();
        }

        private static void DisplayStatusMessage(string message)
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(message);
        }


        private static long NowInMilliseconds()
        {
            TimeSpan timeSpan = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long time = Convert.ToInt64(timeSpan.TotalMilliseconds);
            return time;

        }
        public PerformanceMonitor()
        {

        }

        public static void Start()
        {
            _isRun = true;
            publishStopwatch = new Stopwatch();
            latencyCollection = new List<long>();
            channel = string.Format("performance-meter-{0}{1}", NowInMilliseconds(), new Random().NextDouble());
            numberOfPublishedMessages = 0;

            Subscribe();
        }

        private static void Subscribe()
        {
            pubnub.Subscribe<string>(channel, SubscribeUserCallback, SubscribeConnectCallback);
        }

        private static void SubscribeUserCallback(string receivedResponse)
        {
            List<object> receivedObject = JsonConvert.DeserializeObject<List<object>>(receivedResponse);

            //string messageChannel = receivedObject[2].ToString();

            //Debug.WriteLine("************************");

            publishStopwatch.Stop();

            long elapsedTimeLatency = publishStopwatch.ElapsedMilliseconds;

            if (elapsedTimeLatency <= 0 && latencyCollection.Count > 0)
            {
                elapsedTimeLatency = latencyCollection[0];
            }

            double new_mps_avg = 1000 / elapsedTimeLatency;

            //Debug.WriteLine(string.Format("Latency={0}", elapsedTimeLatency));
            if (latencyCollection != null && latencyCollection.Count > 0)
            {
                //Debug.WriteLine(string.Format("latencyCollection[0]={0}", latencyCollection[0]));
            }

            numberOfPublishedMessages++;

            //Debug.WriteLine(string.Format("numberOfPublishedMessages={0}", numberOfPublishedMessages));

            latencyAverage = (elapsedTimeLatency + latencyAverage) / 2;
            mps_avg = (new_mps_avg + mps_avg) / 2;

            latencyCollection.Add(elapsedTimeLatency);

            publishStopwatch.Reset();
            publishStopwatch.Start();

            if (numberOfPublishedMessages != maximumPublishCount)
            {
                Publish();
            }
            else
            {
                StandardNormalDistributionPoints(latencyCollection, true);
                StandardNormalDistributionPoints(latencyCollection, false);
            }

            //Thread.Sleep(publishIntervalInMilliseconds);

            //calculateMedian();
            Console.Clear();
            Console.SetCursorPosition(0, 2);
            Console.WriteLine("{0} Performance Samples Recorded", numberOfPublishedMessages);

            Console.SetCursorPosition(0, 4);

            StandardDeviationOfLatencyCollection();
            //Console.ReadLine();

        }

        private static void SubscribeConnectCallback(string receivedResponse)
        {
            DisplayStatusMessage(receivedResponse);

            Publish();
        }

        private static void Publish()
        {
            publishStopwatch.Start();

            pubnub.Publish<string>(channel, message, PublishCallback);
        }

        private static void PublishCallback(string receivedResponse)
        {
            DisplayStatusMessage(receivedResponse);
        }

        private static double GetMedian()
        {
            latencyCollection.Sort();
            int size = latencyCollection.Count;
            int middle = size / 2;

            double median = (size % 2 != 0) ? latencyCollection[middle] : (latencyCollection[middle] + latencyCollection[middle - 1]) / 2;

            return median;
        }

        private static double StandardDeviationOfLatencyCollection()
        {
            if (latencyCollection != null && latencyCollection.Count > 0)
            {
                double[] targetPercentageLookup1 = { 1, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 };
                double[] targetPercentageLookup2 = { 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 99 };
                //string[] targetPercentageLookup1 = { "Fastest", "5%", "10%", "20%", "25%", "30%", "40%", "45%" };
                //string[] targetPercentageLookup2 = { "50%", "66%", "75%", "80%", "90%", "95%", "98%", "Slowest" };

                latencyCollection.Sort();
                //Console.WriteLine(string.Join(",",latencyCollection.ToArray()));
                double mean = latencyCollection.Average();
                double sd = Math.Sqrt(latencyCollection.Average(data => Math.Pow(data - mean, 2)));

                List<CumulativePercentage> lookupList = CalculateBellCurveCumulativePercentile(mean, sd, latencyCollection);
                List<BellCurveDensity> densityList = CalculateBellCurveDensity(mean, sd, latencyCollection);
                List<BellCurveStandardDeviationScore> graphPoint = CalculateBellCurveStandardDeviationScores(mean, sd, latencyCollection);

                //using (StreamWriter streamWriter = new StreamWriter(@"C:\Pandu\GitHub\pubnub-api\csharp\3.4\graphlist.txt", false))
                //{
                //    graphPoint.ForEach(item =>
                //    {
                //        streamWriter.WriteLine(string.Format("{0},{1}", item.Percentage, item.NumberOfDataPoints));
                //    });
                //    streamWriter.Close();
                //}

                Console.BufferWidth = Int16.MaxValue - 1;
                Console.BufferHeight = Int16.MaxValue - 1;

                int currentTopDashCursorLeftPosition = Console.CursorLeft;
                int currentTopDashCursorTopPosition = Console.CursorTop;

                int currentMidDashCursorLeftPosition = Console.CursorLeft;
                int currentMidDashCursorTopPosition = Console.CursorTop;

                int currentBottomDashCursorLeftPosition = Console.CursorLeft;
                int currentBottomDashCursorTopPosition = Console.CursorTop;

                int currentCursorLeftPosition = Console.CursorLeft;
                int currentCursorTopPosition = Console.CursorTop;
                int boxLength = 8;
                for (int x = 0; x < targetPercentageLookup1.Length; x++)
                {
                    for (int y = 0; y < boxLength; y++)
                    {
                        Console.SetCursorPosition(currentTopDashCursorLeftPosition, currentTopDashCursorTopPosition);
                        Console.Write("-");
                        if ((y == 0) || (x == targetPercentageLookup1.Length - 1 && y == boxLength - 1))
                        {
                            Console.SetCursorPosition(currentTopDashCursorLeftPosition, currentTopDashCursorTopPosition + 1);
                            Console.Write("|");
                        }

                        currentTopDashCursorLeftPosition++;
                    }

                    Console.SetCursorPosition(currentCursorLeftPosition, currentCursorTopPosition + 1);
                    Console.Write("| ");
                    IEnumerable<long> currentData = (from dataPoint in lookupList
                                                     where dataPoint.Percentage == targetPercentageLookup1[x]
                                                     select dataPoint.Latency);
                    if (currentData != null && currentData.Count<long>() > 0)
                    {
                        Console.Write(currentData.First());
                        Console.Write("ms");
                    }
                    //else
                    //{
                    //    Console.ForegroundColor = ConsoleColor.Black;
                    //    Console.BackgroundColor = ConsoleColor.Yellow;
                    //    Console.Write(GetEstimatedLatency(mean, sd, targetPercentageLookup1[x]));
                    //    Console.Write("ms");
                    //    Console.ResetColor();
                    //}

                    for (int y = 0; y < boxLength; y++)
                    {
                        Console.SetCursorPosition(currentMidDashCursorLeftPosition, currentMidDashCursorTopPosition + 2);
                        Console.Write("-");

                        currentMidDashCursorLeftPosition++;
                    }

                    Console.SetCursorPosition(currentCursorLeftPosition, currentCursorTopPosition + 3);
                    Console.Write(string.Format("| {0}%", targetPercentageLookup1[x].ToString()));

                    for (int y = 0; y < boxLength; y++)
                    {
                        Console.SetCursorPosition(currentBottomDashCursorLeftPosition, currentBottomDashCursorTopPosition + 4);
                        Console.Write("-");

                        if ((y == 0) || (x == targetPercentageLookup1.Length - 1 && y == boxLength - 1))
                        {
                            Console.SetCursorPosition(currentBottomDashCursorLeftPosition, currentBottomDashCursorTopPosition + 3);
                            Console.Write("|");
                        }

                        currentBottomDashCursorLeftPosition++;
                    }

                    currentCursorLeftPosition = currentCursorLeftPosition + boxLength;
                }

                Console.SetCursorPosition(0, Console.CursorTop + 2);

                currentTopDashCursorLeftPosition = Console.CursorLeft;
                currentTopDashCursorTopPosition = Console.CursorTop;

                currentMidDashCursorLeftPosition = Console.CursorLeft;
                currentMidDashCursorTopPosition = Console.CursorTop;

                currentBottomDashCursorLeftPosition = Console.CursorLeft;
                currentBottomDashCursorTopPosition = Console.CursorTop;

                currentCursorLeftPosition = Console.CursorLeft;
                currentCursorTopPosition = Console.CursorTop;

                for (int x = 0; x < targetPercentageLookup2.Length; x++)
                {
                    for (int y = 0; y < boxLength; y++)
                    {
                        Console.SetCursorPosition(currentTopDashCursorLeftPosition, currentTopDashCursorTopPosition);
                        Console.Write("-");
                        if ((y == 0) || (x == targetPercentageLookup2.Length - 1 && y == boxLength - 1))
                        {
                            Console.SetCursorPosition(currentTopDashCursorLeftPosition, currentTopDashCursorTopPosition + 1);
                            Console.Write("|");
                        }

                        currentTopDashCursorLeftPosition++;
                    }

                    Console.SetCursorPosition(currentCursorLeftPosition, currentCursorTopPosition + 1);
                    IEnumerable<long> currentData = (from dataPoint in lookupList
                                                     where dataPoint.Percentage == targetPercentageLookup2[x]
                                                     select dataPoint.Latency);
                    Console.Write("| ");
                    if (currentData != null && currentData.Count<long>() > 0)
                    {
                        Console.Write(currentData.First());
                        Console.Write("ms");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Yellow;
                        Console.Write(GetEstimatedLatency(mean, sd, targetPercentageLookup2[x]));
                        Console.Write("ms");
                        Console.ResetColor();
                    }

                    for (int y = 0; y < boxLength; y++)
                    {
                        Console.SetCursorPosition(currentMidDashCursorLeftPosition, currentMidDashCursorTopPosition + 2);
                        Console.Write("-");

                        currentMidDashCursorLeftPosition++;
                    }

                    Console.SetCursorPosition(currentCursorLeftPosition, currentCursorTopPosition + 3);
                    Console.Write("| ");
                    //Console.ForegroundColor = ConsoleColor.Yellow;
                    //Console.BackgroundColor = ConsoleColor.Blue;
                    Console.Write(string.Format("{0}%", targetPercentageLookup2[x].ToString()));
                    Console.ResetColor();

                    for (int y = 0; y < boxLength; y++)
                    {
                        Console.SetCursorPosition(currentBottomDashCursorLeftPosition, currentBottomDashCursorTopPosition + 4);
                        Console.Write("-");

                        if ((y == 0) || (x == targetPercentageLookup1.Length - 1 && y == boxLength - 1))
                        {
                            Console.SetCursorPosition(currentBottomDashCursorLeftPosition, currentBottomDashCursorTopPosition + 3);
                            Console.Write("|");
                        }

                        currentBottomDashCursorLeftPosition++;
                    }

                    currentCursorLeftPosition = currentCursorLeftPosition + boxLength;
                }

            }
            return 0;
        }

        private static void StandardNormalDistributionPoints(List<long> latencyCollection, bool cumulative)
        {
            latencyCollection.Sort();

            int size = latencyCollection.Count;
            int middle = size / 2;

            double median = (size % 2 != 0) ? latencyCollection[middle] : (latencyCollection[middle] + latencyCollection[middle - 1]) / 2;

            double mean = latencyCollection.Average();
            double variation = latencyCollection.Average(data => Math.Pow(data - mean, 2));
            double standardDeviation = Math.Sqrt(variation);

            if (standardDeviation != 0)
            {
                double dataPoint=0;
                BellCurveData data1;
                Console.WriteLine("GetNormalDistributionDatapointBasedOnProbability");
                List<BellCurveData> bellCurveData = new List<BellCurveData>();

                if (cumulative)
                {
                    //For Cumulative Curve Shape
                    for (double pValue = 0.001; pValue <= 0.999; pValue += 0.005)
                    {
                        dataPoint = GetNormalDistributionDatapointBasedOnProbability(pValue, mean, standardDeviation);
                        data1 = new BellCurveData();
                        data1.Probability = pValue;
                        data1.Datapoint = dataPoint;
                        bellCurveData.Add(data1);
                        Console.WriteLine("{0},{1}", dataPoint, pValue);
                    }
                }
                else
                {
                    //For Bell Curve Shape
                    //for (double pValue = 0.001; pValue <= 0.5; pValue += 0.005)
                    //{
                    //    dataPoint = GetNormalDistributionDatapointBasedOnProbability(pValue, mean, standardDeviation);
                    //    data1 = new BellCurveData();
                    //    data1.Probability = pValue;
                    //    data1.Datapoint = dataPoint;
                    //    bellCurveData.Add(data1);
                    //    Console.WriteLine("{0},{1}", dataPoint, pValue);

                    //    dataPoint = GetNormalDistributionDatapointBasedOnProbability(1 - pValue, mean, standardDeviation);
                    //    data1 = new BellCurveData();
                    //    data1.Probability = pValue;
                    //    data1.Datapoint = dataPoint;
                    //    bellCurveData.Add(data1);
                    //    Console.WriteLine("{0},{1}", dataPoint, pValue);
                    //}
                    for (double pValue = 0.001; pValue <= 0.999; pValue += 0.005)
                    {
                        data1 = new BellCurveData();
                        dataPoint = GetNormalDistributionDatapointBasedOnProbability(pValue, mean, standardDeviation);
                        data1.Probability = (pValue <= 0.5) ? pValue : 1 - pValue;
                        data1.Datapoint = dataPoint;
                        bellCurveData.Add(data1);
                        Console.WriteLine("{0},{1}", dataPoint, pValue);
                    }
                }

                double dataProbability = 0.0;
                BellCurveData data2;
                Console.WriteLine("GetNormalDistributionDatapointBasedOnDatapoint");
                List<BellCurveData> bellCurveData2 = new List<BellCurveData>();
                foreach (long sampleData in latencyCollection)
                {
                    dataProbability = GetNormalDistributionProbabilityBasedOnDatapoint(sampleData, mean, standardDeviation);
                    data2 = new BellCurveData();
                    if (cumulative)
                    {
                        data2.Probability = dataProbability;
                    }
                    else
                    {
                        data2.Probability = (sampleData > mean) ? 1 - dataProbability : dataProbability;
                    }
                    data2.Datapoint = sampleData;
                    bellCurveData2.Add(data2);
                    Console.WriteLine("{0},{1}", sampleData, dataProbability);

                    //dataProbability = GetNormalDistributionProbabilityBasedOnDatapoint(sampleData, mean, standardDeviation);
                    //data2 = new BellCurveData();
                    //data2.Probability = 1-dataProbability;
                    //data2.Datapoint = sampleData;
                    //bellCurveData2.Add(data2);
                    //Console.WriteLine("{0},{1}", sampleData, 1-dataProbability);

                }

                //chart code start
                Chart c = new Chart();
                c.AntiAliasing = AntiAliasingStyles.All;
                c.TextAntiAliasingQuality = TextAntiAliasingQuality.High;
                c.Width = 640; //SET HEIGHT
                c.Height = 480; //SET WIDTH

                ChartArea ca = new ChartArea();
                ca.BackColor = Color.FromArgb(248, 248, 248);
                ca.BackSecondaryColor = Color.FromArgb(255, 255, 255);
                ca.BackGradientStyle = GradientStyle.TopBottom;

                ca.AxisY.IsMarksNextToAxis = true;
                ca.AxisY.Title = "Probability";
                ca.AxisY.LineColor = Color.FromArgb(157, 157, 157);
                ca.AxisY.MajorTickMark.Enabled = true;
                ca.AxisY.MinorTickMark.Enabled = true;
                ca.AxisY.MajorTickMark.LineColor = Color.FromArgb(157, 157, 157);
                ca.AxisY.MinorTickMark.LineColor = Color.FromArgb(200, 200, 200);
                ca.AxisY.LabelStyle.ForeColor = Color.FromArgb(89, 89, 89);
                ca.AxisY.LabelStyle.Format = "{0:0.0}";
                ca.AxisY.LabelStyle.IsEndLabelVisible = false;
                ca.AxisY.LabelStyle.Font = new Font("Calibri", 4, FontStyle.Regular);
                ca.AxisY.MajorGrid.LineColor = Color.FromArgb(234, 234, 234);

                ca.AxisX.Title = string.Format("Performance Latency ( Mean = {0} ; SD = {1} )", mean, Math.Round(standardDeviation,2));
                ca.AxisX.IsMarksNextToAxis = true;
                ca.AxisX.LabelStyle.Enabled = true;
                ca.AxisX.LineColor = Color.FromArgb(157, 157, 157);
                ca.AxisX.MajorGrid.LineWidth = 0;
                ca.AxisX.MajorTickMark.Enabled = true;
                ca.AxisX.MinorTickMark.Enabled = true;
                ca.AxisX.LabelStyle.Format = "{0}";
                ca.AxisX.MajorTickMark.LineColor = Color.FromArgb(157, 157, 157);
                ca.AxisX.MinorTickMark.LineColor = Color.FromArgb(200, 200, 200);

                c.ChartAreas.Add(ca);

                Series cumulativeExpectedSeries = new Series();
                cumulativeExpectedSeries.ChartType = SeriesChartType.Line;
                cumulativeExpectedSeries.Font = new Font("Lucida Sans Unicode", 6f);
                cumulativeExpectedSeries.Color = Color.FromArgb(215, 47, 6);
                cumulativeExpectedSeries.BorderColor = Color.FromArgb(159, 27, 13);
                cumulativeExpectedSeries.BackSecondaryColor = Color.FromArgb(173, 32, 11);
                cumulativeExpectedSeries.BackGradientStyle = GradientStyle.LeftRight;

                Series cumulativeActualSeries = new Series();
                cumulativeActualSeries.ChartType = SeriesChartType.Point;
                cumulativeActualSeries.Font = new Font("Lucida Sans Unicode", 6f);
                cumulativeActualSeries.Color = Color.FromArgb(107, 83, 204);
                cumulativeActualSeries.BorderColor = Color.FromArgb(63, 43, 142);
                cumulativeActualSeries.BackSecondaryColor = Color.FromArgb(173, 32, 11);
                cumulativeActualSeries.BackGradientStyle = GradientStyle.LeftRight;

                foreach (BellCurveData data in bellCurveData)
                {
                    DataPoint chartPoint = new DataPoint();
                    chartPoint.XValue = data.Datapoint;
                    chartPoint.YValues = new double[] {data.Probability};
                    //chartPoint.XValue = data.Datapoint;
                    //chartPoint.YValues = (from curveData in bellCurveData select curveData.Probability).ToArray<double>();
                    //chartPoint.XValue = data.Probability;
                    //chartPoint.YValues = (from curveData in bellCurveData select curveData.Datapoint).ToArray<double>();
                    cumulativeExpectedSeries.Points.Add(chartPoint);
                }

                c.Series.Add(cumulativeExpectedSeries);

                foreach (BellCurveData data in bellCurveData2)
                {
                    DataPoint chartPoint = new DataPoint();
                    chartPoint.XValue = data.Datapoint;
                    chartPoint.YValues = new double[] { data.Probability };
                    cumulativeActualSeries.Points.Add(chartPoint);
                }

                c.Series.Add(cumulativeActualSeries);

                double minimumProbability = 0.0;

                Series meanIndicator = new Series();
                meanIndicator.ChartType = SeriesChartType.Line;
                meanIndicator.Font = new Font("Lucida Sans Unicode", 6f);
                meanIndicator.Color = Color.FromArgb(157, 210, 151);
                meanIndicator.BorderColor = Color.FromArgb(117, 191, 108);
                meanIndicator.BackSecondaryColor = Color.FromArgb(173, 32, 11);
                meanIndicator.BackGradientStyle = GradientStyle.LeftRight;

                minimumProbability = bellCurveData.Min(pair => pair.Probability);
                foreach (BellCurveData data in bellCurveData)
                {
                    DataPoint chartPoint = new DataPoint();
                    chartPoint.XValue = mean;
                    chartPoint.YValues = new double[] { data.Probability };
                    if (data.Probability == minimumProbability)
                    {
                        chartPoint.Label = string.Format("Mean = {0}", mean); ;
                    }
                    meanIndicator.Points.Add(chartPoint);
                }
                c.Series.Add(meanIndicator);

                if (!cumulative)
                {
                    Series OneSDRightIndicator = new Series();
                    OneSDRightIndicator.ChartType = SeriesChartType.Line;
                    OneSDRightIndicator.Font = new Font("Lucida Sans Unicode", 6f);
                    OneSDRightIndicator.Color = Color.FromArgb(157, 210, 151);
                    OneSDRightIndicator.BorderColor = Color.FromArgb(117, 191, 108);
                    OneSDRightIndicator.BackSecondaryColor = Color.FromArgb(173, 32, 11);
                    OneSDRightIndicator.BackGradientStyle = GradientStyle.LeftRight;

                    minimumProbability = bellCurveData.Min(pair => pair.Probability);
                    foreach (BellCurveData data in bellCurveData)
                    {
                        DataPoint chartPoint = new DataPoint();
                        chartPoint.XValue = mean + standardDeviation;
                        chartPoint.YValues = new double[] { data.Probability };
                        if (data.Probability == minimumProbability)
                        {
                            chartPoint.Label = "+1SD";
                        }
                        OneSDRightIndicator.Points.Add(chartPoint);
                    }
                    c.Series.Add(OneSDRightIndicator);

                    Series OneSDLeftIndicator = new Series();
                    OneSDLeftIndicator.ChartType = SeriesChartType.Line;
                    OneSDLeftIndicator.Font = new Font("Lucida Sans Unicode", 6f);
                    OneSDLeftIndicator.Color = Color.FromArgb(157, 210, 151);
                    OneSDLeftIndicator.BorderColor = Color.FromArgb(117, 191, 108);
                    OneSDLeftIndicator.BackSecondaryColor = Color.FromArgb(173, 32, 11);
                    OneSDLeftIndicator.BackGradientStyle = GradientStyle.LeftRight;

                    minimumProbability = bellCurveData.Min(pair => pair.Probability);
                    foreach (BellCurveData data in bellCurveData)
                    {
                        DataPoint chartPoint = new DataPoint();
                        chartPoint.XValue = mean - standardDeviation;
                        chartPoint.YValues = new double[] { data.Probability };
                        if (data.Probability == minimumProbability)
                        {
                            chartPoint.Label = "-1SD";
                        }
                        OneSDLeftIndicator.Points.Add(chartPoint);
                    }
                    c.Series.Add(OneSDLeftIndicator);

                    Series TwoSDRightIndicator = new Series();
                    TwoSDRightIndicator.ChartType = SeriesChartType.Line;
                    TwoSDRightIndicator.Font = new Font("Lucida Sans Unicode", 6f);
                    TwoSDRightIndicator.Color = Color.FromArgb(157, 210, 151);
                    TwoSDRightIndicator.BorderColor = Color.FromArgb(117, 191, 108);
                    TwoSDRightIndicator.BackSecondaryColor = Color.FromArgb(173, 32, 11);
                    TwoSDRightIndicator.BackGradientStyle = GradientStyle.LeftRight;

                    minimumProbability = bellCurveData.Min(pair => pair.Probability);
                    foreach (BellCurveData data in bellCurveData)
                    {
                        DataPoint chartPoint = new DataPoint();
                        chartPoint.XValue = mean + (2*standardDeviation);
                        chartPoint.YValues = new double[] { data.Probability };
                        if (data.Probability == minimumProbability)
                        {
                            chartPoint.Label = "+2SD";
                        }
                        TwoSDRightIndicator.Points.Add(chartPoint);
                    }
                    c.Series.Add(TwoSDRightIndicator);

                    Series TwoSDLeftIndicator = new Series();
                    TwoSDLeftIndicator.ChartType = SeriesChartType.Line;
                    TwoSDLeftIndicator.Font = new Font("Lucida Sans Unicode", 6f);
                    TwoSDLeftIndicator.Color = Color.FromArgb(157, 210, 151);
                    TwoSDLeftIndicator.BorderColor = Color.FromArgb(117, 191, 108);
                    TwoSDLeftIndicator.BackSecondaryColor = Color.FromArgb(173, 32, 11);
                    TwoSDLeftIndicator.BackGradientStyle = GradientStyle.LeftRight;

                    minimumProbability = bellCurveData.Min(pair => pair.Probability);
                    foreach (BellCurveData data in bellCurveData)
                    {
                        DataPoint chartPoint = new DataPoint();
                        chartPoint.XValue = mean - (2 * standardDeviation);
                        chartPoint.YValues = new double[] { data.Probability };
                        if (data.Probability == minimumProbability)
                        {
                            chartPoint.Label = "-2SD";
                        }
                        TwoSDLeftIndicator.Points.Add(chartPoint);
                    }
                    c.Series.Add(TwoSDLeftIndicator);
                }
                //DataPoint meanPoint = new DataPoint();
                //meanPoint.XValue = 0;
                //meanPoint.YValues = new double[] { mean,0.5 };
                //meanIndicator.Points.Add(meanPoint);
                
                //meanPoint = new DataPoint();
                //meanPoint.XValue = mean;
                //meanPoint.YValues = new double[] { mean, 0.5 };
                //meanIndicator.Points.Add(meanPoint);

                //c.Series.Add(meanIndicator);
                if (cumulative)
                {
                    c.SaveImage(@"bellCurveDataCDF.png", ChartImageFormat.Png);
                }
                else
                {
                    c.SaveImage(@"bellCurveDataPDF.png", ChartImageFormat.Png);
                }
                //chart code ended

                //using (StreamWriter streamWriter = new StreamWriter(@"C:\Pandu\GitHub\pubnub-api\csharp\3.4\bellCurveData.txt", false))
                //{
                //    streamWriter.WriteLine("Mean={0}; SD={1}", mean, standardDeviation);
                //    bellCurveData.ForEach(item =>
                //    {
                //        streamWriter.WriteLine(string.Format("{0},{1}", item.Datapoint, item.Probability));
                //    });
                //    bellCurveData2.ForEach(item =>
                //    {
                //        streamWriter.WriteLine(string.Format(",,{0},{1}", item.Datapoint, item.Probability));
                //    });
                //    streamWriter.Close();
                //}
            }

            //if (standardDeviation != 0)
            //{
            //    Console.WriteLine("GetNormalDistributionProbabilityBasedOnDatapoint");
            //    for (int x = 0; x < latencyCollection.Count; x++)
            //    {
            //        double pValue = GetNormalDistributionProbabilityBasedOnDatapoint(latencyCollection[x], mean, standardDeviation);
            //        Console.WriteLine("{0},{1}", latencyCollection[x], pValue);
            //    }
            //}

        }

        //private static double NormalDensity(double dataPoint, double mean, double standardDeviation)
        //{
        //    double zScore = (dataPoint - mean)/standardDeviation;
        //    return Math.Exp(-(Math.Pow(zScore, 2)/2))/Math.Sqrt(2*Math.PI)/standardDeviation;
        //}

        private static double GetNormalDistributionProbabilityBasedOnDatapoint(double dataPoint, double mean, double standardDeviation)
        {
            double zScore = (dataPoint - mean)/standardDeviation;
            Chart chart = new Chart();
            double probability = chart.DataManipulator.Statistics.NormalDistribution(zScore);
            //double probability = formula.NormalDistribution(zScore); //Math.Exp(-(Math.Pow(zScore, 2) / 2)) / (standardDeviation * Math.Sqrt(2 * Math.PI));
            //return Math.Round(probability,4);
            return probability;
        }

        private static double GetNormalDistributionDatapointBasedOnProbability(double pValue, double mean, double standardDeviation)
        {
            Chart chart = new Chart();
            double zScore = chart.DataManipulator.Statistics.InverseNormalDistribution(pValue);
            //double dataPoint = Math.Sqrt(-Math.Log(pValue*standardDeviation*Math.Sqrt(2*Math.PI)) * 2 * Math.Pow(standardDeviation,2))+standardDeviation;
            double dataPoint = (zScore * standardDeviation) + mean;
            return dataPoint;
        }
        

        //private static double GetBellCurvePoint(double percentage, double mean)
        //{

        //}

        private static List<BellCurveStandardDeviationScore> CalculateBellCurveStandardDeviationScores(double mean, double standardDeviation, List<long> latencyCollection)
        {
            List<CumulativePercentage> cumulativePercentList = new List<CumulativePercentage>();

            for (int x = 0; x < latencyCollection.Count; x++)
            {
                double zScore = (latencyCollection[x] - mean) / standardDeviation;
                CumulativePercentage cumulativePercent = new CumulativePercentage();
                cumulativePercent.Latency = latencyCollection[x];
                cumulativePercent.Percentage = NormalDistributionTable.GetPercentageForZScore(zScore);

                cumulativePercentList.Add(cumulativePercent);
            }

            List<BellCurveStandardDeviationScore> bellCurveScoreList = new List<BellCurveStandardDeviationScore>();

            foreach (CumulativePercentage item in cumulativePercentList)
            {
                //bellCurveScoreList.Contains(
                //int exitingDatapointCount = bellCurveScoreList.Select(datapoint => datapoint.Percentage == item.Percentage).Count();
                int exitingDatapointCount = bellCurveScoreList.Where(datapoint => datapoint.Percentage == item.Percentage).Count();
                if (exitingDatapointCount == 0)
                {
                    BellCurveStandardDeviationScore datapoint = new BellCurveStandardDeviationScore();
                    datapoint.Percentage = item.Percentage;
                    datapoint.NumberOfDataPoints = 1;
                    bellCurveScoreList.Add(datapoint);
                }
                else
                {
                    //BellCurveStandardDeviationScore dataPoint = bellCurveScoreList.Find(datapoint => datapoint.Percentage == item.Percentage);
                    BellCurveStandardDeviationScore dataPoint = bellCurveScoreList.Where(datapoint => datapoint.Percentage == item.Percentage).First();
                    dataPoint.NumberOfDataPoints = dataPoint.NumberOfDataPoints + 1;
                }
            }
            return bellCurveScoreList;
        }

        private static List<CumulativePercentage> CalculateBellCurveCumulativePercentile(double mean, double standardDeviation, List<long> latencyCollection)
        {
            double[] targetPercentage = { 1, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 99 };
            List<double> targetList = targetPercentage.ToList<double>();

            List<CumulativePercentage> cumulativePercentList = new List<CumulativePercentage>();

            for (int x = 0; x < latencyCollection.Count; x++)
            {
                double zScore = (latencyCollection[x] - mean) / standardDeviation;
                CumulativePercentage cumulativePercent = new CumulativePercentage();
                cumulativePercent.Latency = latencyCollection[x];
                cumulativePercent.Percentage = NormalDistributionTable.GetPercentageForZScore(zScore);
                if (targetList.Contains<double>(cumulativePercent.Percentage))
                {
                    targetList.Remove(cumulativePercent.Percentage);
                }
                //Console.WriteLine("{0} ms = {1} %", cumulativePercent.Latency, cumulativePercent.Percentage);
                cumulativePercentList.Add(cumulativePercent);
                //Console.WriteLine(string.Format("{0} ms = {1}%", latencyCollection[x], NormalDistributionTable.GetPercentageForZScore(zScore)));
                //Console.WriteLine(string.Format("{0}%|", NormalDistributionTable.GetPercentageForZScore(zScore).ToString().PadRight(8)));
            }

            targetList.ForEach(item =>
            {
                CumulativePercentage cumulativePercent = new CumulativePercentage();
                cumulativePercent.Percentage = item;
                cumulativePercent.Latency = GetEstimatedLatency(mean, standardDeviation, item);
                if (cumulativePercent.Latency < 0) cumulativePercent.Latency = 0;
                cumulativePercentList.Add(cumulativePercent);
            });

            cumulativePercentList = cumulativePercentList.OrderBy(item => item.Percentage).ToList<CumulativePercentage>();
            return cumulativePercentList;
        }

        private static List<BellCurveDensity> CalculateBellCurveDensity(double mean, double standardDeviation, List<long> latencyCollection)
        {
            List<BellCurveDensity> densityList = new List<BellCurveDensity>();

            for (int x = 0; x < latencyCollection.Count; x++)
            {
                double zScore = (latencyCollection[x] - mean) / standardDeviation;
                BellCurveDensity curveDensity = new BellCurveDensity();
                curveDensity.Latency = latencyCollection[x];
                curveDensity.Density = zScore;
                //Console.WriteLine("{0} ms = {1} %", cumulativePercent.Latency, cumulativePercent.Percentage);
                densityList.Add(curveDensity);
                //Console.WriteLine(string.Format("{0} ms = {1}%", latencyCollection[x], NormalDistributionTable.GetPercentageForZScore(zScore)));
                //Console.WriteLine(string.Format("{0}%|", NormalDistributionTable.GetPercentageForZScore(zScore).ToString().PadRight(8)));
            }

            return densityList;
        }

        private static long GetLatencyPercentile(double zScore, List<long> latencyCollection)
        {
            int size = latencyCollection.Count;
            int middle = size / 2;

            if (zScore < 0)
            {
                return latencyCollection[(int)(middle * Math.Abs(zScore))];
            }
            else if (zScore > 0)
            {
                if (middle + (int)(size * zScore) < latencyCollection.Count)
                {
                    return latencyCollection[middle + (int)(size * zScore)];
                }
                else
                {
                    return latencyCollection[1];
                }
            }
            else
            {
                return latencyCollection[middle];
            }

        }

        private static long GetEstimatedLatency(double mean, double standardDeviation, double percentage)
        {
            double zScore = NormalDistributionTable.GetZScoreForPercentage(percentage);
            double estimatedLatency = (zScore * standardDeviation) + mean;
            return (Int64)estimatedLatency;
        }

        private static void DrawDemoCircle()
        {
        }
    }

    public class BellCurveData
    {
        public double Probability;
        public double Datapoint;
    }

    public class CumulativePercentage
    {
        public long Latency
        {
            get;
            set;
        }

        public double Percentage
        {
            get;
            set;
        }

        //public CumulativePercentage(double percentage, long latency)
        //{
        //    this.Latency = latency;
        //    this.Percentage = percentage;
        //}
    }

    public class BellCurveStandardDeviationScore
    {
        public double Percentage
        {
            get;
            set;
        }

        public double NumberOfDataPoints
        {
            get;
            set;
        }
    }

    public class BellCurveDensity
    {
        public long Latency
        {
            get;
            set;
        }

        public double Density
        {
            get;
            set;
        }

        //public CumulativePercentage(double percentage, long latency)
        //{
        //    this.Latency = latency;
        //    this.Percentage = percentage;
        //}
    }

    public class ZScoreTable
    {
        public double ZScore
        {
            get;
            set;
        }

        public double Percentage
        {
            get;
            set;
        }

        public ZScoreTable(double percentage, double zScore)
        {
            this.ZScore = zScore;
            this.Percentage = percentage;
        }
    }

    public class NormalDistributionTable
    {
        static List<ZScoreTable> scoreTable = null;

        private static List<ZScoreTable> BuildTable()
        {
            scoreTable = new List<ZScoreTable>();

            ZScoreTable score = new ZScoreTable(1, -2.326); scoreTable.Add(score);
            score = new ZScoreTable(2, -2.054); scoreTable.Add(score);
            score = new ZScoreTable(3, -1.881); scoreTable.Add(score);
            score = new ZScoreTable(4, -1.751); scoreTable.Add(score);
            score = new ZScoreTable(5, -1.645); scoreTable.Add(score);
            score = new ZScoreTable(6, -1.555); scoreTable.Add(score);
            score = new ZScoreTable(7, -1.476); scoreTable.Add(score);
            score = new ZScoreTable(8, -1.405); scoreTable.Add(score);
            score = new ZScoreTable(9, -1.341); scoreTable.Add(score);
            score = new ZScoreTable(10, -1.282); scoreTable.Add(score);
            score = new ZScoreTable(11, -1.227); scoreTable.Add(score);
            score = new ZScoreTable(12, -1.175); scoreTable.Add(score);
            score = new ZScoreTable(13, -1.126); scoreTable.Add(score);
            score = new ZScoreTable(14, -1.08); scoreTable.Add(score);
            score = new ZScoreTable(15, -1.036); scoreTable.Add(score);
            score = new ZScoreTable(16, -0.994); scoreTable.Add(score);
            score = new ZScoreTable(17, -0.954); scoreTable.Add(score);
            score = new ZScoreTable(18, -0.915); scoreTable.Add(score);
            score = new ZScoreTable(19, -0.878); scoreTable.Add(score);
            score = new ZScoreTable(20, -0.842); scoreTable.Add(score);
            score = new ZScoreTable(21, -0.806); scoreTable.Add(score);
            score = new ZScoreTable(22, -0.772); scoreTable.Add(score);
            score = new ZScoreTable(23, -0.739); scoreTable.Add(score);
            score = new ZScoreTable(24, -0.706); scoreTable.Add(score);
            score = new ZScoreTable(25, -0.674); scoreTable.Add(score);
            score = new ZScoreTable(26, -0.643); scoreTable.Add(score);
            score = new ZScoreTable(27, -0.613); scoreTable.Add(score);
            score = new ZScoreTable(28, -0.583); scoreTable.Add(score);
            score = new ZScoreTable(29, -0.553); scoreTable.Add(score);
            score = new ZScoreTable(30, -0.524); scoreTable.Add(score);
            score = new ZScoreTable(31, -0.496); scoreTable.Add(score);
            score = new ZScoreTable(32, -0.468); scoreTable.Add(score);
            score = new ZScoreTable(33, -0.44); scoreTable.Add(score);
            score = new ZScoreTable(34, -0.412); scoreTable.Add(score);
            score = new ZScoreTable(35, -0.385); scoreTable.Add(score);
            score = new ZScoreTable(36, -0.358); scoreTable.Add(score);
            score = new ZScoreTable(37, -0.332); scoreTable.Add(score);
            score = new ZScoreTable(38, -0.305); scoreTable.Add(score);
            score = new ZScoreTable(39, -0.279); scoreTable.Add(score);
            score = new ZScoreTable(40, -0.253); scoreTable.Add(score);
            score = new ZScoreTable(41, -0.228); scoreTable.Add(score);
            score = new ZScoreTable(42, -0.202); scoreTable.Add(score);
            score = new ZScoreTable(43, -0.176); scoreTable.Add(score);
            score = new ZScoreTable(44, -0.151); scoreTable.Add(score);
            score = new ZScoreTable(45, -0.126); scoreTable.Add(score);
            score = new ZScoreTable(46, -0.1); scoreTable.Add(score);
            score = new ZScoreTable(47, -0.075); scoreTable.Add(score);
            score = new ZScoreTable(48, -0.05); scoreTable.Add(score);
            score = new ZScoreTable(49, -0.025); scoreTable.Add(score);
            score = new ZScoreTable(50, 0); scoreTable.Add(score);
            score = new ZScoreTable(51, 0.025); scoreTable.Add(score);
            score = new ZScoreTable(52, 0.05); scoreTable.Add(score);
            score = new ZScoreTable(53, 0.075); scoreTable.Add(score);
            score = new ZScoreTable(54, 0.1); scoreTable.Add(score);
            score = new ZScoreTable(55, 0.126); scoreTable.Add(score);
            score = new ZScoreTable(56, 0.151); scoreTable.Add(score);
            score = new ZScoreTable(57, 0.176); scoreTable.Add(score);
            score = new ZScoreTable(58, 0.202); scoreTable.Add(score);
            score = new ZScoreTable(59, 0.228); scoreTable.Add(score);
            score = new ZScoreTable(60, 0.253); scoreTable.Add(score);
            score = new ZScoreTable(61, 0.279); scoreTable.Add(score);
            score = new ZScoreTable(62, 0.305); scoreTable.Add(score);
            score = new ZScoreTable(63, 0.332); scoreTable.Add(score);
            score = new ZScoreTable(64, 0.358); scoreTable.Add(score);
            score = new ZScoreTable(65, 0.385); scoreTable.Add(score);
            score = new ZScoreTable(66, 0.412); scoreTable.Add(score);
            score = new ZScoreTable(67, 0.44); scoreTable.Add(score);
            score = new ZScoreTable(68, 0.468); scoreTable.Add(score);
            score = new ZScoreTable(69, 0.496); scoreTable.Add(score);
            score = new ZScoreTable(70, 0.524); scoreTable.Add(score);
            score = new ZScoreTable(71, 0.553); scoreTable.Add(score);
            score = new ZScoreTable(72, 0.583); scoreTable.Add(score);
            score = new ZScoreTable(73, 0.613); scoreTable.Add(score);
            score = new ZScoreTable(74, 0.643); scoreTable.Add(score);
            score = new ZScoreTable(75, 0.674); scoreTable.Add(score);
            score = new ZScoreTable(76, 0.706); scoreTable.Add(score);
            score = new ZScoreTable(77, 0.739); scoreTable.Add(score);
            score = new ZScoreTable(78, 0.772); scoreTable.Add(score);
            score = new ZScoreTable(79, 0.806); scoreTable.Add(score);
            score = new ZScoreTable(80, 0.842); scoreTable.Add(score);
            score = new ZScoreTable(81, 0.878); scoreTable.Add(score);
            score = new ZScoreTable(82, 0.915); scoreTable.Add(score);
            score = new ZScoreTable(83, 0.954); scoreTable.Add(score);
            score = new ZScoreTable(84, 0.994); scoreTable.Add(score);
            score = new ZScoreTable(85, 1.036); scoreTable.Add(score);
            score = new ZScoreTable(86, 1.08); scoreTable.Add(score);
            score = new ZScoreTable(87, 1.126); scoreTable.Add(score);
            score = new ZScoreTable(88, 1.175); scoreTable.Add(score);
            score = new ZScoreTable(89, 1.227); scoreTable.Add(score);
            score = new ZScoreTable(90, 1.282); scoreTable.Add(score);
            score = new ZScoreTable(91, 1.341); scoreTable.Add(score);
            score = new ZScoreTable(92, 1.405); scoreTable.Add(score);
            score = new ZScoreTable(93, 1.476); scoreTable.Add(score);
            score = new ZScoreTable(94, 1.555); scoreTable.Add(score);
            score = new ZScoreTable(95, 1.645); scoreTable.Add(score);
            score = new ZScoreTable(96, 1.751); scoreTable.Add(score);
            score = new ZScoreTable(97, 1.881); scoreTable.Add(score);
            score = new ZScoreTable(98, 2.054); scoreTable.Add(score);
            score = new ZScoreTable(99, 2.326); scoreTable.Add(score);

            return scoreTable;
        }

        public static double GetPercentageForZScore(double zScore)
        {
            double percent = 0;
            if (scoreTable == null) BuildTable();
            //ZScoreTable result =  scoreTable.Find(score => score.ZScore <= zScore);
            IEnumerable<double> result = (from score in scoreTable
                                          where score.ZScore <= zScore
                                          select score.Percentage);

            if (result != null && result.Count() > 0)
            {
                percent = result.Last<double>();
            }

            return percent;
        }

        public static double GetZScoreForPercentage(double percentage)
        {
            double zScore = 0;
            if (scoreTable == null) BuildTable();

            IEnumerable<double> result = (from score in scoreTable
                                          where score.Percentage == percentage
                                          select score.ZScore);

            if (result != null && result.Count() > 0)
            {
                zScore = result.First<double>();
            }

            return zScore;
        }
    }

}

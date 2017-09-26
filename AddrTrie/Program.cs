using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS;
using VDS.Common.Tries;
//using FuzzyString;
using DuoVia.FuzzyStrings;
using CsvHelper;
using System.IO;
using SpellingCorrector;
using TestLibrary;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AddrTrie
{
    [JsonObject]
    class CodeMappings
    {
        [JsonProperty]
        public string s1 { get; set; }
        [JsonConverter(typeof(MyConverter))]
        public Dictionary<string, List<string>> s2 { get; set;  } 
    }

    public class MyConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Dictionary<string, List<string>>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var dict = value as Dictionary<string, List<string>>;
            dict.Keys.ToList().ForEach(e =>
            {
                writer.WriteStartObject();
                writer.WritePropertyName(nameof(CodeMappings.s2));
                writer.WriteStartArray();
                dict[e].ForEach(x =>
                {
                    writer.WritePropertyName("s");
                    serializer.Serialize(writer, x);
                });
                writer.WriteEndArray();
                writer.WriteEndObject();
            });
        }
    }

    public class Addr
    {
        public string BUILDING1 { get; set; }
        public string BUILDING2 { get; set; }
        public string PHASE1 { get; set; }
        public string PHASE2 { get; set; }
        public string ESTATE { get; set; }
        public string STREET_NO_FROM { get; set; }
        public string STREET_NO_TO { get; set; }
        public string STREET { get; set; }
        public string PLACE { get; set; }
        public string DISTRICT { get; set; }
        public string REGION { get; set; }
        public string SCORE { get; set; }

        public override string ToString()
        {
            return string.Format($"{(string.IsNullOrEmpty(BUILDING1) ? "NA" : BUILDING1)} | {(string.IsNullOrEmpty(ESTATE) ? "NA" : ESTATE)} | {STREET} | {DISTRICT} | {REGION}");        }
    }

    class Program
    {
        static void TraverseChildren(ITrieNode<char, string> node, Action<ITrieNode<char, string>> nodeFunc, Action<ITrieNode<char, string>> leafFunc)
        {
            if (node != null && node.HasValue)
            {
                nodeFunc(node);
            }
            if (node == null || node.IsLeaf)
            {
                leafFunc(node);
                return;
            }
            node.Children.ToList().ForEach(n =>
            {
                TraverseChildren(n, nodeFunc, leafFunc);
            });
        }
        static void TraverseTrie(StringTrie<string> trie, Action<ITrieNode<char, string>> nodeFunc, Action<ITrieNode<char, string>> leafFunc)
        {
            Enumerable.Range(Convert.ToInt32('A'), Convert.ToInt32('Z')).ToList().ForEach(e =>
            {
                var c = Convert.ToChar(e);
                var node = trie.Find(c.ToString());
                TraverseChildren(node, nodeFunc, leafFunc);
            });
        }

        static Regex regexLetterFirst = new Regex(@"(^[A-Z]+\d*[A-Z]*)", RegexOptions.Compiled);
        static Regex regexRange = new Regex(@"(\d{1,}\w*?)\s{0,}-\s{0,}(\d{1,}\w*)", RegexOptions.Compiled);
        static Regex regexSingle = new Regex(@"(\d{1,}\w*)", RegexOptions.Compiled);
        public static bool ConvertStreetNoFromStringToComponents(string stNo, out int from, out string fromAlpha, out int to, out string toAlpha)
        {
            var m = regexLetterFirst.Match(stNo);
            if (m.Success)
            {
                from = 0;
                fromAlpha = m.Groups[1].Value;
                to = 0;
                toAlpha = string.Empty;
                return true;
            }
            m = regexRange.Match(stNo);
            if (m.Success)
            {
                var grpA = m.Groups[1].Value;
                var grpB = m.Groups[2].Value;
                var b1 = ConvertCaptureGrpToStreetIntAndAlpha(grpA, out from, out fromAlpha);
                var b2 = ConvertCaptureGrpToStreetIntAndAlpha(grpB, out to, out toAlpha);
                return b1 && b2;
            }
            m = regexSingle.Match(stNo);
            if (m.Success)
            {
                to = 0;
                toAlpha = string.Empty;
                var grpA = m.Groups[1].Value;
                return ConvertCaptureGrpToStreetIntAndAlpha(grpA, out from, out fromAlpha);
            }
            from = to = 0;
            fromAlpha = toAlpha = string.Empty;
            return false;
        }

        public static bool ConvertCaptureGrpToStreetIntAndAlpha(string grp, out int no, out string alpha)
        {
            Regex regex = new Regex(@"(\d+)(\w*)");
            var m = regex.Match(grp);
            if (m.Success)
            {
                no = Convert.ToInt32(m.Groups[1].Value);
                alpha = m.Groups[2].Value;
                return true;
            }
            no = 0;
            alpha = string.Empty;
            return false;
        }

        static void Main(string[] args)
        {
            GetAppSettings settings = new GetAppSettings();
            Console.WriteLine(settings.GetSetting("Test"));

            Spelling spelling = new Spelling();
            string word = "";

            word = "PORK";
            Console.WriteLine("{0} => {1}", word, spelling.Correct(word));

            word = "FUU"; // 'correcter' is not in the dictionary file so this doesn't work
            Console.WriteLine("{0} => {1}", word, spelling.Correct(word));

            word = "LAMM";
            Console.WriteLine("{0} => {1}", word, spelling.Correct(word));

            word = "PAINT";
            Console.WriteLine("{0} => {1}", word, spelling.Correct(word));

            word = "ETAE";
            Console.WriteLine("{0} => {1}", word, spelling.Correct(word));

            // A sentence
            string sentence = "I havve speled thes woord wwrong"; // sees speed instead of spelled (see notes on norvig.com)
            string correction = "";
            foreach (string item in sentence.Split(' '))
            {
                correction += " " + spelling.Correct(item);
            }
            Console.WriteLine("Did you mean:" + correction);

            Enumerable.Range(Convert.ToInt32('A'), 26).Union(Enumerable.Range(Convert.ToInt32('0'), 10)).ToList().
                ForEach(e => Console.WriteLine(Convert.ToChar(e)));

            Console.WriteLine($"{DuoVia.FuzzyStrings.StringExtensions.FuzzyMatch("HONG", "HONG")}");
            Console.WriteLine($"{DuoVia.FuzzyStrings.StringExtensions.FuzzyMatch("HOONG", "HONG")}");
            Console.WriteLine($"{DuoVia.FuzzyStrings.StringExtensions.FuzzyMatch("HOONG", "HSOUNG")}");
            Console.WriteLine($"{DuoVia.FuzzyStrings.StringExtensions.FuzzyMatch("HOONG", "HSIN")}");
            Console.WriteLine($"{DuoVia.FuzzyStrings.StringExtensions.FuzzyMatch("Hello Wor1ld3", "Hello Warl12d3")}");



            Console.WriteLine($"{FuzzyString.ComparisonMetrics.JaccardDistance("Hello World", "Hello World")}");
            Console.WriteLine($"{FuzzyString.ComparisonMetrics.JaccardDistance("Hello World", "Hello Warld")}");
            Console.WriteLine($"{FuzzyString.ComparisonMetrics.JaccardDistance("Hello World", "Hello Warl12d3")}");
            Console.WriteLine($"{FuzzyString.ComparisonMetrics.JaccardDistance("Hello Wor1ld3", "Hello Warl12d3")}");

            Console.WriteLine($"{FuzzyString.ComparisonMetrics.HammingDistance("Hello World", "Hello World")}");
            Console.WriteLine($"{FuzzyString.ComparisonMetrics.HammingDistance("Hello World", "Hello Warld")}");
            Console.WriteLine($"{FuzzyString.ComparisonMetrics.HammingDistance("Hello World", "Hello Warl12d3")}");
            Console.WriteLine($"{FuzzyString.ComparisonMetrics.HammingDistance("Hello Wor1ld3", "Hello Warl12d3")}");

            Console.WriteLine($"{FuzzyString.ComparisonMetrics.SorensenDiceDistance("Hello World", "Hello World")}");
            Console.WriteLine($"{FuzzyString.ComparisonMetrics.SorensenDiceDistance("Hello World", "Hello Warld")}");
            Console.WriteLine($"{FuzzyString.ComparisonMetrics.SorensenDiceDistance("Hello World", "Hello Warl12d3")}");
            Console.WriteLine($"{FuzzyString.ComparisonMetrics.SorensenDiceDistance("Hello Wor1ld3", "Hello Warl12d3")}");

            Console.WriteLine("Hello World".DiceCoefficient("Hello World"));
            Console.WriteLine("Hello World".DiceCoefficient("Hello Warld"));
            Console.WriteLine("Hello World".DiceCoefficient("Heo Warl12d3"));
            Console.WriteLine("Hello World".DiceCoefficient("Hello Warl12d3"));

            StringTrie<string> Trie = new StringTrie<string>();

            Trie.Add("ABC", "ABC");
            Trie.Add("ABCDEF", "ABCDEF");
            Trie.Add("FGHIJK", "FGHIJK");
            Trie.Add("KKK", "KKK");
            Trie.Add("LLLLL", "LLLLL");
            Trie.Add("LLLLLT", "LLLLLT");
            Trie.Add("Tai Po", "Tai Po");
            Trie.Add("Tai Po ABC", "Tai Po ABC");
            Trie.Add("Tai Po ABC DEF", "Tai Po ABC DEF");
            Trie.Add("Tai Po XXX", "Tai Po XXX");
            Trie.Add("Tai Po YYY", "Tai Po YYY");

            TraverseTrie(Trie,
                n =>
                {
                    Console.WriteLine($"Node: {n.Value}");
                },
                lf =>
                {
                    Console.WriteLine($"Leaf: *****");
                });


            var node = Trie.Find("Tai Po A");
            Console.WriteLine(node.IsLeaf + " " + node.HasValue);
            Console.WriteLine("*************************");
            node.Descendants.ToList().ForEach(e =>
            {
                if (e.HasValue)
                    Console.WriteLine(e.Value);
            });
            Console.WriteLine("*************************");
            node = Trie.Find("Tai Po AB");
            Console.WriteLine(node.IsLeaf + " " + node.HasValue);
            node = Trie.Find("Tai Po ABC");
            Console.WriteLine(node.IsLeaf + " " + node.HasValue);


            List<string> l = new List<string>();
            l.Add("");
            l.Add(null);
            l.Add("Hello World");
            foreach (var ll in l)
            {
                if (string.IsNullOrEmpty(ll))
                    continue;
                Console.WriteLine(ll);
            }

            var s = "([([}) []  A [] []   Test  ]]]]]   ";
            Console.WriteLine(s.Replace('(', Convert.ToChar(32)).Replace(')', Convert.ToChar(32)).Replace('[', Convert.ToChar(32)));

            

            /*
            FileStream fs = new FileStream(@"C:\Temp\TestAddr.csv", FileMode.Open, FileAccess.Read);
            var reader = new CsvReader(new StreamReader(fs));
            reader.ReadHeader();
            int i = 0;
            while (reader.Read())
            {
                var addr = reader.GetRecord<Addr>();
                Console.WriteLine(addr);
                if (i++ >= 10)
                    break;
            }
            var p = System.IO.Path.GetTempPath();
            */

            var r = "A".CompareTo("B");

            var cm = new CodeMappings();
            
                
            HashSet<int> hs = new HashSet<int>();
            hs.Add(1);
            hs.Add(2);
            hs.Add(1);
            hs.Add(2);
            hs.Add(3);

            foreach (var x in hs)
                Console.WriteLine(x);

            Console.WriteLine("First Result Set ******************************************************************************");
            var res1 = LCSFinder.GetLCS("A B C D E F G H", "A B C D U V Y K");
            var res2 = LCSFinder.GetLCS("A B C D E F G H", "A I B T C O D L");
            Console.WriteLine(res1);
            Console.WriteLine(res2);

            Console.WriteLine("Second Result Set ******************************************************************************");
            var res3 = LCSFinder.GetLCS("AA BB CC DDD EE FFFF GG H", "AA XXXX BDB YYYYYY CC DD123DD U EE V Y K");
            var res4 = LCSFinder.GetLCS("AA BB CC DDD EE FFFF GG H", "AA I BB T CC OXXXXX DDD L EE");
            Console.WriteLine(res3);
            Console.WriteLine(res4);

            Console.WriteLine("Third Result Set ******************************************************************************");
            var res5 = LCSFinder.GetLCS("AA BB CC DD E F G", "AA BB CC BB II CC KK DD H I K");
            var res6 = LCSFinder.GetLCS("AA BB CC DD E F G", "AA BB CCCC DD II E A F B G");
            Console.WriteLine(res5);
            Console.WriteLine(res6);
            Console.WriteLine("******************************************************************************");

            Console.WriteLine("First Result Set ******************************************************************************");
            var resX = "A B C D E F G H".LongestCommonSubsequence( "A B C D U V Y K");
            var resY = "A B C D E F G H".LongestCommonSubsequence( "A I B T C O D L");
            Console.WriteLine(resX);
            Console.WriteLine(resY);

            Console.WriteLine("Second Result Set ******************************************************************************");
            var resA = "AA BB CC DDD EE FFFF GG H".LongestCommonSubsequence( "AA XXXX BDB YYYYYY CC DD123DD U EE V Y K");
            var resB = "AA BB CC DDD EE FFFF GG H".LongestCommonSubsequence( "AA I BB T CC OXXXXX DDD L EE");
            Console.WriteLine(resA);
            Console.WriteLine(resB);

            Console.WriteLine("Third Result Set ******************************************************************************");
            var resC = "AA BB CC DD E F G".LongestCommonSubsequence( "AA BB CC BB II CC KK DD H I K");
            var resD = "AA BB CC DD E F G".LongestCommonSubsequence("AA BB CCCC DD II E A F B G");
            Console.WriteLine(resC);
            Console.WriteLine(resD);
            Console.WriteLine("******************************************************************************");


            Console.WriteLine("1 Result Set ******************************************************************************");
            var r1 = "ABCDEFGH".LongestCommonSubsequence("ABCDUVYK");
            var r2 = "ABCDEFGH".LongestCommonSubsequence("AIBTCODL");
            Console.WriteLine(r1);
            Console.WriteLine(r2);

            Console.WriteLine("2 Result Set ******************************************************************************");
            var r3 = "AABBCCDDDEEFFFFGGH".LongestCommonSubsequence("AAXXXXBDBYYYYYYCCDD123DDUEEVYK");
            var r4 = "AABBCCDDDEEFFFFGGH".LongestCommonSubsequence("AAIBBTCCOXXXXXDDDLEE");
            Console.WriteLine(r3);
            Console.WriteLine(r4);

            Console.WriteLine("3 Result Set ******************************************************************************");
            var r5 = "AABBCCDDEFG".LongestCommonSubsequence("AABBCCBBIICCKKDDHIK");
            var r6 = "AABBCCDDEFG".LongestCommonSubsequence("AABBCCCCDDIIEAFBG");
            Console.WriteLine(r5);
            Console.WriteLine(r6);
            Console.WriteLine("******************************************************************************");

            Console.WriteLine("4 Result Set ******************************************************************************");
            var r7 = "SUN FUNG WAI".LongestCommonSubsequence("SAN FUNG WAI");
            var r8 = "SUN FUNG WAI".LongestCommonSubsequence("SAN WAI COURT");
            Console.WriteLine(r7);
            Console.WriteLine(r8);
            Console.WriteLine("******************************************************************************");


            Enumerable.Range(1, 20).ToList().ForEach(e =>
            {
                var rA = ToRoman(e);
                var sA = ConvertRomanToNumber(rA);
                Console.WriteLine($"{rA}->{sA}");
            });

            MainStore ms = new MainStore();
            ms.Serialize(@"C:\Temp\tt.json");





            List<StreetNoExpansion> stl = new List<StreetNoExpansion>()
            {
                new StreetNoExpansion(13, "A", 13, "A"),    //0  
                new StreetNoExpansion(13, "L", 14, "L"),    //1
                new StreetNoExpansion(13, "L", 17, "L"),    //2
                new StreetNoExpansion(13, "L", 20, "L"),    //3
                new StreetNoExpansion(13, "L", 14, "M"),    //4
                new StreetNoExpansion(12, "", 12, ""),      //5
                new StreetNoExpansion(12, "A", 12, "A"),    //6
                new StreetNoExpansion(12, "", 19, ""),      //7
                new StreetNoExpansion(12, "", 20, ""),      //8
                new StreetNoExpansion(12, "", 12, ""),      //9
                new StreetNoExpansion(12, "", 13, ""),      //10
                new StreetNoExpansion(12, "A", 12, "B"),    //11
                new StreetNoExpansion(14, "D", 21, "S"),    //12
                new StreetNoExpansion(15, "A", 17, ""),     //13
                new StreetNoExpansion(15, "A", 29, ""),     //14
                new StreetNoExpansion(15, "B", 16, ""),     //15
                new StreetNoExpansion(15, "C", 15, ""),     //16
                new StreetNoExpansion(16, "", 17, "B"),     //17
                new StreetNoExpansion(16, "", 18, "B"),     //18
                new StreetNoExpansion(16, "", 16, "B"),     //19
                new StreetNoExpansion(16, "", 20, "D"),     //20
                new StreetNoExpansion(16, "A", 17, ""),     //21
                new StreetNoExpansion(16, "B", 18, ""),     //22
                new StreetNoExpansion(16, "C", 16, ""),     //23
                new StreetNoExpansion(16, "D", 20, ""),     //24
                new StreetNoExpansion(3, "EA3", 3, "EA3"),  //25
                new StreetNoExpansion(3, "A", 0, ""),       //26
                new StreetNoExpansion(3, "", 0, ""),        //27
            };
            stl.ForEach(e => Console.WriteLine(e));
            var line = "";
            while (!string.IsNullOrEmpty(line = Console.ReadLine()))
            {
                Console.Write(">");
                if (!ConvertStreetNoFromStringToComponents(line, out int from, out string fromA, out int to, out string toA))
                {
                    Console.WriteLine("Conversion Failed");
                    continue;
                }
                int idx = 0;
                stl.ForEach(e =>
                {
                    Console.WriteLine($"{idx++}->{e.MatchStreetNo(from, fromA, to, toA)}");
                });
            }

        }


        public static string ToRoman(int number)
        {
            if ((number < 0) || (number > 3999)) return string.Empty;
            if (number < 1) return string.Empty;
            if (number >= 1000) return "M" + ToRoman(number - 1000);
            if (number >= 900) return "CM" + ToRoman(number - 900); //EDIT: i've typed 400 instead 900
            if (number >= 500) return "D" + ToRoman(number - 500);
            if (number >= 400) return "CD" + ToRoman(number - 400);
            if (number >= 100) return "C" + ToRoman(number - 100);
            if (number >= 90) return "XC" + ToRoman(number - 90);
            if (number >= 50) return "L" + ToRoman(number - 50);
            if (number >= 40) return "XL" + ToRoman(number - 40);
            if (number >= 10) return "X" + ToRoman(number - 10);
            if (number >= 9) return "IX" + ToRoman(number - 9);
            if (number >= 5) return "V" + ToRoman(number - 5);
            if (number >= 4) return "IV" + ToRoman(number - 4);
            if (number >= 1) return "I" + ToRoman(number - 1);
            throw new ArgumentOutOfRangeException("something bad happened");
        }

        private static Dictionary<char, int> _romanMap = new Dictionary<char, int>
        {
            {'I', 1}, {'V', 5}, {'X', 10}, {'L', 50}, {'C', 100}, {'D', 500}, {'M', 1000}
        };

        public static int ConvertRomanToNumber(string text)
        {
            int totalValue = 0, prevValue = 0;
            foreach (var c in text)
            {
                if (!_romanMap.ContainsKey(c))
                    return 0;
                var crtValue = _romanMap[c];
                totalValue += crtValue;
                if (prevValue != 0 && prevValue < crtValue)
                {
                    if (prevValue == 1 && (crtValue == 5 || crtValue == 10)
                        || prevValue == 10 && (crtValue == 50 || crtValue == 100)
                        || prevValue == 100 && (crtValue == 500 || crtValue == 1000))
                        totalValue -= 2 * prevValue;
                    else
                        return 0;
                }
                prevValue = crtValue;
            }
            return totalValue;
        }

    }

    public enum EntityType
    {
        UNKNOWN = 0,
        BUILDING1 = 1,
        BUILDING2 = 2,
        PHASE1 = 4,
        PHASE2 = 8,
        ESTATE = 16,
        STREET_NO = 32,
        STREET = 64,
        PLACE = 128,
        DISTRICT = 256,
        REGION = 512
    }


    public class EntityExpansion
    {
        public EntityType entityType;
    }


    
    [JsonObject]
    class Base
    {
        [JsonProperty]
        public string BaseString;
        [JsonProperty]
        public int BaseID;
    }



    [JsonObject]
    public class StreetNoExpansion : EntityExpansion
    {
        public enum StreetNoMatchingStatus
        {
            NOT_MATCH,
            EXACT_MATCH,
            MATCH_FROM,
            MATCH_TO,
            IN_RANGE
        }

        public enum EvenOddState
        {
            GOT_START_ONLY = 1,
            BOTH_START_END_EVEN = 2,
            BOTH_START_END_ODD = 4,
            START_END_MIXED_EVENODD = 8
        }

        [JsonProperty]
        public int stFrom;
        [JsonProperty]
        public int stTo;
        [JsonProperty]
        public string stFromAlpha;
        [JsonProperty]
        public string stToAlpha;
        [JsonProperty]
        public List<(int no, string alpha)> coveredRange;

        public StreetNoExpansion(int from, string fromA, int to, string toA)
        {
            entityType = EntityType.STREET_NO;
            stFrom = from;
            stTo = to;
            stFromAlpha = fromA;
            stToAlpha = toA;
            var eoState = CheckEvenOddState();

            void GenCoveredRange(int start, string startA, int end, string endA)
            {
                if (coveredRange == null)
                    coveredRange = new List<(int no, string alpha)>();

                void AddToCoveredRangeByEvenOddInterval()
                {
                    for (int i = start; i <= end; i += 2)
                    {
                        coveredRange.Add((i, string.Empty));
                    }
                }

                void AddToCoveredRangeByContinuousInterval()
                {
                    for (int i = start; i <= end; i++)
                    {
                        coveredRange.Add((i, string.Empty));
                    }
                }

                // For Letter starting street No.  Assume start and NO to.
                if (start == 0 && !string.IsNullOrEmpty(startA) && end == 0 && string.IsNullOrEmpty(endA))
                {
                    coveredRange.Add((0, startA));
                }
                else
                if (!string.IsNullOrWhiteSpace(startA) && !string.IsNullOrWhiteSpace(endA))
                {
                    Debug.Assert(start > 0 && end > 0);
                    if (startA == endA)
                    {
                        if ((eoState & EvenOddState.START_END_MIXED_EVENODD) == EvenOddState.START_END_MIXED_EVENODD)
                        {
                            for (int i = start; i <= end; i++)
                            {
                                coveredRange.Add((i, $"{startA}"));
                            }
                        }
                        else
                        {
                            for (int i = start; i <= end; i += 2)
                            {
                                coveredRange.Add((i, $"{startA}"));
                            }
                        }
                    }
                    else
                    {
                        // stA and edA are different
                        if ((eoState & EvenOddState.START_END_MIXED_EVENODD) == EvenOddState.START_END_MIXED_EVENODD)
                        {
                            for (int i = start; i <= end; i++)
                            {
                                for (char c = startA[0]; c <= endA[0]; c++)
                                {
                                    coveredRange.Add((i, c.ToString()));
                                }
                            }
                        }
                        else
                        {
                            for (int i = start; i <= end; i += 2)
                            {
                                for (char c = startA[0]; c <= endA[0]; c++)
                                {
                                    coveredRange.Add((i, c.ToString()));
                                }
                            }
                            if (!coveredRange.Contains((end, endA)))
                                coveredRange.Add((end, endA));
                        }
                    }
                }
                else
                if (!string.IsNullOrEmpty(startA) && string.IsNullOrEmpty(endA))
                {
                    Debug.Assert(start > 0);
                    if (end == 0)
                    {
                        coveredRange.Add((start, startA));
                    }
                    else
                    if (startA[0] == 'A')
                    {
                        coveredRange.Add((start, "A"));
                        coveredRange.Add((start, "B"));
                    }
                    else
                        coveredRange.Add((start, startA));

                    if (end > 0)
                    {
                        if ((eoState & EvenOddState.START_END_MIXED_EVENODD) == EvenOddState.START_END_MIXED_EVENODD)
                        {
                            AddToCoveredRangeByContinuousInterval();
                        }
                        else
                        {
                            AddToCoveredRangeByEvenOddInterval();
                            if (!coveredRange.Contains((end, string.Empty)))
                                coveredRange.Add((end, string.Empty));
                        }
                        if (start != end)
                            coveredRange.Remove((start, string.Empty));
                    }
                }
                else
                if (string.IsNullOrEmpty(startA) && !string.IsNullOrEmpty(endA))
                {
                    Debug.Assert(start > 0 && end > 0);

                    if ((eoState & EvenOddState.START_END_MIXED_EVENODD) == EvenOddState.START_END_MIXED_EVENODD)
                    {
                        AddToCoveredRangeByContinuousInterval();
                    }
                    else
                    {
                        AddToCoveredRangeByEvenOddInterval();
                    }
                    for (char c = 'A'; c <= endA[0]; c++)
                        coveredRange.Add((end, c.ToString()));
                    if (start != end)
                        coveredRange.Remove((end, string.Empty));
                }
                // Both startA and endA are null
                else
                {
                    if (end == 0 || start == end)
                    {
                        coveredRange.Add((start, startA));
                    }
                    else
                    if (Math.Abs(start - end) == 1)
                    {
                        coveredRange.Add((start, string.Empty));
                        coveredRange.Add((end, string.Empty));
                    }
                    else
                    {
                        if ((eoState & EvenOddState.START_END_MIXED_EVENODD) == EvenOddState.START_END_MIXED_EVENODD)
                        {
                            AddToCoveredRangeByContinuousInterval();
                        }
                        else
                        {
                            AddToCoveredRangeByEvenOddInterval();
                        }
                    }
                }
            }

            GenCoveredRange(stFrom, stFromAlpha, stTo, stToAlpha);
        }

        public EvenOddState CheckEvenOddState()
        {
            if (stTo == 0)
                return EvenOddState.GOT_START_ONLY | (stFrom % 2 == 0 ? EvenOddState.BOTH_START_END_EVEN : EvenOddState.BOTH_START_END_ODD);
            if ((stFrom % 2 == 0) && (stTo % 2 == 0))
                return EvenOddState.BOTH_START_END_EVEN;
            if ((stFrom % 2 != 0) && (stTo % 2 != 0))
                return EvenOddState.BOTH_START_END_ODD;
            return EvenOddState.START_END_MIXED_EVENODD;
        }

        public bool IsLetterHeading =>
            (stFrom == 0 && stTo == 0 && string.IsNullOrEmpty(stToAlpha) && !string.IsNullOrEmpty(stFromAlpha));

        public (bool isLetterHeading, bool hasFromOnly, bool hasFromAlpha) FromSideInfo
        {
            get
            {
                if (IsLetterHeading)
                    return (true, true, true);
                return (false, stFrom != 0 && stTo == 0, !string.IsNullOrEmpty(stFromAlpha));
            }
        }

        public (bool hasTo, bool hasToAlpha) ToSideInfo
        {
            get => (stTo != 0, !string.IsNullOrEmpty(stToAlpha));
        }

        public bool IsFromSideEqual(StreetNoExpansion stEx)
        {
            if (IsLetterHeading && stEx.IsLetterHeading)
                return stFromAlpha == stEx.stFromAlpha;
            if (IsLetterHeading && !stEx.IsLetterHeading ||
                !IsLetterHeading && stEx.IsLetterHeading)
                return false;
            if (stEx.stFrom != stFrom)
                return false;
            if (string.IsNullOrEmpty(stEx.stFromAlpha) && string.IsNullOrEmpty(stFromAlpha))
                return stFrom == stEx.stFrom;
            if ((string.IsNullOrEmpty(stEx.stFromAlpha) && !string.IsNullOrEmpty(stFromAlpha)) ||
                (!string.IsNullOrEmpty(stEx.stFromAlpha) && string.IsNullOrEmpty(stFromAlpha)))
                return false;
            return stEx.stFromAlpha == stFromAlpha;
        }

        public bool IsToSideEqual(StreetNoExpansion stEx)
        {
            if (stEx.stTo != stTo)
                return false;
            if (string.IsNullOrEmpty(stEx.stToAlpha) && string.IsNullOrEmpty(stToAlpha))
                return stTo == stEx.stTo;
            if ((string.IsNullOrEmpty(stEx.stToAlpha) && !string.IsNullOrEmpty(stToAlpha)) ||
                (!string.IsNullOrEmpty(stEx.stToAlpha) && string.IsNullOrEmpty(stToAlpha)))
                return false;
            return stEx.stToAlpha == stToAlpha;
        }

        public List<(int no, string alpha)> CoveredRange => coveredRange;

        public StreetNoMatchingStatus MatchStreetNo(StreetNoExpansion stExpansion)
        {
            var resFrom = stExpansion.FromSideInfo;
            if (resFrom.isLetterHeading)
            {
                if (!stExpansion.IsLetterHeading)
                    return StreetNoMatchingStatus.NOT_MATCH;
                return IsFromSideEqual(stExpansion) ? StreetNoMatchingStatus.EXACT_MATCH : StreetNoMatchingStatus.NOT_MATCH;
            }
            if (resFrom.hasFromOnly)
            {
                if (coveredRange.Contains((stExpansion.stFrom, stExpansion.stFromAlpha)))
                {
                    var f = coveredRange.First();
                    if (f.no == stExpansion.stFrom && f.alpha == stExpansion.stFromAlpha)
                        return StreetNoMatchingStatus.MATCH_FROM;
                    var l = coveredRange.Last();
                    if (l.no == stExpansion.stFrom && l.alpha == stExpansion.stFromAlpha)
                        return StreetNoMatchingStatus.MATCH_TO;
                    return StreetNoMatchingStatus.IN_RANGE;
                }
                else
                    return StreetNoMatchingStatus.NOT_MATCH;
            }
            else
            // Has got both From and To, then needs to do range check.
            {
                var resTo = stExpansion.ToSideInfo;
                Debug.Assert(resTo.hasTo);
                if (stExpansion.stFrom == stFrom && stExpansion.stTo == stTo &&
                    stExpansion.stFromAlpha == stFromAlpha && stExpansion.stToAlpha == stToAlpha)
                    return StreetNoMatchingStatus.EXACT_MATCH;
                return stExpansion.CoveredRange.Except(CoveredRange).Any() ?
                    StreetNoMatchingStatus.NOT_MATCH : StreetNoMatchingStatus.IN_RANGE;
            }
        }

        public StreetNoMatchingStatus MatchStreetNo(int from, string fromA, int to, string toA)
        {
            return MatchStreetNo(new StreetNoExpansion(from, fromA, to, toA));
        }

        public override string ToString()
        {
            var s1 = new StringBuilder($"{stFrom},{(string.IsNullOrEmpty(stFromAlpha) ? "N/A" : stFromAlpha)} - {stTo},{(string.IsNullOrEmpty(stToAlpha) ? "N/A" : stToAlpha)}{Environment.NewLine}");
            s1.Append($"Covered Range:{Environment.NewLine}");
            coveredRange.ForEach(e => s1.Append(e).Append(Environment.NewLine));
            return s1.ToString();
        }
    }





    [JsonObject]
    class Extension1 : Base
    {
        [JsonProperty]
        public string MyString;
    }

    [JsonObject]
    class Extension2 : Base
    {
        [JsonProperty]
        public string AnotherString;
    }

    [JsonObject]
    class MainStore
    {
        [JsonProperty]
        public string name;
        [JsonProperty]
        public Base extension1;
        [JsonProperty]
        public Base extension2;
        [JsonProperty]
        List<Base> BaseList = new List<Base>();
        public MainStore()
        {
            name = "MainStore";
            extension1 = new Extension1()
            {
                BaseString = "Base string of externson 1",
                BaseID = Int32.MaxValue,
                MyString = "This is Extension 1"
            };
            extension2 = new Extension2()
            {
                BaseString = "Base string of externson 2",
                BaseID = Int32.MinValue,
                AnotherString = "This is Extension 2"
            };
            for (int i = 0; i < 10; i++)
            {
                if (i % 2 == 0)
                {
                    var ext = new Extension1()
                    {
                        BaseString = $"BaseString of 1 in Loop {i}",
                        BaseID = i,
                        MyString = $"This is my string {i}"
                    };
                    BaseList.Add(ext);
                }
                else
                {
                    var ext = new Extension2()
                    {
                        BaseString = $"BaseString of 2 in Loop {i}",
                        BaseID = i,
                        AnotherString = $"This is another string {i}"
                    };
                    BaseList.Add(ext);
                }
            }

        }

        public void Serialize(string fn)
        {
            FileStream fs = new FileStream(fn, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            using (StreamWriter sw = new StreamWriter(fs))
            using (JsonTextWriter writer = new JsonTextWriter(sw))
            {
                JsonSerializer serializer = new JsonSerializer()
                {
                    Formatting = Newtonsoft.Json.Formatting.Indented,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    TypeNameHandling = TypeNameHandling.All
                };
                serializer.Serialize(writer, this);
            }
        }

 

 

    }
}


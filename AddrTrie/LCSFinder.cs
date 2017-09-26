using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddrTrie
{
    public class LCSFinder
    {
        public enum BackTracking
        {
            NEITHER,
            UP,
            LEFT,
            UP_AND_LEFT
        }

        public static (string commonSequence, int score) GetLCS(string s, string t)
        {
            string[] ss = s.Split(' ');
            string[] tt = t.Split(' ');
            return LCS(ss, tt);
        }

        private static int ConsecutiveMeasure(int k)
        {
            //f(k)=k*a - b;
            return k * k;
        }

        private static (string, int) LCS(string[] list1, string[] list2)
        {
            int m = list1.Length;
            int n = list2.Length;

            int[,] lcs = new int[m + 1, n + 1];
            BackTracking[,] backTracer = new BackTracking[m + 1, n + 1];
            int[,] w = new int[m + 1, n + 1];
            int i, j;

            for (i = 0; i <= m; ++i)
            {
                lcs[i, 0] = 0;
                backTracer[i, 0] = BackTracking.UP;

            }
            for (j = 0; j <= n; ++j)
            {
                lcs[0, j] = 0;
                backTracer[0, j] = BackTracking.LEFT;
            }

            for (i = 1; i <= m; ++i)
            {
                for (j = 1; j <= n; ++j)
                {
                    if (list1[i - 1].Equals(list2[j - 1]))
                    {
                        int k = w[i - 1, j - 1];
                        //lcs[i,j] = lcs[i-1,j-1] + 1;
                        lcs[i, j] = lcs[i - 1, j - 1] + ConsecutiveMeasure(k + 1) - ConsecutiveMeasure(k);
                        backTracer[i, j] = BackTracking.UP_AND_LEFT;
                        w[i, j] = k + 1;
                    }
                    else
                    {
                        lcs[i, j] = lcs[i - 1, j - 1];
                        backTracer[i, j] = BackTracking.NEITHER;
                    }

                    if (lcs[i - 1, j] >= lcs[i, j])
                    {
                        lcs[i, j] = lcs[i - 1, j];
                        backTracer[i, j] = BackTracking.UP;
                        w[i, j] = 0;
                    }

                    if (lcs[i, j - 1] >= lcs[i, j])
                    {
                        lcs[i, j] = lcs[i, j - 1];
                        backTracer[i, j] = BackTracking.LEFT;
                        w[i, j] = 0;
                    }
                }
            }

            i = m;
            j = n;

            string subseq = "";
            int p = lcs[i, j];

            //trace the backtracking matrix.
            while (i > 0 || j > 0)
            {
                if (backTracer[i, j] == BackTracking.UP_AND_LEFT)
                {
                    i--;
                    j--;
                    subseq = list1[i] + subseq;
                    //Trace.WriteLine(i + " " + list1[i] + " " + j);
                }

                else if (backTracer[i, j] == BackTracking.UP)
                {
                    i--;
                }

                else if (backTracer[i, j] == BackTracking.LEFT)
                {
                    j--;
                }
            }
            return (subseq, p);
        }
    }
}

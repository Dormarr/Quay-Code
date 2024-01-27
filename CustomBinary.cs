using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quay_Code
{
    class CustomBinary
    {
        public static string Write4Bit(char[] input)
        {
            Dictionary<char, string> BitKey = new Dictionary<char, string> {
                { '0', "1100" },
                { '1', "0001" },
                { '2', "0010" },
                { '3', "0011" },
                { '4', "0100" },
                { '5', "0101" },
                { '6', "0110" },
                { '7', "0111" },
                { '8', "1000" },
                { '9', "1001" }
            };

            StringBuilder stringBuilder = new StringBuilder();

            foreach (char c in input)
            {
                if (BitKey.ContainsKey(c))
                {
                    stringBuilder.Append(BitKey[c]);
                }
            }
            return stringBuilder.ToString();
        }

        public static int[] Read4Bit(string[] input)
        {
            Dictionary<string, int> BitKeyBack = new Dictionary<string, int>
            {
                { "1100", 0},
                { "0001", 1},
                { "0010", 2},
                { "0011", 3},
                { "0100", 4},
                { "0101", 5},
                { "0110", 6},
                { "0111", 7},
                { "1000", 8},
                { "1001", 9}
            };

            List<int> ints = new List<int>();

            for(int i=0 ; i<input.Length; i++)
            {
                if (BitKeyBack.ContainsKey(input[i]))
                {
                    ints.Add(BitKeyBack[input[i]]);
                }
            }

            return ints.ToArray();
        }

        public static string WriteBinary(string input, int sizeMetric)
        {
            StringBuilder sb = new StringBuilder();
            string output = "";

            foreach(char c in input)
            {
                sb.Append(Convert.ToString(c, 2).PadLeft(8, '0'));
            }

            //pad binary to fill d-bits.

            switch (sizeMetric)
            {
                case 12:
                    sb.Append("011011");
                    break;
                case 18:
                    sb.Append("011011");
                    break;
                case 24:
                    sb.Append("11");
                    break;
                case 32:
                    sb.Append("1101");
                    break;
                default:
                    break;
            }

            output = sb.ToString();

            return output;
        }
    }
}

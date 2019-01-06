using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rule = System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.List<string>>;

namespace Generator_analizatoare_LL1
{
    class CodeGenerator
    {
        public LL1Grammar Grammar { get; set; }

        private string GenerateRuleMethods(Rule rule)
        {
            string checkEachSymbol = string.Empty;

            foreach (var symbol in rule.Value)
            {
                if (Grammar.Nonterminals.Contains(symbol))
                {
                    checkEachSymbol += string.Join(Environment.NewLine,
                   $"           if (!{symbol}())",
                    "               return false;",
                    Environment.NewLine);
                }
                else
                {
                    checkEachSymbol += string.Join(Environment.NewLine,
                   $"           if (input[index] != \"{symbol}\")",
                    "               return false;",
                    "           index++; ",
                    Environment.NewLine);
                }
            }

            System.Diagnostics.Debug.Assert(Grammar.ProductionRules.Contains(rule));

            return string.Join(Environment.NewLine,
               $"       private static bool Rule{Grammar.ProductionRules.IndexOf(rule).ToString()}()",
                "       {",
                checkEachSymbol +
                "            return true;",
                "       }",
                "");
        }

        private string GenerateNonterminalMethods(string nonterminal)
        {
            string stringWithIfs = string.Empty;
            foreach (var terminal in Grammar.Terminals)
            {
                Rule rule = Grammar.ParsingTable[nonterminal, terminal];
                int indexOfRule = Grammar.ProductionRules.IndexOf(rule);
                if (indexOfRule >= 0)
                    stringWithIfs += string.Join(Environment.NewLine,
                        $"           if (input[index] == \"{terminal}\")",
                        $"              return Rule{indexOfRule}();",
                        Environment.NewLine);
            }
            return string.Join(Environment.NewLine,
               $"       private static bool {nonterminal}()",
                "       {",
                stringWithIfs +
                "           return false;",
                "       }",
                "");
        }

        public string GetCode()
        {
            string methods = string.Empty;

            foreach (var nonterminal in Grammar.Nonterminals)
                methods += Environment.NewLine + GenerateNonterminalMethods(nonterminal);
            foreach (var rule in Grammar.ProductionRules)
                methods += Environment.NewLine + GenerateRuleMethods(rule);


            return string.Join(Environment.NewLine,
                "using System;",
                "using System.Linq;",
                "using System.Text.RegularExpressions;",
                "",
                "namespace LL1ConsoleTest",
                "{",
                "    class Program",
                "    {",
                "        private static string[] input;",
                "        private static int index;",
                "",
                "        static void Main(string[] args)",
                "        {",
                "            if (args.Any())",
                "            {",
                "                index = 0;",
                "                input = args;",
               @"                input = new[] { ""$"" }.Concat(input.Concat(new[] { ""$"" })).ToArray();",
               $"                Console.WriteLine({Grammar.StartSymbol}());",
                "                return;",
                "            }",
                "            string line;",
                "            while ((line = Console.ReadLine()).Any())",
                "            {",
                "                index = 0;",
               @"                input = new Regex(@""\s"").Split(line);",
               @"                input = new[] { ""$"" }.Concat(input.Concat(new[] { ""$"" })).ToArray();",
               $"                Console.WriteLine({Grammar.StartSymbol}());",
                "            }",
                "        }",
                methods +
                "    }",
                "}"
                );
        }
    }
}

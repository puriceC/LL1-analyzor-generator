using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Rule = System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.List<string>>;


namespace Generator_analizatoare_LL1
{


    class Grammar
    {

        public string StartSymbol { get; set; }
        public List<string> Terminals { get; set; }
        public List<string> Nonterminals { get; set; }
        public List<Rule> ProductionRules { get; set; }

        public Grammar()
        {
            StartSymbol = string.Empty;
            Terminals = new List<string>();
            Nonterminals = new List<string>();
            ProductionRules = new List<Rule>();
        }
        public Grammar(Grammar grammar)
        {
            StartSymbol = grammar.StartSymbol;
            Terminals = grammar.Terminals;
            Nonterminals = grammar.Nonterminals;
            ProductionRules = grammar.ProductionRules;
        }

        private List<string> SplitByWhiteSpace(string s)
        {
            return new Regex(@"\s").Split(s).ToList();
        }

        public void ReadFile(string fileName)
        {
            string[] text = File.ReadLines(fileName).ToArray();
            StartSymbol = text[0];
            Nonterminals = SplitByWhiteSpace(text[1]);
            Terminals = SplitByWhiteSpace(text[2]);
            int ruleCount = int.Parse(text[3]);
            for (int i = 4; i < ruleCount + 4; i++)
            {
                int index = text[i].IndexOf(':');
                string leftSide = text[i].Substring(0, index);
                string rightSide = text[i].Substring(index + 1);
                ProductionRules.Add(new Rule(leftSide, SplitByWhiteSpace(rightSide)));
            }
        }
    }
}
/*
        <TextBlock Grid.Column="1" Grid.Row="1" x:Name="pathBlock"/>
        <TextBox x:Name="textblock" TextWrapping="Wrap" Text="TextBlock" />
        <Button Grid.Column="2" Grid.Row="1" Content="Button" x:Name="btn" Click="Button_Click"/>
        <Button Grid.Column="2" Grid.Row="2" Content="Button" x:Name="btn2" Click="Button_Click"/>
 * */

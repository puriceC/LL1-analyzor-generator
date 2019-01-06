using System;
using System.Collections.Generic;
using System.Linq;

using Rule = System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.List<string>>;


namespace Generator_analizatoare_LL1
{


    class LL1Grammar : Grammar
    {
        // is indexed with 2 strings ( stack symbol and input symbol) and returns the rule
        public class Table
        {
            public Table(Grammar grammar)
            {
                this.grammar = grammar;
                int terminalsCount = grammar.Terminals.Count;
                int nonterminalsCount = grammar.Nonterminals.Count;
                matrix = new Rule[nonterminalsCount + terminalsCount, terminalsCount];
            }

            private readonly Rule[,] matrix;
            private readonly Grammar grammar;

            private int IndexOf(string symbol)
            {
                int index = grammar.Terminals.IndexOf(symbol);
                if (index == -1) // if it wasn't found in the terminals' set it must be a nonterminal
                    index = grammar.Terminals.Count + grammar.Nonterminals.IndexOf(symbol);

                System.Diagnostics.Debug.Assert(index >= 0);

                return index;
            }


            // Define the indexer to allow client code to use [] notation.
            public Rule this[string stackSymbol, string inputSymbol]
            {
                get => matrix[IndexOf(stackSymbol), IndexOf(inputSymbol)];
                set => matrix[IndexOf(stackSymbol), IndexOf(inputSymbol)] = value;
            }
        }

        public Table ParsingTable { get; set; }
        private Dictionary<string, List<string>> firstSets = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> followSets = new Dictionary<string, List<string>>();
        private Dictionary<Rule, List<string>> firstAndFollowSets = new Dictionary<Rule, List<string>>();
        private int uniqueSymbolId = 0;

        public LL1Grammar(Grammar grammar) : base(grammar)
        {
            // changes the grammar to only accept sentences between $ and $
            var oldStart = StartSymbol;
            StartSymbol = "LL1_Start";
            Terminals.Add("$");
            Nonterminals.Add(StartSymbol);
            ProductionRules.Add(new Rule(StartSymbol, new List<string> { "$", oldStart, "$" }));

            RemoveLeftRecursion();
            LeftFactor();

            //check for collisions in the first and follow sets 
            foreach (var nonterminal in Nonterminals)
            {
                //select all symbols from this nonterminal's first and follow sets
                var list = ProductionRules.Where(rule => rule.Key.Equals(nonterminal)).SelectMany(rule => D(rule));
                //if there are duplicates, distinct will return a list with a different count
                if (list.Distinct().Count() != list.Count())
                    throw new Exception("Grammar cannot be made LL1");
            }

            ParsingTable = new Table(this);
            foreach (var nonterminal in Nonterminals)
            {
                foreach (var rule in ProductionRules.Where(rule => rule.Key == nonterminal))
                {
                    foreach (var terminal in D(rule))
                    {
                        ParsingTable[nonterminal, terminal] = rule;
                    }
                }
            }
        }

        #region Methods

        private List<string> D(Rule rule)
        {
            // check if it was already calculated
            if (firstAndFollowSets.ContainsKey(rule))
                return firstAndFollowSets[rule];
            // save the calculated value and return it
            if (!rule.Value.Any())
                return firstAndFollowSets[rule] = Follow(rule.Key);
            return firstAndFollowSets[rule] = First(rule.Value.ElementAt(0));
        }
        private List<string> First(string symbol)
        {
            if (firstSets.ContainsKey(symbol))
                return firstSets[symbol];

            firstSets[symbol] = new List<string>();

            if (Terminals.Contains(symbol))
                firstSets[symbol].Add(symbol);

            foreach (var rule in ProductionRules.Where(r => r.Key == symbol))
            {
                if (!rule.Value.Any())
                    firstSets[symbol] = firstSets[symbol].Union(Follow(rule.Key)).ToList();
                else if (Terminals.Contains(rule.Value.First()))
                    firstSets[symbol].Add(rule.Value.First());
                else if (Nonterminals.Contains(rule.Value.First()) && rule.Value.First() != symbol)
                    firstSets[symbol] = firstSets[symbol].Union(First(rule.Value.First())).ToList();
            }
            firstSets[symbol] = firstSets[symbol].Distinct().ToList();
            return firstSets[symbol];
        }
        private List<string> Follow(string symbol)
        {
            if (followSets.ContainsKey(symbol))
                return followSets[symbol];

            followSets[symbol] = new List<string>();

            foreach (var rule in ProductionRules)
            {
                for (int index = 0; index < rule.Value.Count; ++index)
                {
                    if (rule.Value.ElementAt(index) == symbol)
                    {
                        if (index + 1 >= rule.Value.Count)
                            followSets[symbol] = followSets[symbol].Union(Follow(rule.Key)).ToList();
                        else if (Terminals.Contains(rule.Value.ElementAt(index + 1)))
                            followSets[symbol].Add(rule.Value.ElementAt(index + 1));
                        else if (Nonterminals.Contains(rule.Value.ElementAt(index + 1)))
                            followSets[symbol] = followSets[symbol].Union(First(rule.Value.ElementAt(index + 1))).ToList();
                    }
                }
            }
            followSets[symbol] = followSets[symbol].Distinct().ToList();
            return followSets[symbol];
        }


        private bool IsLeftRecursive()
        {
            return ProductionRules.Any(rule => rule.Key == rule.Value.FirstOrDefault());
        }
        private bool IsNotFactorised()
        {
            foreach (var rule1 in ProductionRules)
            {
                if (ProductionRules.Any(rule2 =>
                !rule1.Equals(rule2) &&
                rule1.Key == rule2.Key &&
                rule1.Value.Any() &&
                rule1.Value.First() == rule2.Value.FirstOrDefault()))
                    return true;
            }
            return false;
        }

        private void RemoveLeftRecursion()
        {
            while (IsLeftRecursive())
            {
                var toBeAdded = new List<Rule>();
                var toBeRemoved = new List<Rule>();

                foreach (var r1 in ProductionRules.FindAll(rule => rule.Key == rule.Value.FirstOrDefault()))
                {
                    // add a right recursive symbol at the end of each rule that expends the same nonterminal
                    string newSymbol = r1.Key + $"__{uniqueSymbolId++}";
                    Nonterminals.Add(newSymbol);
                    foreach (var r2 in ProductionRules.Where(r2 => (r1.Key == r2.Key)))
                        r2.Value.Add(newSymbol);

                    // add rules for a rigth recursive new nonterminal
                    toBeAdded.Add(new Rule(newSymbol, new List<string>()));
                    toBeAdded.Add(new Rule(newSymbol, r1.Value.GetRange(1, r1.Value.Count - 1)));

                    // remove the left recursive rule
                    toBeRemoved.Add(r1);
                    r1.Value.RemoveAll(r => true);
                }
                // commit the changes to the actual list
                ProductionRules.AddRange(toBeAdded);
                toBeRemoved.ForEach(rule => ProductionRules.Remove(rule));
            }
        }
        private void LeftFactor()
        {
            while (IsNotFactorised())
            {
                var toBeAdded = new List<Rule>();
                var toBeRemoved = new List<Rule>();

                foreach (var r1 in ProductionRules.ToList())
                {
                    foreach (var r2 in ProductionRules.Where(r2 => (r1.Key == r2.Key) && !(r1.Value == r2.Value)).ToList())
                    {
                        var communPart = MatchAsMuch(r1.Value, r2.Value);
                        if (communPart.Any())
                        {
                            // create a new nonterminal
                            string newSymbol = r1.Key + $"_{uniqueSymbolId++}";
                            Nonterminals.Add(newSymbol);

                            // add rules for the new symbol
                            r1.Value.RemoveRange(0, communPart.Count);
                            r2.Value.RemoveRange(0, communPart.Count);
                            toBeAdded.Add(new Rule(newSymbol, r1.Value));
                            toBeAdded.Add(new Rule(newSymbol, r2.Value));

                            // add rule for the old symbol to expend into the common part + the new symbol
                            communPart.Add(newSymbol);
                            toBeAdded.Add(new Rule(r1.Key, communPart));

                            // delete nonfactorised rules
                            toBeRemoved.Add(r1);
                            toBeRemoved.Add(r2);
                        }
                    }
                }

                ProductionRules.AddRange(toBeAdded);
                toBeRemoved.ForEach(rule => ProductionRules.Remove(rule));
            }
        }
        private List<string> MatchAsMuch(List<string> left, List<string> right)
        {
            List<string> matches = new List<string>();
            int minCount = Math.Min(left.Count, right.Count);
            for (int i = 0; (i < minCount) && (left[i] == right[i]); i++)
                matches.Add(left[i]);
            return matches;
        }
        #endregion
    }
}

﻿/*

Copyright Robert Vesse 2009-10
rvesse@vdesign-studios.com

------------------------------------------------------------------------

This file is part of dotNetRDF.

dotNetRDF is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

dotNetRDF is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with dotNetRDF.  If not, see <http://www.gnu.org/licenses/>.

------------------------------------------------------------------------

dotNetRDF may alternatively be used under the LGPL or MIT License

http://www.gnu.org/licenses/lgpl.html
http://www.opensource.org/licenses/mit-license.php

If these licenses are not suitable for your intended use please contact
us at the above stated email address to discuss alternative
terms.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.Common;
using VDS.RDF.Parsing;
using VDS.RDF.Query.Algebra;
using VDS.RDF.Nodes;
using VDS.RDF.Query.Expressions.Primary;
using VDS.RDF.Query.Patterns;

namespace VDS.RDF.Query.Expressions.Functions.Sparql.Boolean
{
    /// <summary>
    /// Represents an EXIST/NOT EXIST clause used as a Function in an Expression
    /// </summary>
    public class ExistsFunction 
        : ISparqlExpression
    {
        private GraphPattern _pattern;
        private bool _mustExist;

        private BaseMultiset _result;
        private int? _lastInput;
        private int _lastCount = 0;
        private List<System.String> _joinVars;
        private HashSet<int> _exists;

        /// <summary>
        /// Creates a new EXISTS/NOT EXISTS function
        /// </summary>
        /// <param name="pattern">Graph Pattern</param>
        /// <param name="mustExist">Whether this is an EXIST</param>
        public ExistsFunction(GraphPattern pattern, bool mustExist)
        {
            this._pattern = pattern;
            this._mustExist = mustExist;
        }

        /// <summary>
        /// Gets the Value of this function which is a Boolean as a Literal Node
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <param name="bindingID">Binding ID</param>
        /// <returns></returns>
        public IValuedNode Evaluate(SparqlEvaluationContext context, int bindingID)
        {
            if (this._result == null || this._lastInput == null || (int)this._lastInput != context.InputMultiset.GetHashCode() || this._lastCount != context.InputMultiset.Count) this.EvaluateInternal(context);

            if (this._result is IdentityMultiset) return new BooleanNode(null, true);
            if (this._mustExist)
            {
                //If an EXISTS then Null/Empty Other results in false
                if (this._result is NullMultiset) return new BooleanNode(null, false);
                if (this._result.IsEmpty) return new BooleanNode(null, false);
            }
            else
            {
                //If a NOT EXISTS then Null/Empty results in true
                if (this._result is NullMultiset) return new BooleanNode(null, true);
                if (this._result.IsEmpty) return new BooleanNode(null, true);
            }

            if (this._joinVars.Count == 0)
            {
                //If Disjoint then all solutions are compatible
                if (this._mustExist)
                {
                    //If Disjoint and must exist then true since
                    return new BooleanNode(null, true);
                }
                else
                {
                    //If Disjoint and must not exist then false
                    return new BooleanNode(null, false);
                }
            }

            ISet x = context.InputMultiset[bindingID];

            bool exists = this._exists.Contains(x.ID);
            if (this._mustExist)
            {
                //If an EXISTS then return the value of exists i.e. are there any compatible solutions
                return new BooleanNode(null, exists);
            }
            else
            {
                //If a NOT EXISTS then return the negation of exists i.e. if compatible solutions exist then we must return false, if none we return true
                return new BooleanNode(null, !exists);
            }
        }

        /// <summary>
        /// Internal method which evaluates the Graph Pattern
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <remarks>
        /// We only ever need to evaluate the Graph Pattern once to get the Results
        /// </remarks>
        private void EvaluateInternal(SparqlEvaluationContext context)
        {
            this._result = null;
            this._lastInput = context.InputMultiset.GetHashCode();
            this._lastCount = context.InputMultiset.Count;

            //REQ: Optimise the algebra here
            ISparqlAlgebra existsClause = this._pattern.ToAlgebra();
            BaseMultiset initialInput = context.InputMultiset;
            this._result = context.Evaluate(existsClause);
            context.InputMultiset = initialInput;

            //This is the new algorithm which is also correct but is O(3n) so much faster and scalable
            //Downside is that it does require more memory than the old algorithm
            this._joinVars = context.InputMultiset.Variables.Where(v => this._result.Variables.Contains(v)).ToList();
            if (this._joinVars.Count == 0) return;

            List<HashTable<INode, int>> values = new List<HashTable<INode, int>>();
            List<List<int>> nulls = new List<List<int>>();
            foreach (System.String var in this._joinVars)
            {
                values.Add(new HashTable<INode, int>(HashTableBias.Enumeration));
                nulls.Add(new List<int>());
            }

            //First do a pass over the LHS Result to find all possible values for joined variables
            foreach (ISet x in context.InputMultiset.Sets)
            {
                int i = 0;
                foreach (System.String var in this._joinVars)
                {
                    INode value = x[var];
                    if (value != null)
                    {
                        values[i].Add(value, x.ID);
                    }
                    else
                    {
                        nulls[i].Add(x.ID);
                    }
                    i++;
                }
            }

            //Then do a pass over the RHS and work out the intersections
            this._exists = new HashSet<int>();
            foreach (ISet y in this._result.Sets)
            {
                IEnumerable<int> possMatches = null;
                int i = 0;
                foreach (System.String var in this._joinVars)
                {
                    INode value = y[var];
                    if (value != null)
                    {
                        if (values[i].ContainsKey(value))
                        {
                            possMatches = (possMatches == null ? values[i].GetValues(value).Concat(nulls[i]) : possMatches.Intersect(values[i].GetValues(value).Concat(nulls[i])));
                        }
                        else
                        {
                            possMatches = Enumerable.Empty<int>();
                            break;
                        }
                    }
                    else
                    {
                        //Don't forget that a null will be potentially compatible with everything
                        possMatches = (possMatches == null ? context.InputMultiset.SetIDs : possMatches.Intersect(context.InputMultiset.SetIDs));
                    }
                    i++;
                }
                if (possMatches == null) continue;

                //Look at possible matches, if is a valid match then mark the set as having an existing match
                //Don't reconsider sets which have already been marked as having an existing match
                foreach (int poss in possMatches)
                {
                    if (this._exists.Contains(poss)) continue;
                    if (context.InputMultiset[poss].IsCompatibleWith(y, this._joinVars))
                    {
                        this._exists.Add(poss);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the Variables used in this Expression
        /// </summary>
        public IEnumerable<string> Variables
        {
            get 
            { 
                return (from p in this._pattern.TriplePatterns
                        from v in p.Variables
                        select v).Distinct();
            }
        }

        /// <summary>
        /// Gets the String representation of the Expression
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder output = new StringBuilder();
            if (this._mustExist)
            {
                output.Append("EXISTS ");
            }
            else
            {
                output.Append("NOT EXISTS ");
            }
            output.Append(this._pattern.ToString());
            return output.ToString();
        }

        /// <summary>
        /// Gets the Type of the Expression
        /// </summary>
        public SparqlExpressionType Type
        {
            get
            {
                return SparqlExpressionType.GraphOperator;
            }
        }

        /// <summary>
        /// Gets the Functor of the Expression
        /// </summary>
        public string Functor
        {
            get
            {
                if (this._mustExist)
                {
                    return SparqlSpecsHelper.SparqlKeywordExists;
                }
                else
                {
                    return SparqlSpecsHelper.SparqlKeywordNotExists;
                }
            }
        }

        /// <summary>
        /// Gets the Arguments of the Expression
        /// </summary>
        public IEnumerable<ISparqlExpression> Arguments
        {
            get
            {
                return new ISparqlExpression[] { new GraphPatternTerm(this._pattern) };
            }
        }

        /// <summary>
        /// Transforms the Expression using the given Transformer
        /// </summary>
        /// <param name="transformer">Expression Transformer</param>
        /// <returns></returns>
        public ISparqlExpression Transform(IExpressionTransformer transformer)
        {
            ISparqlExpression temp = transformer.Transform(new GraphPatternTerm(this._pattern));
            if (temp is GraphPatternTerm)
            {
                return new ExistsFunction(((GraphPatternTerm)temp).Pattern, this._mustExist);
            }
            else
            {
                throw new RdfQueryException("Unable to transform an EXISTS/NOT EXISTS function since the expression transformer in use failed to transform the inner Graph Pattern Expression to another Graph Pattern Expression");
            }
        }
    }
}

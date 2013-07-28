﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF.Parsing;
using VDS.RDF.Nodes;

namespace VDS.RDF.Query.Expressions.Functions.Sparql.String
{
    /// <summary>
    /// Represents the SPARQL CONCAT function
    /// </summary>
    public class ConcatFunction
        : ISparqlExpression
    {
        private List<ISparqlExpression> _exprs = new List<ISparqlExpression>();

        /// <summary>
        /// Creates a new SPARQL Concatenation function
        /// </summary>
        /// <param name="expressions">Enumeration of expressions</param>
        public ConcatFunction(IEnumerable<ISparqlExpression> expressions)
        {
            this._exprs.AddRange(expressions);
        }

        /// <summary>
        /// Gets the Value of the function as evaluated in the given Context for the given Binding ID
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="bindingID">Binding ID</param>
        /// <returns></returns>
        public IValuedNode Evaluate(SparqlEvaluationContext context, int bindingID)
        {
            string langTag = null;
            bool allString = true;
            bool allSameTag = true;

            StringBuilder output = new StringBuilder();
            foreach (ISparqlExpression expr in this._exprs)
            {
                INode temp = expr.Evaluate(context, bindingID);
                if (temp == null) throw new RdfQueryException("Cannot evaluate the SPARQL CONCAT() function when an argument evaluates to a Null");

                switch (temp.NodeType)
                {
                    case NodeType.Literal:
                        //Check whether the Language Tags and Types are the same
                        //We need to do this so that we can produce the appropriate output
                        ILiteralNode lit = (ILiteralNode)temp;
                        if (langTag == null)
                        {
                            langTag = lit.Language;
                        }
                        else
                        {
                            allSameTag = allSameTag && (langTag.Equals(lit.Language));
                        }

                        //Have to ensure that if Typed is an xsd:string
                        if (lit.DataType != null && !lit.DataType.ToString().Equals(XmlSpecsHelper.XmlSchemaDataTypeString)) throw new RdfQueryException("Cannot evaluate the SPARQL CONCAT() function when an argument is a Typed Literal which is not an xsd:string");
                        allString = allString && lit.DataType != null;

                        output.Append(lit.Value);
                        break;

                    default:
                        throw new RdfQueryException("Cannot evaluate the SPARQL CONCAT() function when an argument is not a Literal Node");
                }
            }

            //Produce the appropriate literal form depending on our inputs
            if (allString)
            {
                return new StringNode(null, output.ToString(), UriFactory.Create(XmlSpecsHelper.XmlSchemaDataTypeString));
            }
            else if (allSameTag)
            {
                return new StringNode(null, output.ToString(), langTag);
            }
            else
            {
                return new StringNode(null, output.ToString());
            }
        }

        /// <summary>
        /// Gets the Arguments the function applies to
        /// </summary>
        public IEnumerable<ISparqlExpression> Arguments
        {
            get
            {
                return this._exprs;
            }
        }

        /// <summary>
        /// Gets the Variables used in the function
        /// </summary>
        public IEnumerable<string> Variables
        {
            get
            {
                return (from expr in this._exprs
                        from v in expr.Variables
                        select v);
            }
        }

        /// <summary>
        /// Gets the String representation of the function
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder output = new StringBuilder();
            output.Append(SparqlSpecsHelper.SparqlKeywordConcat);
            output.Append('(');
            for (int i = 0; i < this._exprs.Count; i++)
            {
                output.Append(this._exprs[i].ToString());
                if (i < this._exprs.Count - 1) output.Append(", ");
            }
            output.Append(")");
            return output.ToString();
        }

        /// <summary>
        /// Gets the Type of the SPARQL Expression
        /// </summary>
        public SparqlExpressionType Type
        {
            get
            {
                return SparqlExpressionType.Function;
            }
        }

        /// <summary>
        /// Gets the Functor of the expression
        /// </summary>
        public string Functor
        {
            get
            {
                return SparqlSpecsHelper.SparqlKeywordConcat;
            }
        }

        /// <summary>
        /// Transforms the Expression using the given Transformer
        /// </summary>
        /// <param name="transformer">Expression Transformer</param>
        /// <returns></returns>
        public ISparqlExpression Transform(IExpressionTransformer transformer)
        {
            return new ConcatFunction(this._exprs.Select(e => transformer.Transform(e)));
        }
    }
}

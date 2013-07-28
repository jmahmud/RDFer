﻿/*

Copyright Robert Vesse 2009-12
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
using System.Reflection;
using System.Threading;
using VDS.RDF.Configuration;
using VDS.RDF.Parsing;

namespace VDS.RDF.Query.Datasets
{
    /// <summary>
    /// An abstract dataset wrapper that can be used to wrap another dataset and just modify some functionality i.e. provides a decorator over an existing dataset
    /// </summary>
    public abstract class WrapperDataset
        : ISparqlDataset, IConfigurationSerializable
#if !NO_RWLOCK
        , IThreadSafeDataset
#endif
    {
#if !NO_RWLOCK
        private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
#endif

        /// <summary>
        /// Underlying Dataset
        /// </summary>
        protected ISparqlDataset _dataset;

        /// <summary>
        /// Creates a new wrapped dataset
        /// </summary>
        /// <param name="dataset">Dataset</param>
        public WrapperDataset(ISparqlDataset dataset)
        {
            if (dataset == null) throw new ArgumentNullException("dataset");
            this._dataset = dataset;
        }

#if !NO_RWLOCK
        /// <summary>
        /// Gets the Lock used to ensure MRSW concurrency on the dataset when available
        /// </summary>
        public ReaderWriterLockSlim Lock
        {
            get
            {
                if (this._dataset is IThreadSafeDataset)
                {
                    return ((IThreadSafeDataset)this._dataset).Lock;
                }
                else
                {
                    return this._lock;
                }
            }
        }
#endif

        /// <summary>
        /// Gets the underlying dataset
        /// </summary>
        public ISparqlDataset UnderlyingDataset
        {
            get
            {
                return this._dataset;
            }
        }

        #region ISparqlDataset Members

        /// <summary>
        /// Sets the Active Graph for the dataset
        /// </summary>
        /// <param name="graphUris">Graph URIs</param>
        public virtual void SetActiveGraph(IEnumerable<Uri> graphUris)
        {
            this._dataset.SetActiveGraph(graphUris);
        }

        /// <summary>
        /// Sets the Active Graph for the dataset
        /// </summary>
        /// <param name="graphUri">Graph URI</param>
        public virtual void SetActiveGraph(Uri graphUri)
        {
            this._dataset.SetActiveGraph(graphUri);
        }

        /// <summary>
        /// Sets the Default Graph for the dataset
        /// </summary>
        /// <param name="graphUri">Graph URI</param>
        public virtual void SetDefaultGraph(Uri graphUri)
        {
            this._dataset.SetDefaultGraph(graphUri);
        }

        /// <summary>
        /// Sets the Default Graph for the dataset
        /// </summary>
        /// <param name="graphUris">Graph URIs</param>
        public virtual void SetDefaultGraph(IEnumerable<Uri> graphUris)
        {
            this._dataset.SetDefaultGraph(graphUris);
        }

        /// <summary>
        /// Resets the Active Graph
        /// </summary>
        public virtual void ResetActiveGraph()
        {
            this._dataset.ResetActiveGraph();
        }

        /// <summary>
        /// Resets the Default Graph
        /// </summary>
        public virtual void ResetDefaultGraph()
        {
            this._dataset.ResetDefaultGraph();
        }

        /// <summary>
        /// Gets the Default Graph URIs
        /// </summary>
        public virtual IEnumerable<Uri> DefaultGraphUris
        {
            get
            {
                return this._dataset.DefaultGraphUris;
            }
        }

        /// <summary>
        /// Gets the Active Graph URIs
        /// </summary>
        public virtual IEnumerable<Uri> ActiveGraphUris
        {
            get
            {
                return this._dataset.ActiveGraphUris;
            }
        }

        /// <summary>
        /// Gets whether the default graph is the union of all graphs
        /// </summary>
        public virtual bool UsesUnionDefaultGraph
        {
            get
            {
                return this._dataset.UsesUnionDefaultGraph;
            }
        }

        /// <summary>
        /// Adds a Graph to the dataset
        /// </summary>
        /// <param name="g">Graph</param>
        public virtual void AddGraph(IGraph g)
        {
            this._dataset.AddGraph(g);
        }

        /// <summary>
        /// Removes a Graph from the dataset
        /// </summary>
        /// <param name="graphUri">Graph URI</param>
        public virtual void RemoveGraph(Uri graphUri)
        {
            this._dataset.RemoveGraph(graphUri);
        }

        /// <summary>
        /// Gets whether the dataset contains a given Graph
        /// </summary>
        /// <param name="graphUri">Graph URI</param>
        /// <returns></returns>
        public virtual bool HasGraph(Uri graphUri)
        {
            return this._dataset.HasGraph(graphUri);
        }

        /// <summary>
        /// Gets the Graphs in the dataset
        /// </summary>
        public virtual IEnumerable<IGraph> Graphs
        {
            get 
            {
                return this._dataset.Graphs;
            }
        }

        /// <summary>
        /// Gets the URIs of Graphs in the dataset
        /// </summary>
        public virtual IEnumerable<Uri> GraphUris
        {
            get 
            {
                return this._dataset.GraphUris;
            }
        }

        /// <summary>
        /// Gets a Graph from the dataset
        /// </summary>
        /// <param name="graphUri">Graph URI</param>
        /// <returns></returns>
        public virtual IGraph this[Uri graphUri]
        {
            get
            {
                return this._dataset[graphUri];
            }
        }

        /// <summary>
        /// Gets a modifiable graph from the dataset
        /// </summary>
        /// <param name="graphUri">Graph URI</param>
        /// <returns></returns>
        public virtual IGraph GetModifiableGraph(Uri graphUri)
        {
            return this._dataset.GetModifiableGraph(graphUri);
        }

        /// <summary>
        /// Gets whether the dataset has any triples
        /// </summary>
        public virtual bool HasTriples
        {
            get 
            {
                return this._dataset.HasTriples; 
            }
        }

        /// <summary>
        /// Gets whether the dataset contains a given triple
        /// </summary>
        /// <param name="t">Triple</param>
        /// <returns></returns>
        public virtual bool ContainsTriple(Triple t)
        {
            return this._dataset.ContainsTriple(t);
        }

        /// <summary>
        /// Gets all triples from the dataset
        /// </summary>
        public virtual IEnumerable<Triple> Triples
        {
            get
            {
                return this._dataset.Triples;
            }
        }

        /// <summary>
        /// Gets triples with a given subject
        /// </summary>
        /// <param name="subj">Subject</param>
        /// <returns></returns>
        public virtual IEnumerable<Triple> GetTriplesWithSubject(INode subj)
        {
            return this._dataset.GetTriplesWithSubject(subj);
        }

        /// <summary>
        /// Gets triples with a given predicate
        /// </summary>
        /// <param name="pred">Predicate</param>
        /// <returns></returns>
        public virtual IEnumerable<Triple> GetTriplesWithPredicate(INode pred)
        {
            return this._dataset.GetTriplesWithPredicate(pred);
        }

        /// <summary>
        /// Gets triples with a given object
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns></returns>
        public virtual IEnumerable<Triple> GetTriplesWithObject(INode obj)
        {
            return this._dataset.GetTriplesWithObject(obj);
        }

        /// <summary>
        /// Gets triples with a given subject and predicate
        /// </summary>
        /// <param name="subj">Subject</param>
        /// <param name="pred">Predicate</param>
        /// <returns></returns>
        public virtual IEnumerable<Triple> GetTriplesWithSubjectPredicate(INode subj, INode pred)
        {
            return this._dataset.GetTriplesWithSubjectPredicate(subj, pred);
        }

        /// <summary>
        /// Gets triples with a given subject and object
        /// </summary>
        /// <param name="subj">Subject</param>
        /// <param name="obj">Object</param>
        /// <returns></returns>
        public virtual IEnumerable<Triple> GetTriplesWithSubjectObject(INode subj, INode obj)
        {
            return this._dataset.GetTriplesWithSubjectObject(subj, obj);
        }

        /// <summary>
        /// Gets triples with a given predicate and object
        /// </summary>
        /// <param name="pred">Predicate</param>
        /// <param name="obj">Object</param>
        /// <returns></returns>
        public virtual IEnumerable<Triple> GetTriplesWithPredicateObject(INode pred, INode obj)
        {
            return this._dataset.GetTriplesWithPredicateObject(pred, obj);
        }

        /// <summary>
        /// Flushes any changes to the dataset
        /// </summary>
        public virtual void Flush()
        {
            this._dataset.Flush();
        }

        /// <summary>
        /// Discards any changes to the dataset
        /// </summary>
        public virtual void Discard()
        {
            this._dataset.Discard();
        }

        #endregion

        /// <summary>
        /// Serializes the Configuration of the Dataset
        /// </summary>
        /// <param name="context">Serialization Context</param>
        public virtual void SerializeConfiguration(ConfigurationSerializationContext context)
        {
            if (this._dataset is IConfigurationSerializable)
            {
                INode dataset = context.NextSubject;
                INode rdfType = context.Graph.CreateUriNode(UriFactory.Create(RdfSpecsHelper.RdfType));
                INode dnrType = ConfigurationLoader.CreateConfigurationNode(context.Graph, ConfigurationLoader.PropertyType);
                INode datasetClass = ConfigurationLoader.CreateConfigurationNode(context.Graph, ConfigurationLoader.ClassSparqlDataset);
                INode usingDataset = ConfigurationLoader.CreateConfigurationNode(context.Graph, ConfigurationLoader.PropertyUsingDataset);
                INode innerDataset = context.Graph.CreateBlankNode();

#if !SILVERLIGHT
                String assm = Assembly.GetAssembly(this.GetType()).FullName;
#else
                String assm = this.GetType().Assembly.FullName;
#endif
                if (assm.Contains(",")) assm = assm.Substring(0, assm.IndexOf(','));
                String effectiveType = this.GetType().FullName + (assm.Equals("dotNetRDF") ? String.Empty : ", " + assm);

                context.Graph.Assert(dataset, rdfType, datasetClass);
                context.Graph.Assert(dataset, dnrType, context.Graph.CreateLiteralNode(effectiveType));
                context.Graph.Assert(dataset, usingDataset, innerDataset);
                context.NextSubject = innerDataset;

                ((IConfigurationSerializable)this._dataset).SerializeConfiguration(context);
            }
            else
            {
                throw new DotNetRdfConfigurationException("Unable to serialize configuration as the inner dataset is now serializable");
            }
        }
    }
}

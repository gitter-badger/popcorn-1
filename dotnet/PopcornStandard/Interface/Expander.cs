﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Skyward.Popcorn
{
    using ContextType = System.Collections.Generic.Dictionary<string, object>;
    /// <summary>
    /// This is the public interface part for the 'Expander' class.
    /// The expander will allow you to project from one type to another, dynamically selecting which properties to include and
    /// which properties to descend into and retrieve (the expansion part).
    /// 
    /// Types will be mapped implicitly where possible, or you may provide a 'Translator' that handles providing data for a 
    /// particular property.
    /// 
    /// This is intended primarily for Api usage so a client can selectively include properties and nested data in their query.
    /// </summary>
    public partial class Expander
    {
        /// <summary>
        /// This is the core of the expander.  This registers incoming types (the source of the data) and specifies a 
        /// single outgoing type that it will be converted to.
        /// 
        /// It is possible that in the future we may want to provide multiple destination options, primarily for nested 
        /// entities.  Top-level entities will always need a 'default' outgoing type.
        /// </summary>
        internal Dictionary<Type, MappingDefinition> Mappings { get; } = new Dictionary<Type, MappingDefinition>();
        
        /// <summary>
        /// Query whether or not a particular object is either a Mapped type or a collection of a Mapped type.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public bool WillExpand(object source)
        {
            Type sourceType = source.GetType();
            return WillExpandType(sourceType);

        }

        /// <summary>
        /// Query whether or not a particular type is either a Mapped type or a collection of a Mapped type.
        /// </summary>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public bool WillExpandType(Type sourceType)
        {
            if (WillExpandDirect(sourceType))
                return true;
            return WillExpandCollection(sourceType);
        }

        /// <summary>
        /// The entry point method for converting a type into its projection an selectively including data.
        /// This will work on either a Mapped Type or a collection of a Mapped Type.
        /// This version using anonymous objects works well for the Api use case.  We may want a generic typed
        /// version if we ever think of a reason to use this elsewhere.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="context">A context dictionary that will be passed around to all conversion routines.</param>
        /// <param name="includes"></param>
        /// <returns></returns>
        public object Expand(object source, ContextType context = null, IEnumerable<PropertyReference> includes = null)
        {
            // Create a context if one wasn't provided
            if (context == null)
                context = new ContextType();

            // Create an empty include list if one wasn't provided
            if (includes == null)
                includes = new PropertyReference[] { };

            Type sourceType = source.GetType();

            // See if this is a directly expandable type (Mapped Type)
            if (WillExpandDirect(sourceType))
            {
                return ExpandDirectObject(source, context, includes);
            }

            // Otherwise, see if this is a collection of an expandable type
            if (WillExpandCollection(sourceType))
            {
                var interfaceType = sourceType.GetTypeInfo().GetInterfaces()
                   .First(t => t.IsConstructedGenericType
                   && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));

                // Verify that the generic parameter is something we would expand
                var genericType = interfaceType.GenericTypeArguments[0];
                return ExpandCollection(source, typeof(ArrayList), context, includes);
            }

            // Otherwise, the caller requested that we expand a type we have no knowledge of.
            throw new InvalidOperationException(sourceType.ToString());
        }
    }
}
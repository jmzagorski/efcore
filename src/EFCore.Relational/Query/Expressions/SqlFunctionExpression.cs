// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Query.Expressions
{
    /// <summary>
    ///     Represents a SQL function call expression.
    /// </summary>
    [DebuggerDisplay("{" + nameof(ToString) + "()}")]
    public class SqlFunctionExpression : Expression
    {
        private readonly ReadOnlyCollection<Expression> _arguments;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SqlFunctionExpression" /> class.
        /// </summary>
        /// <param name="functionName"> Name of the function. </param>
        /// <param name="returnType"> The return type. </param>
        public SqlFunctionExpression(
            [NotNull] string functionName,
            [NotNull] Type returnType)
            : this(
                  Check.NotEmpty(functionName, nameof(functionName)),
                  Check.NotNull(returnType, nameof(returnType)),
                  Enumerable.Empty<Expression>())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SqlFunctionExpression" /> class.
        /// </summary>
        /// <param name="functionName"> Name of the function. </param>
        /// <param name="returnType"> The return type. </param>
        /// <param name="arguments"> The arguments. </param>
        public SqlFunctionExpression(
            [NotNull] string functionName,
            [NotNull] Type returnType,
            [NotNull] IEnumerable<Expression> arguments)
            : this(
                  Check.NotEmpty(functionName, nameof(functionName)),
                  Check.NotNull(returnType, nameof(returnType)),
                  /*schema*/ null,
                  Check.NotNull(arguments, nameof(arguments)))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SqlFunctionExpression" /> class.
        /// </summary>
        /// <param name="functionName"> Name of the function. </param>
        /// <param name="schema"> The schema this function exists in if any. </param>
        /// <param name="returnType"> The return type. </param>
        /// <param name="arguments"> The arguments. </param>
        public SqlFunctionExpression(
            [NotNull] string functionName,
            [NotNull] Type returnType,
            [CanBeNull] string schema,
            [NotNull] IEnumerable<Expression> arguments)
            : this(
                  /*instance*/ null,
                  Check.NotEmpty(functionName, nameof(functionName)),
                  schema,
                  Check.NotNull(returnType, nameof(returnType)),
                  Check.NotNull(arguments, nameof(arguments)))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SqlFunctionExpression" /> class.
        /// </summary>
        /// <param name="instance"> The instance on which the function is called. </param>
        /// <param name="functionName"> Name of the function. </param>
        /// <param name="returnType"> The return type. </param>
        /// <param name="arguments"> The arguments. </param>
        public SqlFunctionExpression(
            [NotNull] Expression instance,
            [NotNull] string functionName,
            [NotNull] Type returnType,
            [NotNull] IEnumerable<Expression> arguments)
            : this(
                  Check.NotNull(instance, nameof(instance)),
                  Check.NotEmpty(functionName, nameof(functionName)),
                  /*schema*/ null,
                  Check.NotNull(returnType, nameof(returnType)),
                  Check.NotNull(arguments, nameof(arguments)))
        {
        }

        private SqlFunctionExpression(
            [CanBeNull] Expression instance,
            [NotNull] string functionName,
            [CanBeNull] string schema,
            [NotNull] Type returnType,
            [NotNull] IEnumerable<Expression> arguments)
        {
            Instance = instance;
            FunctionName = functionName;
            Type = returnType;
            Schema = schema;
            _arguments = arguments.ToList().AsReadOnly();
        }

        /// <summary>
        ///     Gets the name of the function.
        /// </summary>
        /// <value>
        ///     The name of the function.
        /// </value>
        public virtual string FunctionName { get; }

        /// <summary>
        ///     Gets the name of the schema.
        /// </summary>
        /// <value>
        ///     The name of the schema.
        /// </value>
        public virtual string Schema { get; }

        /// <summary>
        ///     The instance.
        /// </summary>
        public virtual Expression Instance { get; }

        /// <summary>
        ///     The arguments.
        /// </summary>
        public virtual IReadOnlyList<Expression> Arguments => _arguments;

        /// <summary>
        ///     Returns the node type of this <see cref="Expression" />. (Inherited from <see cref="Expression" />.)
        /// </summary>
        /// <returns>The <see cref="ExpressionType" /> that represents this expression.</returns>
        public override ExpressionType NodeType => ExpressionType.Extension;

        /// <summary>
        ///     Gets the static type of the expression that this <see cref="Expression" /> represents. (Inherited from <see cref="Expression" />.)
        /// </summary>
        /// <returns>The <see cref="Type" /> that represents the static type of the expression.</returns>
        public override Type Type { get; }

        /// <summary>
        ///     Dispatches to the specific visit method for this node type.
        /// </summary>
        protected override Expression Accept(ExpressionVisitor visitor)
        {
            Check.NotNull(visitor, nameof(visitor));

            return visitor is ISqlExpressionVisitor specificVisitor
                ? specificVisitor.VisitSqlFunction(this)
                : base.Accept(visitor);
        }

        /// <summary>
        ///     Reduces the node and then calls the <see cref="ExpressionVisitor.Visit(Expression)" /> method passing the
        ///     reduced expression.
        ///     Throws an exception if the node isn't reducible.
        /// </summary>
        /// <param name="visitor"> An instance of <see cref="ExpressionVisitor" />. </param>
        /// <returns> The expression being visited, or an expression which should replace it in the tree. </returns>
        /// <remarks>
        ///     Override this method to provide logic to walk the node's children.
        ///     A typical implementation will call visitor.Visit on each of its
        ///     children, and if any of them change, should return a new copy of
        ///     itself with the modified children.
        /// </remarks>
        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var newInstance = Instance != null ? visitor.Visit(Instance) : null;
            var newArguments = visitor.VisitAndConvert(_arguments, "VisitChildren");

            return newInstance != Instance || newArguments != _arguments
                ? new SqlFunctionExpression(newInstance, FunctionName, Schema, Type, newArguments)
                : this;
        }

        /// <summary>
        ///     Tests if this object is considered equal to another.
        /// </summary>
        /// <param name="obj"> The object to compare with the current object. </param>
        /// <returns>
        ///     true if the objects are considered equal, false if they are not.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && Equals((SqlFunctionExpression)obj);
        }

        private bool Equals(SqlFunctionExpression other)
            => Type == other.Type
               && string.Equals(FunctionName, other.FunctionName)
               && string.Equals(Schema, other.Schema)
               && _arguments.SequenceEqual(other._arguments)
               && (Instance == null && other.Instance == null
                    || Instance?.Equals(other.Instance) == true);

            

        /// <summary>
        ///     Returns a hash code for this object.
        /// </summary>
        /// <returns>
        ///     A hash code for this object.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _arguments.Aggregate(0, (current, argument) => current + ((current * 397) ^ argument.GetHashCode()));
                hashCode = (hashCode * 397) ^ (Instance?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ FunctionName.GetHashCode();
                hashCode = (hashCode * 397) ^ (Schema?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ Type.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        ///     Creates a <see cref="string" /> representation of the Expression.
        /// </summary>
        /// <returns>A <see cref="string" /> representation of the Expression.</returns>
        public override string ToString()
            => (Instance != null ? Instance + "." : Schema != null ? Schema + "." : "") +
            $"{FunctionName}({string.Join("", "", Arguments)}";
    }
}

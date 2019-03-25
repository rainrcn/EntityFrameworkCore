// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Data.Common;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.Storage.Internal
{
    /// <summary>
    ///     <para>
    ///         This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///         directly from your code. This API may change or be removed in future releases.
    ///     </para>
    ///     <para>
    ///         The service lifetime is <see cref="ServiceLifetime.Scoped"/>. This means that each
    ///         <see cref="DbContext"/> instance will use its own instance of this service.
    ///         The implementation may depend on other services registered with any lifetime.
    ///         The implementation does not need to be thread-safe.
    ///     </para>
    /// </summary>
    public class RawSqlCommandBuilder : IRawSqlCommandBuilder
    {
        private readonly IRelationalCommandBuilderFactory _relationalCommandBuilderFactory;
        private readonly ISqlGenerationHelper _sqlGenerationHelper;
        private readonly IParameterNameGeneratorFactory _parameterNameGeneratorFactory;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public RawSqlCommandBuilder(
            [NotNull] IRelationalCommandBuilderFactory relationalCommandBuilderFactory,
            [NotNull] ISqlGenerationHelper sqlGenerationHelper,
            [NotNull] IParameterNameGeneratorFactory parameterNameGeneratorFactory,
            [NotNull] IDiagnosticsLogger<DbLoggerCategory.Database.Command> logger)
        {
            Check.NotNull(relationalCommandBuilderFactory, nameof(relationalCommandBuilderFactory));
            Check.NotNull(sqlGenerationHelper, nameof(sqlGenerationHelper));
            Check.NotNull(parameterNameGeneratorFactory, nameof(parameterNameGeneratorFactory));
            Check.NotNull(logger, nameof(logger));

            _relationalCommandBuilderFactory = relationalCommandBuilderFactory;
            _sqlGenerationHelper = sqlGenerationHelper;
            _parameterNameGeneratorFactory = parameterNameGeneratorFactory;
            Logger = logger;
        }

        public virtual IDiagnosticsLogger<DbLoggerCategory.Database.Command> Logger { get; }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual IRelationalCommand Build(string sql)
            => _relationalCommandBuilderFactory
                .Create()
                .Append(Check.NotEmpty(sql, nameof(sql)))
                .Build();

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual RawSqlCommand Build(string sql, IEnumerable<object> parameters)
        {
            var relationalCommandBuilder = _relationalCommandBuilderFactory.Create();

            var substitutions = new List<string>();

            var parameterNameGenerator = _parameterNameGeneratorFactory.Create();

            var parameterValues = new Dictionary<string, object>();

            foreach (var parameter in parameters)
            {
                if (parameter is DbParameter dbParameter)
                {
                    if (string.IsNullOrEmpty(dbParameter.ParameterName))
                    {
                        dbParameter.ParameterName = _sqlGenerationHelper.GenerateParameterName(parameterNameGenerator.GenerateNext());
                    }

                    substitutions.Add(dbParameter.ParameterName);
                    relationalCommandBuilder.AddRawParameter(dbParameter.ParameterName, dbParameter);
                }
                else
                {
                    var parameterName = parameterNameGenerator.GenerateNext();
                    var substitutedName = _sqlGenerationHelper.GenerateParameterName(parameterName);

                    substitutions.Add(substitutedName);
                    relationalCommandBuilder.AddParameter(parameterName, substitutedName);
                    parameterValues.Add(parameterName, parameter);
                }
            }

            // ReSharper disable once CoVariantArrayConversion
            sql = string.Format(sql, substitutions.ToArray());

            return new RawSqlCommand(
                relationalCommandBuilder.Append(sql).Build(),
                parameterValues);
        }
    }
}

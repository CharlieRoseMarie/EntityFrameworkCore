// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Transactions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.Diagnostics
{
    /// <summary>
    ///     <para>
    ///         This class contains static methods used by EF Core internals and relationl database providers to
    ///         write information to an <see cref="ILogger" /> and a <see cref="DiagnosticListener" /> for
    ///         well-known events.
    ///     </para>
    ///     <para>
    ///         This type is typically used by database providers (and other extensions). It is generally
    ///         not used in application code.
    ///     </para>
    /// </summary>
    public static class RelationalLoggerExtensions
    {
        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.CommandExecuting" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="command"> The database command object. </param>
        /// <param name="executeMethod"> Represents the method that will be called to execute the command. </param>
        /// <param name="commandId"> The correlation ID associated with the given <see cref="DbCommand" />. </param>
        /// <param name="connectionId"> The correlation ID associated with the <see cref="DbConnection" /> being used. </param>
        /// <param name="async"> Indicates whether or not this is an async operation. </param>
        /// <param name="startTime"> The time that execution began. </param>
        public static void CommandExecuting(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Database.Command> diagnostics,
            [NotNull] DbCommand command,
            DbCommandMethod executeMethod,
            Guid commandId,
            Guid connectionId,
            bool async,
            DateTimeOffset startTime)
        {
            var definition = RelationalResources.LogExecutingCommand(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    command.Parameters.FormatParameters(ShouldLogParameterValues(diagnostics, command)),
                    command.CommandType,
                    command.CommandTimeout,
                    Environment.NewLine,
                    command.CommandText.TrimEnd());
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new CommandEventData(
                        definition,
                        CommandExecuting,
                        command,
                        executeMethod,
                        commandId,
                        connectionId,
                        async,
                        ShouldLogParameterValues(diagnostics, command),
                        startTime));
            }
        }

        private static string CommandExecuting(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<string, CommandType, int, string, string>)definition;
            var p = (CommandEventData)payload;
            return d.GenerateMessage(
                p.Command.Parameters.FormatParameters(p.LogParameterValues),
                p.Command.CommandType,
                p.Command.CommandTimeout,
                Environment.NewLine,
                p.Command.CommandText.TrimEnd());
        }

        private static bool ShouldLogParameterValues(
            IDiagnosticsLogger<DbLoggerCategory.Database.Command> diagnostics,
            DbCommand command)
            => command.Parameters.Count > 0
               && diagnostics.ShouldLogSensitiveData();

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.CommandExecuted" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="command"> The database command object. </param>
        /// <param name="executeMethod"> Represents the method that will be called to execute the command. </param>
        /// <param name="commandId"> The correlation ID associated with the given <see cref="DbCommand" />. </param>
        /// <param name="connectionId"> The correlation ID associated with the <see cref="DbConnection" /> being used. </param>
        /// <param name="methodResult"> The return value from the underlying method execution. </param>
        /// <param name="async"> Indicates whether or not this is an async command. </param>
        /// <param name="startTime"> The time that execution began. </param>
        /// <param name="duration"> The duration of the command execution, not including consuming results. </param>
        public static void CommandExecuted(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Database.Command> diagnostics,
            [NotNull] DbCommand command,
            DbCommandMethod executeMethod,
            Guid commandId,
            Guid connectionId,
            [CanBeNull] object methodResult,
            bool async,
            DateTimeOffset startTime,
            TimeSpan duration)
        {
            var definition = RelationalResources.LogExecutedCommand(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds),
                    command.Parameters.FormatParameters(ShouldLogParameterValues(diagnostics, command)),
                    command.CommandType,
                    command.CommandTimeout,
                    Environment.NewLine,
                    command.CommandText.TrimEnd());
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new CommandExecutedEventData(
                        definition,
                        CommandExecuted,
                        command,
                        executeMethod,
                        commandId,
                        connectionId,
                        methodResult,
                        async,
                        ShouldLogParameterValues(diagnostics, command),
                        startTime,
                        duration));
            }
        }

        private static string CommandExecuted(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<string, string, CommandType, int, string, string>)definition;
            var p = (CommandExecutedEventData)payload;
            return d.GenerateMessage(
                string.Format(CultureInfo.InvariantCulture, "{0:N0}", p.Duration.TotalMilliseconds),
                p.Command.Parameters.FormatParameters(p.LogParameterValues),
                p.Command.CommandType,
                p.Command.CommandTimeout,
                Environment.NewLine,
                p.Command.CommandText.TrimEnd());
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.CommandError" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="command"> The database command object. </param>
        /// <param name="executeMethod"> Represents the method that will be called to execute the command. </param>
        /// <param name="commandId"> The correlation ID associated with the given <see cref="DbCommand" />. </param>
        /// <param name="connectionId"> The correlation ID associated with the <see cref="DbConnection" /> being used. </param>
        /// <param name="exception"> The exception that caused this failure. </param>
        /// <param name="async"> Indicates whether or not this is an async command. </param>
        /// <param name="startTime"> The time that execution began. </param>
        /// <param name="duration"> The amount of time that passed until the exception was raised. </param>
        public static void CommandError(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Database.Command> diagnostics,
            [NotNull] DbCommand command,
            DbCommandMethod executeMethod,
            Guid commandId,
            Guid connectionId,
            [NotNull] Exception exception,
            bool async,
            DateTimeOffset startTime,
            TimeSpan duration)
        {
            var definition = RelationalResources.LogCommandFailed(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds),
                    command.Parameters.FormatParameters(ShouldLogParameterValues(diagnostics, command)),
                    command.CommandType,
                    command.CommandTimeout,
                    Environment.NewLine,
                    command.CommandText.TrimEnd());
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new CommandErrorEventData(
                        definition,
                        CommandError,
                        command,
                        executeMethod,
                        commandId,
                        connectionId,
                        exception,
                        async,
                        ShouldLogParameterValues(diagnostics, command),
                        startTime,
                        duration));
            }
        }

        private static string CommandError(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<string, string, CommandType, int, string, string>)definition;
            var p = (CommandErrorEventData)payload;
            return d.GenerateMessage(
                string.Format(CultureInfo.InvariantCulture, "{0:N0}", p.Duration.TotalMilliseconds),
                p.Command.Parameters.FormatParameters(p.LogParameterValues),
                p.Command.CommandType,
                p.Command.CommandTimeout,
                Environment.NewLine,
                p.Command.CommandText.TrimEnd());
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.ConnectionOpening" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="connection"> The connection. </param>
        /// <param name="startTime"> The time that the operation was started. </param>
        /// <param name="async"> Indicates whether or not this is an async operation. </param>
        public static void ConnectionOpening(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Database.Connection> diagnostics,
            [NotNull] IRelationalConnection connection,
            DateTimeOffset startTime,
            bool async)
        {
            var definition = RelationalResources.LogOpeningConnection(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    connection.DbConnection.Database, connection.DbConnection.DataSource);
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new ConnectionEventData(
                        definition,
                        ConnectionOpening,
                        connection.DbConnection,
                        connection.ConnectionId,
                        async,
                        startTime));
            }
        }

        private static string ConnectionOpening(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<string, string>)definition;
            var p = (ConnectionEventData)payload;
            return d.GenerateMessage(
                p.Connection.Database,
                p.Connection.DataSource);
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.ConnectionOpened" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="connection"> The connection. </param>
        /// <param name="startTime"> The time that the operation was started. </param>
        /// <param name="duration"> The amount of time before the connection was opened. </param>
        /// <param name="async"> Indicates whether or not this is an async operation. </param>
        public static void ConnectionOpened(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Database.Connection> diagnostics,
            [NotNull] IRelationalConnection connection,
            DateTimeOffset startTime,
            TimeSpan duration,
            bool async)
        {
            var definition = RelationalResources.LogOpenedConnection(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    connection.DbConnection.Database, connection.DbConnection.DataSource);
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new ConnectionEndEventData(
                        definition,
                        ConnectionOpened,
                        connection.DbConnection,
                        connection.ConnectionId,
                        async,
                        startTime,
                        duration));
            }
        }

        private static string ConnectionOpened(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<string, string>)definition;
            var p = (ConnectionEndEventData)payload;
            return d.GenerateMessage(
                p.Connection.Database,
                p.Connection.DataSource);
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.ConnectionClosing" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="connection"> The connection. </param>
        /// <param name="startTime"> The time that the operation was started. </param>
        /// <param name="async"> Indicates whether or not this is an async operation. </param>
        public static void ConnectionClosing(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Database.Connection> diagnostics,
            [NotNull] IRelationalConnection connection,
            DateTimeOffset startTime,
            bool async)
        {
            var definition = RelationalResources.LogClosingConnection(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    connection.DbConnection.Database, connection.DbConnection.DataSource);
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new ConnectionEventData(
                        definition,
                        ConnectionClosing,
                        connection.DbConnection,
                        connection.ConnectionId,
                        false,
                        startTime));
            }
        }

        private static string ConnectionClosing(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<string, string>)definition;
            var p = (ConnectionEventData)payload;
            return d.GenerateMessage(
                p.Connection.Database,
                p.Connection.DataSource);
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.ConnectionClosed" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="connection"> The connection. </param>
        /// <param name="startTime"> The time that the operation was started. </param>
        /// <param name="duration"> The amount of time before the connection was closed. </param>
        /// <param name="async"> Indicates whether or not this is an async operation. </param>
        public static void ConnectionClosed(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Database.Connection> diagnostics,
            [NotNull] IRelationalConnection connection,
            DateTimeOffset startTime,
            TimeSpan duration,
            bool async)
        {
            var definition = RelationalResources.LogClosedConnection(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    connection.DbConnection.Database, connection.DbConnection.DataSource);
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new ConnectionEndEventData(
                        definition,
                        ConnectionClosed,
                        connection.DbConnection,
                        connection.ConnectionId,
                        false,
                        startTime,
                        duration));
            }
        }

        private static string ConnectionClosed(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<string, string>)definition;
            var p = (ConnectionEndEventData)payload;
            return d.GenerateMessage(
                p.Connection.Database,
                p.Connection.DataSource);
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.ConnectionError" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="connection"> The connection. </param>
        /// <param name="exception"> The exception representing the error. </param>
        /// <param name="startTime"> The time that the operation was started. </param>
        /// <param name="duration"> The elapsed time before the operation failed. </param>
        /// <param name="async"> Indicates whether or not this is an async operation. </param>
        /// <param name="logErrorAsDebug"> A flag indicating the exception is being handled and so it should be logged at Debug level. </param>
        public static void ConnectionError(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Database.Connection> diagnostics,
            [NotNull] IRelationalConnection connection,
            [NotNull] Exception exception,
            DateTimeOffset startTime,
            TimeSpan duration,
            bool async,
            bool logErrorAsDebug)
        {
            var definition = logErrorAsDebug
                ? RelationalResources.LogConnectionErrorAsDebug(diagnostics)
                : RelationalResources.LogConnectionError(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    connection.DbConnection.Database, connection.DbConnection.DataSource);
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new ConnectionErrorEventData(
                        definition,
                        ConnectionError,
                        connection.DbConnection,
                        connection.ConnectionId,
                        exception,
                        async,
                        startTime,
                        duration));
            }
        }

        private static string ConnectionError(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<string, string>)definition;
            var p = (ConnectionErrorEventData)payload;
            return d.GenerateMessage(
                p.Connection.Database,
                p.Connection.DataSource);
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.TransactionStarted" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="connection"> The connection. </param>
        /// <param name="transaction"> The transaction. </param>
        /// <param name="transactionId"> The correlation ID associated with the <see cref="DbTransaction" />. </param>
        /// <param name="startTime"> The time that the operation was started. </param>
        public static void TransactionStarted(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> diagnostics,
            [NotNull] IRelationalConnection connection,
            [NotNull] DbTransaction transaction,
            Guid transactionId,
            DateTimeOffset startTime)
        {
            var definition = RelationalResources.LogBeginningTransaction(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    transaction.IsolationLevel.ToString("G"));
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new TransactionEventData(
                        definition,
                        TransactionStarted,
                        transaction,
                        transactionId,
                        connection.ConnectionId,
                        startTime));
            }
        }

        private static string TransactionStarted(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<string>)definition;
            var p = (TransactionEventData)payload;
            return d.GenerateMessage(
                p.Transaction.IsolationLevel.ToString("G"));
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.TransactionUsed" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="connection"> The connection. </param>
        /// <param name="transaction"> The transaction. </param>
        /// <param name="transactionId"> The correlation ID associated with the <see cref="DbTransaction" />. </param>
        /// <param name="startTime"> The time that the operation was started. </param>
        public static void TransactionUsed(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> diagnostics,
            [NotNull] IRelationalConnection connection,
            [NotNull] DbTransaction transaction,
            Guid transactionId,
            DateTimeOffset startTime)
        {
            var definition = RelationalResources.LogUsingTransaction(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    transaction.IsolationLevel.ToString("G"));
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new TransactionEventData(
                        definition,
                        TransactionUsed,
                        transaction,
                        transactionId,
                        connection.ConnectionId,
                        startTime));
            }
        }

        private static string TransactionUsed(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<string>)definition;
            var p = (TransactionEventData)payload;
            return d.GenerateMessage(
                p.Transaction.IsolationLevel.ToString("G"));
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.TransactionCommitted" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="connection"> The connection. </param>
        /// <param name="transaction"> The transaction. </param>
        /// <param name="transactionId"> The correlation ID associated with the <see cref="DbTransaction" />. </param>
        /// <param name="startTime"> The time that the operation was started. </param>
        /// <param name="duration"> The elapsed time from when the operation was started. </param>
        public static void TransactionCommitted(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> diagnostics,
            [NotNull] IRelationalConnection connection,
            [NotNull] DbTransaction transaction,
            Guid transactionId,
            DateTimeOffset startTime,
            TimeSpan duration)
        {
            var definition = RelationalResources.LogCommittingTransaction(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(diagnostics, warningBehavior);
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new TransactionEndEventData(
                        definition,
                        (d, p) => ((EventDefinition)d).GenerateMessage(),
                        transaction,
                        transactionId,
                        connection.ConnectionId,
                        startTime,
                        duration));
            }
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.TransactionRolledBack" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="connection"> The connection. </param>
        /// <param name="transaction"> The transaction. </param>
        /// <param name="transactionId"> The correlation ID associated with the <see cref="DbTransaction" />. </param>
        /// <param name="startTime"> The time that the operation was started. </param>
        /// <param name="duration"> The elapsed time from when the operation was started. </param>
        public static void TransactionRolledBack(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> diagnostics,
            [NotNull] IRelationalConnection connection,
            [NotNull] DbTransaction transaction,
            Guid transactionId,
            DateTimeOffset startTime,
            TimeSpan duration)
        {
            var definition = RelationalResources.LogRollingbackTransaction(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(diagnostics, warningBehavior);
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new TransactionEndEventData(
                        definition,
                        (d, p) => ((EventDefinition)d).GenerateMessage(),
                        transaction,
                        transactionId,
                        connection.ConnectionId,
                        startTime,
                        duration));
            }
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.TransactionDisposed" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="connection"> The connection. </param>
        /// <param name="transaction"> The transaction. </param>
        /// <param name="transactionId"> The correlation ID associated with the <see cref="DbTransaction" />. </param>
        /// <param name="startTime"> The time that the operation was started. </param>
        public static void TransactionDisposed(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> diagnostics,
            [NotNull] IRelationalConnection connection,
            [NotNull] DbTransaction transaction,
            Guid transactionId,
            DateTimeOffset startTime)
        {
            var definition = RelationalResources.LogDisposingTransaction(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(diagnostics, warningBehavior);
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new TransactionEventData(
                        definition,
                        (d, p) => ((EventDefinition)d).GenerateMessage(),
                        transaction,
                        transactionId,
                        connection.ConnectionId,
                        startTime));
            }
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.TransactionError" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="connection"> The connection. </param>
        /// <param name="transaction"> The transaction. </param>
        /// <param name="transactionId"> The correlation ID associated with the <see cref="DbTransaction" />. </param>
        /// <param name="action"> The action being taken. </param>
        /// <param name="exception"> The exception that represents the error. </param>
        /// <param name="startTime"> The time that the operation was started. </param>
        /// <param name="duration"> The elapsed time from when the operation was started. </param>
        public static void TransactionError(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> diagnostics,
            [NotNull] IRelationalConnection connection,
            [NotNull] DbTransaction transaction,
            Guid transactionId,
            [NotNull] string action,
            [NotNull] Exception exception,
            DateTimeOffset startTime,
            TimeSpan duration)
        {
            var definition = RelationalResources.LogTransactionError(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(diagnostics, warningBehavior, exception);
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new TransactionErrorEventData(
                        definition,
                        (d, p) => ((EventDefinition)d).GenerateMessage(),
                        transaction,
                        connection.ConnectionId,
                        transactionId,
                        action,
                        exception,
                        startTime,
                        duration));
            }
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.AmbientTransactionWarning" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="connection"> The connection. </param>
        /// <param name="startTime"> The time that the operation was started. </param>
        public static void AmbientTransactionWarning(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> diagnostics,
            [NotNull] IRelationalConnection connection,
            DateTimeOffset startTime)
        {
            var definition = RelationalResources.LogAmbientTransaction(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(diagnostics, warningBehavior);
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new ConnectionEventData(
                        definition,
                        (d, p) => ((EventDefinition)d).GenerateMessage(),
                        connection.DbConnection,
                        connection.ConnectionId,
                        false,
                        startTime));
            }
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.AmbientTransactionEnlisted" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="connection"> The connection. </param>
        /// <param name="transaction"> The transaction. </param>
        public static void AmbientTransactionEnlisted(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> diagnostics,
            [NotNull] IRelationalConnection connection,
            [NotNull] Transaction transaction)
        {
            var definition = RelationalResources.LogAmbientTransactionEnlisted(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    transaction.IsolationLevel.ToString("G"));
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new TransactionEnlistedEventData(
                        definition,
                        AmbientTransactionEnlisted,
                        transaction,
                        connection.DbConnection,
                        connection.ConnectionId));
            }
        }

        private static string AmbientTransactionEnlisted(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<string>)definition;
            var p = (TransactionEnlistedEventData)payload;
            return d.GenerateMessage(p.Transaction.IsolationLevel.ToString("G"));
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.ExplicitTransactionEnlisted" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="connection"> The connection. </param>
        /// <param name="transaction"> The transaction. </param>
        public static void ExplicitTransactionEnlisted(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> diagnostics,
            [NotNull] IRelationalConnection connection,
            [NotNull] Transaction transaction)
        {
            var definition = RelationalResources.LogExplicitTransactionEnlisted(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    transaction.IsolationLevel.ToString("G"));
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new TransactionEnlistedEventData(
                        definition,
                        ExplicitTransactionEnlisted,
                        transaction,
                        connection.DbConnection,
                        connection.ConnectionId));
            }
        }

        private static string ExplicitTransactionEnlisted(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<string>)definition;
            var p = (TransactionEnlistedEventData)payload;
            return d.GenerateMessage(p.Transaction.IsolationLevel.ToString("G"));
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.DataReaderDisposing" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="connection"> The connection. </param>
        /// <param name="command"> The database command object. </param>
        /// <param name="dataReader"> The data reader. </param>
        /// <param name="commandId"> The correlation ID associated with the given <see cref="DbCommand" />. </param>
        /// <param name="recordsAffected"> The number of records in the database that were affected. </param>
        /// <param name="readCount"> The number of records that were read. </param>
        /// <param name="startTime"> The time that the operation was started. </param>
        /// <param name="duration"> The elapsed time from when the operation was started. </param>
        public static void DataReaderDisposing(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Database.Command> diagnostics,
            [NotNull] IRelationalConnection connection,
            [NotNull] DbCommand command,
            [NotNull] DbDataReader dataReader,
            Guid commandId,
            int recordsAffected,
            int readCount,
            DateTimeOffset startTime,
            TimeSpan duration)
        {
            var definition = RelationalResources.LogDisposingDataReader(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(diagnostics, warningBehavior);
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new DataReaderDisposingEventData(
                        definition,
                        (d, p) => ((EventDefinition)d).GenerateMessage(),
                        command,
                        dataReader,
                        commandId,
                        connection.ConnectionId,
                        recordsAffected,
                        readCount,
                        startTime,
                        duration));
            }
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.MigrateUsingConnection" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="migrator"> The migrator. </param>
        /// <param name="connection"> The connection. </param>
        public static void MigrateUsingConnection(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Migrations> diagnostics,
            [NotNull] IMigrator migrator,
            [NotNull] IRelationalConnection connection)
        {
            var definition = RelationalResources.LogMigrating(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                var dbConnection = connection.DbConnection;

                definition.Log(
                    diagnostics,
                    warningBehavior,
                    dbConnection.Database, dbConnection.DataSource);
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new MigratorConnectionEventData(
                        definition,
                        MigrateUsingConnection,
                        migrator,
                        connection.DbConnection,
                        connection.ConnectionId));
            }
        }

        private static string MigrateUsingConnection(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<string, string>)definition;
            var p = (MigratorConnectionEventData)payload;
            return d.GenerateMessage(
                p.Connection.Database,
                p.Connection.DataSource);
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.MigrationReverting" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="migrator"> The migrator. </param>
        /// <param name="migration"> The migration. </param>
        public static void MigrationReverting(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Migrations> diagnostics,
            [NotNull] IMigrator migrator,
            [NotNull] Migration migration)
        {
            var definition = RelationalResources.LogRevertingMigration(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    migration.GetId());
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new MigrationEventData(
                        definition,
                        MigrationReverting,
                        migrator,
                        migration));
            }
        }

        private static string MigrationReverting(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<string>)definition;
            var p = (MigrationEventData)payload;
            return d.GenerateMessage(p.Migration.GetId());
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.MigrationApplying" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="migrator"> The migrator. </param>
        /// <param name="migration"> The migration. </param>
        public static void MigrationApplying(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Migrations> diagnostics,
            [NotNull] IMigrator migrator,
            [NotNull] Migration migration)
        {
            var definition = RelationalResources.LogApplyingMigration(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    migration.GetId());
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new MigrationEventData(
                        definition,
                        MigrationApplying,
                        migrator,
                        migration));
            }
        }

        private static string MigrationApplying(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<string>)definition;
            var p = (MigrationEventData)payload;
            return d.GenerateMessage(p.Migration.GetId());
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.MigrationGeneratingDownScript" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="migrator"> The migrator. </param>
        /// <param name="migration"> The migration. </param>
        /// <param name="fromMigration"> The starting migration name. </param>
        /// <param name="toMigration"> The ending migration name. </param>
        /// <param name="idempotent"> Indicates whether or not an idempotent script is being generated. </param>
        public static void MigrationGeneratingDownScript(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Migrations> diagnostics,
            [NotNull] IMigrator migrator,
            [NotNull] Migration migration,
            [CanBeNull] string fromMigration,
            [CanBeNull] string toMigration,
            bool idempotent)
        {
            var definition = RelationalResources.LogGeneratingDown(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    migration.GetId());
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new MigrationScriptingEventData(
                        definition,
                        MigrationGeneratingDownScript,
                        migrator,
                        migration,
                        fromMigration,
                        toMigration,
                        idempotent));
            }
        }

        private static string MigrationGeneratingDownScript(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<string>)definition;
            var p = (MigrationScriptingEventData)payload;
            return d.GenerateMessage(p.Migration.GetId());
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.MigrationGeneratingUpScript" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="migrator"> The migrator. </param>
        /// <param name="migration"> The migration. </param>
        /// <param name="fromMigration"> The starting migration name. </param>
        /// <param name="toMigration"> The ending migration name. </param>
        /// <param name="idempotent"> Indicates whether or not an idempotent script is being generated. </param>
        public static void MigrationGeneratingUpScript(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Migrations> diagnostics,
            [NotNull] IMigrator migrator,
            [NotNull] Migration migration,
            [CanBeNull] string fromMigration,
            [CanBeNull] string toMigration,
            bool idempotent)
        {
            var definition = RelationalResources.LogGeneratingUp(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    migration.GetId());
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new MigrationScriptingEventData(
                        definition,
                        MigrationGeneratingUpScript,
                        migrator,
                        migration,
                        fromMigration,
                        toMigration,
                        idempotent));
            }
        }

        private static string MigrationGeneratingUpScript(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<string>)definition;
            var p = (MigrationScriptingEventData)payload;
            return d.GenerateMessage(p.Migration.GetId());
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.MigrationsNotApplied" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="migrator"> The migrator. </param>
        public static void MigrationsNotApplied(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Migrations> diagnostics,
            [NotNull] IMigrator migrator)
        {
            var definition = RelationalResources.LogNoMigrationsApplied(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(diagnostics, warningBehavior);
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new MigratorEventData(
                        definition,
                        (d, p) => ((EventDefinition)d).GenerateMessage(),
                        migrator));
            }
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.MigrationsNotFound" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="migrator"> The migrator. </param>
        /// <param name="migrationsAssembly"> The assembly in which migrations are stored. </param>
        public static void MigrationsNotFound(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Migrations> diagnostics,
            [NotNull] IMigrator migrator,
            [NotNull] IMigrationsAssembly migrationsAssembly)
        {
            var definition = RelationalResources.LogNoMigrationsFound(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    migrationsAssembly.Assembly.GetName().Name);
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new MigrationAssemblyEventData(
                        definition,
                        MigrationsNotFound,
                        migrator,
                        migrationsAssembly));
            }
        }

        private static string MigrationsNotFound(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<string>)definition;
            var p = (MigrationAssemblyEventData)payload;
            return d.GenerateMessage(p.MigrationsAssembly.Assembly.GetName().Name);
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.MigrationAttributeMissingWarning" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="migrationType"> Info for the migration type. </param>
        public static void MigrationAttributeMissingWarning(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Migrations> diagnostics,
            [NotNull] TypeInfo migrationType)
        {
            var definition = RelationalResources.LogMigrationAttributeMissingWarning(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    migrationType.Name);
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new MigrationTypeEventData(
                        definition,
                        MigrationAttributeMissingWarning,
                        migrationType));
            }
        }

        private static string MigrationAttributeMissingWarning(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<string>)definition;
            var p = (MigrationTypeEventData)payload;
            return d.GenerateMessage(p.MigrationType.Name);
        }

        ///// <summary>
        /////     Logs for the <see cref="RelationalEventId.QueryClientEvaluationWarning" /> event.
        ///// </summary>
        ///// <param name="diagnostics"> The diagnostics logger to use. </param>
        ///// <param name="queryModel"> The query model. </param>
        ///// <param name="queryModelElement"> The element that is being client evaluated. </param>
        //public static void QueryClientEvaluationWarning(
        //    [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Query> diagnostics,
        //    [NotNull] QueryModel queryModel,
        //    [NotNull] object queryModelElement)
        //{
        //    var definition = RelationalResources.LogClientEvalWarning(diagnostics);

        //    var warningBehavior = definition.GetLogBehavior(diagnostics);
        //    if (warningBehavior != WarningBehavior.Ignore)
        //    {
        //        definition.Log(
        //            diagnostics,
        //            warningBehavior,
        //            queryModelElement);
        //    }

        //    if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
        //    {
        //        diagnostics.DiagnosticSource.Write(
        //            definition.EventId.Name,
        //            new QueryModelClientEvalEventData(
        //                definition,
        //                QueryClientEvaluationWarning,
        //                queryModel,
        //                queryModelElement));
        //    }
        //}

        //private static string QueryClientEvaluationWarning(EventDefinitionBase definition, EventData payload)
        //{
        //    var d = (EventDefinition<object>)definition;
        //    var p = (QueryModelClientEvalEventData)payload;
        //    return d.GenerateMessage(p.QueryModelElement);
        //}

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.QueryPossibleUnintendedUseOfEqualsWarning" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="methodCallExpression"> The expression representing the problematic method call. </param>
        public static void QueryPossibleUnintendedUseOfEqualsWarning(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Query> diagnostics,
            [NotNull] MethodCallExpression methodCallExpression)
        {
            var definition = RelationalResources.LogPossibleUnintendedUseOfEquals(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    methodCallExpression);
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new ExpressionEventData(
                        definition,
                        QueryPossibleUnintendedUseOfEqualsWarning,
                        methodCallExpression));
            }
        }

        private static string QueryPossibleUnintendedUseOfEqualsWarning(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<object>)definition;
            var p = (ExpressionEventData)payload;
            return d.GenerateMessage(p.Expression);
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.QueryPossibleExceptionWithAggregateOperatorWarning" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        public static void QueryPossibleExceptionWithAggregateOperatorWarning(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Query> diagnostics)
        {
            var definition = RelationalResources.LogQueryPossibleExceptionWithAggregateOperatorWarning(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(diagnostics, warningBehavior);
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new EventData(
                        definition,
                        (d, p) => ((EventDefinition)d).GenerateMessage()));
            }
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.ModelValidationKeyDefaultValueWarning" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="property"> The property. </param>
        public static void ModelValidationKeyDefaultValueWarning(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Model.Validation> diagnostics,
            [NotNull] IProperty property)
        {
            var definition = RelationalResources.LogKeyHasDefaultValue(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    property.Name,
                    property.DeclaringEntityType.DisplayName());
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new PropertyEventData(
                        definition,
                        ModelValidationKeyDefaultValueWarning,
                        property));
            }
        }

        private static string ModelValidationKeyDefaultValueWarning(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<string, string>)definition;
            var p = (PropertyEventData)payload;
            return d.GenerateMessage(
                p.Property.Name,
                p.Property.DeclaringEntityType.DisplayName());
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.BoolWithDefaultWarning" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="property"> The property. </param>
        public static void BoolWithDefaultWarning(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Model.Validation> diagnostics,
            [NotNull] IProperty property)
        {
            var definition = RelationalResources.LogBoolWithDefaultWarning(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    property.Name,
                    property.DeclaringEntityType.DisplayName());
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new PropertyEventData(
                        definition,
                        BoolWithDefaultWarning,
                        property));
            }
        }

        private static string BoolWithDefaultWarning(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<string, string>)definition;
            var p = (PropertyEventData)payload;
            return d.GenerateMessage(p.Property.Name, p.Property.DeclaringEntityType.DisplayName());
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.BatchReadyForExecution" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="entries"> The entries for entities in the batch. </param>
        /// <param name="commandCount"> The number of commands. </param>
        public static void BatchReadyForExecution(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics,
            [NotNull] IEnumerable<IUpdateEntry> entries,
            int commandCount)
        {
            var definition = RelationalResources.LogBatchReadyForExecution(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    commandCount);
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new BatchEventData(
                        definition,
                        BatchReadyForExecution,
                        entries,
                        commandCount));
            }
        }

        private static string BatchReadyForExecution(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<int>)definition;
            var p = (BatchEventData)payload;
            return d.GenerateMessage(p.CommandCount);
        }

        /// <summary>
        ///     Logs for the <see cref="RelationalEventId.BatchSmallerThanMinBatchSize" /> event.
        /// </summary>
        /// <param name="diagnostics"> The diagnostics logger to use. </param>
        /// <param name="entries"> The entries for entities in the batch. </param>
        /// <param name="commandCount"> The number of commands. </param>
        /// <param name="minBatchSize"> The minimum batch size. </param>
        public static void BatchSmallerThanMinBatchSize(
            [NotNull] this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics,
            [NotNull] IEnumerable<IUpdateEntry> entries,
            int commandCount,
            int minBatchSize)
        {
            var definition = RelationalResources.LogBatchSmallerThanMinBatchSize(diagnostics);

            var warningBehavior = definition.GetLogBehavior(diagnostics);
            if (warningBehavior != WarningBehavior.Ignore)
            {
                definition.Log(
                    diagnostics,
                    warningBehavior,
                    commandCount, minBatchSize);
            }

            if (diagnostics.DiagnosticSource.IsEnabled(definition.EventId.Name))
            {
                diagnostics.DiagnosticSource.Write(
                    definition.EventId.Name,
                    new MinBatchSizeEventData(
                        definition,
                        BatchSmallerThanMinBatchSize,
                        entries,
                        commandCount,
                        minBatchSize));
            }
        }

        private static string BatchSmallerThanMinBatchSize(EventDefinitionBase definition, EventData payload)
        {
            var d = (EventDefinition<int, int>)definition;
            var p = (MinBatchSizeEventData)payload;
            return d.GenerateMessage(p.CommandCount, p.MinBatchSize);
        }
    }
}

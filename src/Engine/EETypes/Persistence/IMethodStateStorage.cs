﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Descriptors;

namespace Dasync.EETypes.Persistence
{
    public interface IMethodStateStorage
    {
        Task<string> WriteStateAsync(
            ServiceId serviceId,
            PersistedMethodId methodId,
            MethodExecutionState state);

        Task<MethodExecutionState> ReadStateAsync(
            ServiceId serviceId,
            PersistedMethodId methodId,
            CancellationToken ct);

        Task WriteResultAsync(
            ServiceId serviceId,
            MethodId methodId,
            string intentId,
            ITaskResult result);

        Task<ITaskResult> TryReadResultAsync(
            ServiceId serviceId,
            MethodId methodId,
            string intentId,
            Type resultValueType,
            CancellationToken ct);
    }
}
